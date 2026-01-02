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

        // Mark connection event
        session.RecordEvent(new SessionEvent
        {
            EventType = "session.connected",
            Data = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId
            }
        }, _clock);
        await _sessions.SaveSessionAsync(session, ct);

        var connectedPayload = new SessionEventPayload
        {
            EventType = "session.connected",
            Data = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId
            },
            SequenceNumber = session.Events.Count
        };

        var connectedEnvelope = BuildEnvelope(
            session.SessionId,
            WsTypes.SessionEvent,
            connectedPayload,
            correlationId: null,
            sequence: session.Events.Count);

        var connectedBytes = JsonSerializer.SerializeToUtf8Bytes(connectedEnvelope, _serializerOptions);
        await socket.SendAsync(connectedBytes, WebSocketMessageType.Text, true, ct);

        var buffer = new byte[32 * 1024];
        var builder = new StringBuilder();

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

            var responses = await HandleMessageAsync(session, rawMessage, ct);
            foreach (var response in responses)
            {
                var payload = JsonSerializer.SerializeToUtf8Bytes(response, _serializerOptions);
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, ct);
            }
        }
    }

    private async Task<IEnumerable<object>> HandleMessageAsync(
        ClassroomSession session,
        string rawMessage,
        CancellationToken ct)
    {
        RawEnvelope? rawEnvelope;

        try
        {
            rawEnvelope = JsonSerializer.Deserialize<RawEnvelope>(rawMessage, _serializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON envelope for session {SessionId}", session.SessionId);
            return Array.Empty<object>();
        }

        if (rawEnvelope is null)
        {
            _logger.LogWarning("Received null envelope for session {SessionId}", session.SessionId);
            return Array.Empty<object>();
        }

        // ✅ SAFETY: ensure message type exists before routing
        if (string.IsNullOrWhiteSpace(rawEnvelope.MessageType))
        {
            _logger.LogWarning(
                "Envelope missing MessageType for session {SessionId}",
                session.SessionId);

            return Array.Empty<object>();
        }

        // ✅ CORRELATION: ensure we always have one
        var correlationId =
            rawEnvelope.CorrelationId
            ?? rawEnvelope.MessageId
            ?? Guid.NewGuid().ToString("N");

        switch (rawEnvelope.MessageType)
        {
            case WsTypes.SessionStart:
                return await HandleSessionStartAsync(session, rawEnvelope, correlationId, ct);

            case WsTypes.StudentInput:
                return await HandleStudentInputAsync(session, rawEnvelope, correlationId, ct);

            default:
                _logger.LogWarning(
                    "Unhandled message type {MessageType} for session {SessionId}",
                    rawEnvelope.MessageType,
                    session.SessionId);

                return Array.Empty<object>();
        }
    }

    private async Task<IEnumerable<object>> HandleSessionStartAsync(ClassroomSession session, RawEnvelope rawEnvelope, string correlationId,CancellationToken ct)
    {
        var payload = rawEnvelope.Payload.Deserialize<SessionStartPayload>(_serializerOptions);
        if (payload is null)
        {
            return Array.Empty<object>();
        }

        if (session.Events.Any(e => e.EventType == "session.started"))
        {
            return Array.Empty<object>(); // or return an ack
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
            },
            SequenceNumber = session.Events.Count
        };

        var ackEnvelope = BuildEnvelope(session.SessionId, WsTypes.SessionEvent, evtPayload, correlationId, session.Events.Count);
        return new object[] { ackEnvelope };
    }

    private async Task<IEnumerable<object>> HandleStudentInputAsync(ClassroomSession session, RawEnvelope rawEnvelope, string correlationId, CancellationToken ct)
    {
        var payload = rawEnvelope.Payload.Deserialize<StudentInputPayload>(_serializerOptions);
        if (payload is null)
        {
            return Array.Empty<object>();
        }

        session.RecordEvent(new SessionEvent
        {
            EventType = "student.input",
            Data = new Dictionary<string, object>
            {
                ["content"] = payload.Content,
                ["inputType"] = payload.Type.ToString()
            }
        }, _clock);

        await _sessions.SaveSessionAsync(session, ct);

        var action = await _orchestrator.SelectNextActionAsync(new TeachingContext
        {
            SessionId = session.SessionId,
            LessonId = session.LessonId,
            StudentId = session.StudentId,
            StudentInput = payload.Content
        }, ct);

        var teacherResponse = new TeacherResponsePayload
        {
            Content = action.Content,
            Type = ResponseType.TextExplanation,
            IsStreaming = false,
            Metadata = action.Metadata
        };

        var responseEnvelope = BuildEnvelope(session.SessionId, WsTypes.TeacherResponse, teacherResponse, correlationId, session.Events.Count);
        return new object[] { responseEnvelope };
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
