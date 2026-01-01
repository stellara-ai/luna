namespace Luna.Classroom.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// HTTP endpoints for classroom operations.
/// WebSocket upgrade happens here; real-time messages on upgraded connection.
/// </summary>
public static class ClassroomEndpoints
{
    public static void MapClassroomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/classroom")
            .WithName("Classroom")
            .WithOpenApi();

        group.MapPost("/sessions", CreateSessionAsync)
            .WithName("CreateSession")
            .Produces<CreateSessionResponse>(StatusCodes.Status201Created);

        group.MapGet("/sessions/{sessionId}", GetSessionAsync)
            .WithName("GetSession")
            .Produces<GetSessionResponse>();

        group.MapPost("/sessions/{sessionId}/end", EndSessionAsync)
            .WithName("EndSession")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<CreateSessionResponse> CreateSessionAsync(
        CreateSessionRequest request,
        CancellationToken ct)
    {
        // Create new session
        throw new NotImplementedException();
    }

    private static async Task<GetSessionResponse> GetSessionAsync(
        string sessionId,
        CancellationToken ct)
    {
        // Retrieve session
        throw new NotImplementedException();
    }

    private static async Task EndSessionAsync(
        string sessionId,
        CancellationToken ct)
    {
        // End session
        throw new NotImplementedException();
    }
}

public record CreateSessionRequest(string StudentId, string LessonId);
public record CreateSessionResponse(string SessionId, string WebSocketUrl);
public record GetSessionResponse(string SessionId, string State, List<object> Events);
