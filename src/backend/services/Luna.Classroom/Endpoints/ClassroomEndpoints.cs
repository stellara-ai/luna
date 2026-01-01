namespace Luna.Classroom.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class ClassroomEndpoints
{
    public static void MapClassroomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/classroom")
            .WithName("Classroom");

        group.MapPost("/sessions", CreateSessionAsync)
            .WithName("CreateSession")
            .Produces<CreateSessionResponse>(StatusCodes.Status201Created);

        group.MapGet("/sessions/{sessionId}", GetSessionAsync)
            .WithName("GetSession")
            .Produces<GetSessionResponse>(StatusCodes.Status200OK);

        group.MapPost("/sessions/{sessionId}/end", EndSessionAsync)
            .WithName("EndSession")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    private static Task<GetSessionResponse> GetSessionAsync(string sessionId, CancellationToken ct)
        => throw new NotImplementedException();

    private static Task EndSessionAsync(string sessionId, CancellationToken ct)
        => throw new NotImplementedException();
}

public record CreateSessionRequest(string StudentId, string LessonId);
public record CreateSessionResponse(string SessionId, string WebSocketUrl);
public record GetSessionResponse(string SessionId, string State, List<object> Events);