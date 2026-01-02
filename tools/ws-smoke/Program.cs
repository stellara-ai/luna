// Program.cs
// WS Smoke Test (REST + WebSocket) for Luna Classroom
//
// Usage:
//   # Option A: run against default http://localhost:5050
//   dotnet run
//
//   # Option B: specify API base url
//   export LUNA_API_BASE_URL="http://localhost:5050"
//   dotnet run
//
// Notes:
// - This program creates a session via REST, then connects via WebSocket,
//   sends session_start + student_input, and prints the responses.

using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

static string NewId() => Guid.NewGuid().ToString("N");

var apiBaseUrl =
    Environment.GetEnvironmentVariable("LUNA_API_BASE_URL")
    ?? "http://localhost:5050";

var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    PropertyNameCaseInsensitive = true
};

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// ------------------------------
// 1) Create session (REST)
// ------------------------------
Console.WriteLine($"API Base URL: {apiBaseUrl}");
Console.WriteLine("Creating classroom session...");

var createResponse = await http.PostAsJsonAsync(
    "/api/classroom/sessions",
    new
    {
        studentId = "stu-smoke",
        lessonId = "lesson-smoke"
    });

createResponse.EnsureSuccessStatusCode();

var createResult = await createResponse.Content.ReadFromJsonAsync<CreateSessionResponse>(serializerOptions);
if (createResult is null)
    throw new Exception("Failed to parse create session response.");

var sessionId = createResult.SessionId;
var wsUrl = createResult.WebSocketUrl;

if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(wsUrl))
    throw new Exception("Create session response missing sessionId or webSocketUrl.");

Console.WriteLine($"SessionId: {sessionId}");
Console.WriteLine($"WS URL:    {wsUrl}");
Console.WriteLine();

// ------------------------------
// 2) Connect WebSocket
// ------------------------------
using var socket = new ClientWebSocket();
await socket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

Console.WriteLine($"Connected: {wsUrl}");
Console.WriteLine();

async Task SendAsync(object obj)
{
    var bytes = JsonSerializer.SerializeToUtf8Bytes(obj, serializerOptions);
    await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
}

async Task<string> ReceiveTextAsync()
{
    var buffer = new byte[32 * 1024];
    var sb = new StringBuilder();

    while (true)
    {
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
            return "<CLOSE>";

        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        if (result.EndOfMessage)
            return sb.ToString();
    }
}

// ------------------------------
// 3) Read initial server message (session.connected)
// ------------------------------
var first = await ReceiveTextAsync();
Console.WriteLine("RECV (connected):");
Console.WriteLine(first);
Console.WriteLine();

// ------------------------------
// 4) Send SessionStart
// ------------------------------
var correlationId = NewId();

var startEnvelope = new
{
    messageId = NewId(),
    correlationId = correlationId,
    messageType = "v1.classroom.session_start",
    timestamp = DateTime.UtcNow,
    sequenceNumber = 1,
    payload = new
    {
        sessionId = sessionId,
        lessonId = "lesson-smoke",
        studentId = "stu-smoke"
    }
};

Console.WriteLine("SEND (session_start)...");
await SendAsync(startEnvelope);

// Expect a SessionEvent ack (e.g. session.started)
var started = await ReceiveTextAsync();
Console.WriteLine("RECV:");
Console.WriteLine(started);
Console.WriteLine();

// ------------------------------
// 5) Send StudentInput
// ------------------------------
var inputEnvelope = new
{
    messageId = NewId(),
    correlationId = correlationId,
    messageType = "v1.classroom.student_input",
    timestamp = DateTime.UtcNow,
    sequenceNumber = 2,
    payload = new
    {
        sessionId = sessionId,
        content = "Hi Luna — can you explain fractions simply?",
        type = "Text"
        // If enum string fails in your server deserializer, use: type = 0
    }
};

Console.WriteLine("SEND (student_input)...");
await SendAsync(inputEnvelope);

// ------------------------------
// 6) Receive TeacherResponse (or streamed messages if you later add them)
// ------------------------------
var response = await ReceiveTextAsync();
Console.WriteLine("RECV (expect teacher_response or deltas):");
Console.WriteLine(response);
Console.WriteLine();

// ------------------------------
// 7) Close
// ------------------------------
Console.WriteLine("Done. Closing...");
await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

// ------------------------------
// DTOs
// ------------------------------
public sealed record CreateSessionResponse(string SessionId, string WebSocketUrl);