// AI CONTRACT:
// Must follow LUNE_CONTEXT.md §2.1 (conversational presence)
// Streaming-first, correlation IDs, timestamps, monotonic sequence
namespace Luna.Classroom.Realtime;

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Luna.Classroom.Orchestration;
using Luna.Classroom.Persistence;
using Luna.Classroom.Sessions;
using Luna.Contracts.Realtime;
using Luna.Contracts.Realtime.Payloads;
using Luna.SharedKernel.Time;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// WebSocket handler for real-time classroom communication.
/// Manages message routing, envelope validation, and lifecycle.
/// </summary>
public interface IWebSocketHandler
{
    Task HandleConnectionAsync(string sessionId, WebSocket socket, CancellationToken ct);
}

public sealed class ClassroomWebSocketHandler : IWebSocketHandler
{
    private readonly ISessionRepository _sessions;
    private readonly ITeachingOrchestrator _orchestrator;
    private readonly ISystemClock _clock;
    private readonly ILogger<ClassroomWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private sealed class SequenceCounter
    {
        private int _value;
        public int Next() => Interlocked.Increment(ref _value);
    }

    private sealed record RawEnvelope(
        string? MessageId,
        string? CorrelationId,
        string? MessageType,
        DateTime? Timestamp,
        int? SequenceNumber,
        JsonElement Payload
    );

    public ClassroomWebSocketHandler(
        ISessionRepository sessions,
        ITeachingOrchestrator orchestrator,
        ISystemClock clock,
        ILogger<ClassroomWebSocketHandler> logger)
    {
        _sessions = sessions;
        _orchestrator = orchestrator;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(string sessionId, WebSocket socket, CancellationToken ct)
    {
        var session = await _sessions.GetSessionAsync(sessionId, ct);
        if (session is null)
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Session not found", ct);
            return;
        }

        var connectionCorrelationId = Guid.NewGuid().ToString("N");
        var seq = new SequenceCounter();

        // Mark connection event
        session.RecordEvent(new SessionEvent
        {
            EventType = "session.connected",
            Data = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["connectionCorrelationId"] = connectionCorrelationId
            }
        }, _clock);
        await _sessions.SaveSessionAsync(session, ct);

