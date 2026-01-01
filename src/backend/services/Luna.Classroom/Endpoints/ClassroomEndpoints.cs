namespace Luna.Classroom.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using Luna.Classroom.Persistence;
using Luna.Classroom.Realtime;
using Luna.Classroom.Sessions;
using Luna.SharedKernel.Time;

public static class ClassroomEndpoints
{
    public static void MapClassroomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/classroom")
            .WithName("Classroom");

        group.MapPost("/sessions", CreateSessionAsync)
            .WithName("CreateSession")
            .Produces<CreateSessionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/sessions/{sessionId}", GetSessionAsync)
            .WithName("GetSession")
            .Produces<GetSessionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/sessions/{sessionId}/end", EndSessionAsync)
            .WithName("EndSession")
            .Produces(StatusCodes.Status204NoContent);

        app.MapGet("/ws/classroom/sessions/{sessionId}", AcceptWebSocketAsync)
            .WithName("ClassroomWebSocket");
    }

    private static async Task<IResult> CreateSessionAsync(
        CreateSessionRequest request,
        ISessionRepository sessions,
        ISystemClock clock,
        HttpContext ctx,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.StudentId) || string.IsNullOrWhiteSpace(request.LessonId))
        {
            return Results.BadRequest("studentId and lessonId are required");
        }

        var session = ClassroomSession.Create(request.StudentId, request.LessonId, clock);

        session.RecordEvent(new SessionEvent
        {
            EventType = "session.created",
            Data = new Dictionary<string, object>
            {
                ["lessonId"] = request.LessonId,
                ["studentId"] = request.StudentId
            }
        }, clock);

        await sessions.SaveSessionAsync(session, ct);

        var wsScheme = ctx.Request.IsHttps ? "wss" : "ws";
        var wsUrl = $"{wsScheme}://{ctx.Request.Host}/ws/classroom/sessions/{session.SessionId}";

        var response = new CreateSessionResponse(session.SessionId, wsUrl);
        return Results.Created($"/api/classroom/sessions/{session.SessionId}", response);
    }

    private static async Task<IResult> GetSessionAsync(
        string sessionId,
        ISessionRepository sessions,
        CancellationToken ct)
    {
        var session = await sessions.GetSessionAsync(sessionId, ct);
        if (session is null)
        {
            return Results.NotFound();
        }

        var response = new GetSessionResponse(
            session.SessionId,
            session.State.ToString(),
            session.Events.Select(e => new SessionEventDto(e.Sequence, e.EventType, e.Timestamp, e.Data)).ToList());

        return Results.Ok(response);
    }

    private static async Task<IResult> EndSessionAsync(
        string sessionId,
        ISessionRepository sessions,
        ISystemClock clock,
        CancellationToken ct)
    {
        var session = await sessions.GetSessionAsync(sessionId, ct);
        if (session is null)
        {
            return Results.NotFound();
        }

        session.End("ended.via.api", clock);
        await sessions.SaveSessionAsync(session, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AcceptWebSocketAsync(
    string sessionId,
    HttpContext ctx,
    IWebSocketHandler handler,
    CancellationToken ct)
    {
        if (!ctx.WebSockets.IsWebSocketRequest)
            return Results.BadRequest("WebSocket connection expected");

        using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
        await handler.HandleConnectionAsync(sessionId, socket, ct);

        return Results.Empty;
    }   
}

public record CreateSessionRequest(string StudentId, string LessonId);
public record CreateSessionResponse(string SessionId, string WebSocketUrl);
public record GetSessionResponse(string SessionId, string State, List<SessionEventDto> Events);

public record SessionEventDto(int Sequence, string EventType, DateTime Timestamp, Dictionary<string, object> Data);