        var connectedPayload = new SessionEventPayload
        {
            EventType = "session.connected",
            Data = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["connectionCorrelationId"] = connectionCorrelationId
            }
        };

        var connectedEnvelope = BuildEnvelope(
            session.SessionId,
            WsTypes.SessionEvent,
            connectedPayload,
            correlationId: connectionCorrelationId,
            sequence: seq.Next());

        var connectedBytes = JsonSerializer.SerializeToUtf8Bytes(connectedEnvelope, _serializerOptions);
        await socket.SendAsync(connectedBytes, WebSocketMessageType.Text, true, ct);

        var buffer = new byte[32 * 1024];
        var builder = new StringBuilder();
        // 1) Outbound send lock (one per WS connection)
        var sendLock = new SemaphoreSlim(1, 1);
        // Reserved for future turn arbitration / interruption logic
        // private readonly SemaphoreSlim _sessionLock = new(1, 1);

        //sender
        async Task SendEnvelopeAsync(object envelope)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, _serializerOptions);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }

        // 2) Safe sender (serializes all outbound sends)
        async Task SafeSendEnvelopeAsync(object envelope)
        {
            await sendLock.WaitAsync(ct);
            try
            {
                await SendEnvelopeAsync(envelope);
            }
            finally
            {
                sendLock.Release();
            }
        }

        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            
            if (result.Count == 0 && result.EndOfMessage)
            {
                await HandleDisconnectAsync(session, socket, ct);
                break;
            }
            
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await HandleDisconnectAsync(session, socket, ct);
                break;
            }

            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (!result.EndOfMessage)
            {
                continue;
            }

            var rawMessage = builder.ToString();
            builder.Clear();

            await HandleMessageAsync(session, rawMessage, connectionCorrelationId, seq, SafeSendEnvelopeAsync, ct);
        }
    }

    private async Task HandleMessageAsync(ClassroomSession session, string rawMessage, string connectionCorrelationId, SequenceCounter seq, Func<object, Task> safeSendEnvelopeAsync, CancellationToken ct)
    {
        RawEnvelope? rawEnvelope;

        try
        {
            rawEnvelope = JsonSerializer.Deserialize<RawEnvelope>(rawMessage, _serializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON envelope for session {SessionId}", session.SessionId);
            return;
        }

        if (rawEnvelope is null)
        {
            _logger.LogWarning("Received null envelope for session {SessionId}", session.SessionId);
            return;
        }

        // ✅ SAFETY: ensure message type exists before routing
        if (string.IsNullOrWhiteSpace(rawEnvelope.MessageType))
        {
            _logger.LogWarning("Envelope missing MessageType for session {SessionId}", session.SessionId);

            return;
        }

        // FLOW correlation: prefer client-supplied correlationId.
        // If absent, fall back to client messageId (stable for this request/flow).
        // If even that is absent, fall back to the connectionCorrelationId (stable for the WS session thread).
        var flowCorrelationId =
            rawEnvelope.CorrelationId
            ?? rawEnvelope.MessageId
            ?? connectionCorrelationId;

        if (ct.IsCancellationRequested) return;
        switch (rawEnvelope.MessageType)
        {
            case WsTypes.SessionStart:
                await HandleSessionStartAsync(session, rawEnvelope, flowCorrelationId, seq, safeSendEnvelopeAsync, ct);
                return;

            case WsTypes.StudentInput:
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleStudentInputAsync(session, rawEnvelope, flowCorrelationId, seq, safeSendEnvelopeAsync, ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "StudentInput handler failed for session {SessionId}", session.SessionId);
                        }
                    });
                    return;
            default:
                _logger.LogWarning("Unhandled message type {MessageType} for session {SessionId}",
                    rawEnvelope.MessageType, session.SessionId);
                return;
        }
    }

    private async Task HandleSessionStartAsync(ClassroomSession session, RawEnvelope rawEnvelope, string correlationId, SequenceCounter seq, Func<object, Task> sendAsync, CancellationToken ct)
    {
        var payload = rawEnvelope.Payload.Deserialize<SessionStartPayload>(_serializerOptions);
        if (payload is null)
        {
            return;
        }

        if (session.Events.Any(e => e.EventType == "session.started"))
        {
            // re-emit started ack so retries are stable
            var alreadyPayload = new SessionEventPayload
            {
                EventType = "session.started",
                Data = new Dictionary<string, object> { ["lessonId"] = session.LessonId }
            };

            await sendAsync(BuildEnvelope(session.SessionId, WsTypes.SessionEvent, alreadyPayload, correlationId, seq.Next()));
            return;
        }

        session.RecordEvent(new SessionEvent
        {
            EventType = "session.started",
            Data = new Dictionary<string, object>
            {
                ["lessonId"] = payload.LessonId,
                ["studentId"] = payload.StudentId
            }
        }, _clock);

        await _sessions.SaveSessionAsync(session, ct);

        var evtPayload = new SessionEventPayload
        {
            EventType = "session.started",
            Data = new Dictionary<string, object>
            {
                ["lessonId"] = payload.LessonId
            }
        };

        var ackEnvelope = BuildEnvelope(session.SessionId, WsTypes.SessionEvent, evtPayload, correlationId, sequence: seq.Next());
        await sendAsync(ackEnvelope);
    }

    private async Task HandleStudentInputAsync(ClassroomSession session, RawEnvelope rawEnvelope, string correlationId, SequenceCounter seq, Func<object, Task> sendAsync, CancellationToken ct)
    {
        var payload = rawEnvelope.Payload.Deserialize<StudentInputPayload>(_serializerOptions);
        if (payload is null)
            return;

        // TurnId comes from client when provided; else server generates.
        var turnId = !string.IsNullOrWhiteSpace(payload.TurnId)
            ? payload.TurnId!
            : Guid.NewGuid().ToString("N");

        // Persist student input as auditable event
        session.RecordEvent(new SessionEvent
        {
            EventType = "student.input",
            Data = new Dictionary<string, object>
            {
                ["content"] = payload.Content,
                ["inputType"] = payload.Type.ToString(),
                ["turnId"] = turnId
            }
        }, _clock);

        await _sessions.SaveSessionAsync(session, ct);

        // Offset semantics: relative to when we started generating this teacher turn
        var sw = Stopwatch.StartNew();
        int OffsetMs() => (int)sw.ElapsedMilliseconds;

        var deltaIndex = 0; // 1-based in payload

        // 1) TeacherTurnStart
        var turnStart = new TeacherTurnStartPayload
        {
            TurnId = turnId,
            OffsetMs = 0,
            Reason = "student_input"
        };

        await sendAsync(BuildEnvelope(
            session.SessionId,
            WsTypes.TeacherTurnStart,
            turnStart,
            correlationId,
            sequence: seq.Next()));

        // 2) Micro-ack
        var delta1 = new TeacherTextDeltaPayload
        {
            TurnId = turnId,
            DeltaIndex = ++deltaIndex,
            Delta = "Okay — ",
            OffsetMs = OffsetMs(),
            IsFinal = false,
            Operation = TextDeltaOperation.Append
        };

        await sendAsync(BuildEnvelope(
            session.SessionId,
            WsTypes.TeacherTextDelta,
            delta1,
            correlationId,
            sequence: seq.Next()));

        // ✅ DEV-ONLY timing pulse
        #if DEBUG
            await Task.Delay(25, ct);
        #endif

        // 3) Partial thought
        var delta2 = new TeacherTextDeltaPayload
        {
            TurnId = turnId,
            DeltaIndex = ++deltaIndex,
            Delta = "let’s work through that together… ",
            OffsetMs = OffsetMs(),
            IsFinal = false,
            Operation = TextDeltaOperation.Append
        };

        await sendAsync(BuildEnvelope(
            session.SessionId,
            WsTypes.TeacherTextDelta,
            delta2,
            correlationId,
            sequence: seq.Next()));

        // ✅ DEV-ONLY thinking pulse
        #if DEBUG
            await Task.Delay(40, ct);
        #endif

        // 4) Orchestrator call
        var action = await _orchestrator.SelectNextActionAsync(new TeachingContext
        {
            SessionId = session.SessionId,
            LessonId = session.LessonId,
            StudentId = session.StudentId,
            StudentInput = payload.Content
        }, ct);

        // 5) Main content (final delta for now)
        var deltaMain = new TeacherTextDeltaPayload
        {
            TurnId = turnId,
            DeltaIndex = ++deltaIndex,
            Delta = action.Content,
            OffsetMs = OffsetMs(),
            IsFinal = true,
            Operation = TextDeltaOperation.Append
        };

        await sendAsync(BuildEnvelope(
            session.SessionId,
            WsTypes.TeacherTextDelta,
            deltaMain,
            correlationId,
            sequence: seq.Next()));

        // 6) TeacherTurnEnd
        var turnEnd = new TeacherTurnEndPayload
        {
            TurnId = turnId,
            OffsetMs = OffsetMs(),
            Outcome = "completed"
        };

        await sendAsync(BuildEnvelope(
            session.SessionId,
            WsTypes.TeacherTurnEnd,
            turnEnd,
            correlationId,
            sequence: seq.Next()));

        return;
    }

    private async Task HandleDisconnectAsync(ClassroomSession session, WebSocket socket, CancellationToken ct)
    {
        session.End("client.disconnected", _clock);
        await _sessions.SaveSessionAsync(session, ct);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", ct);
    }

    private WsEnvelope<TPayload> BuildEnvelope<TPayload>(string sessionId, string messageType, TPayload payload, string? correlationId, int? sequence)
        where TPayload : WsPayload
    {
        payload.SessionId ??= sessionId;
        payload.CreatedAt = _clock.UtcNow;

        return new WsEnvelope<TPayload>(
            Guid.NewGuid().ToString("N"),
            correlationId ?? Guid.NewGuid().ToString("N"),
            messageType,
            _clock.UtcNow,
            sequence,
            payload);
    }
}
