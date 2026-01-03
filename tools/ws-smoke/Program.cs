// Program.cs
// WS Smoke Test (REST + WebSocket) for Luna Classroom (Streaming-only / Option A)
//
// Usage:
//   dotnet run
//   export LUNA_API_BASE_URL="http://localhost:5050" && dotnet run

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

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

Console.WriteLine($"API Base URL: {apiBaseUrl}");
Console.WriteLine("Creating classroom session...");

var createResponse = await http.PostAsJsonAsync(
    "/api/classroom/sessions",
    new { studentId = "stu-smoke", lessonId = "lesson-smoke" });

createResponse.EnsureSuccessStatusCode();

var createResult = await createResponse.Content.ReadFromJsonAsync<CreateSessionResponse>(serializerOptions)
    ?? throw new Exception("Failed to parse create session response.");

var sessionId = createResult.SessionId;
var wsUrl = createResult.WebSocketUrl;

if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(wsUrl))
    throw new Exception("Create session response missing sessionId or webSocketUrl.");

Console.WriteLine($"SessionId: {sessionId}");
Console.WriteLine($"WS URL:    {wsUrl}");
Console.WriteLine();

// ------------------------------
// WebSocket connect
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

async Task<string> ReceiveTextAsync(CancellationToken ct)
{
    var buffer = new byte[32 * 1024];
    var sb = new StringBuilder();

    while (true)
    {
        var result = await socket.ReceiveAsync(buffer, ct);
        if (result.MessageType == WebSocketMessageType.Close)
            return "<CLOSE>";

        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        if (result.EndOfMessage)
            return sb.ToString();
    }
}

static WsEnvelope? ParseEnvelope(string json)
{
    try { return JsonSerializer.Deserialize<WsEnvelope>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true }); }
    catch { return null; }
}

static string? GetTurnId(WsEnvelope env)
{
    if (env.Payload.ValueKind != JsonValueKind.Object) return null;
    if (env.Payload.TryGetProperty("turnId", out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
    if (env.Payload.TryGetProperty("TurnId", out var v2) && v2.ValueKind == JsonValueKind.String) return v2.GetString();
    return null;
}

static int? GetDeltaIndex(WsEnvelope env)
{
    if (env.Payload.ValueKind != JsonValueKind.Object) return null;
    if (env.Payload.TryGetProperty("deltaIndex", out var v) && v.ValueKind == JsonValueKind.Number) return v.GetInt32();
    if (env.Payload.TryGetProperty("DeltaIndex", out var v2) && v2.ValueKind == JsonValueKind.Number) return v2.GetInt32();
    return null;
}

static string GetDeltaText(WsEnvelope env)
{
    if (env.Payload.ValueKind != JsonValueKind.Object) return "";
    if (env.Payload.TryGetProperty("delta", out var v) && v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
    if (env.Payload.TryGetProperty("Delta", out var v2) && v2.ValueKind == JsonValueKind.String) return v2.GetString() ?? "";
    return "";
}

static void AssertMonotonicSequence(List<WsEnvelope> envs, string label)
{
    int? prev = null;
    foreach (var e in envs)
    {
        if (e.SequenceNumber is null) continue;
        if (prev is not null && e.SequenceNumber <= prev)
            throw new Exception($"Non-monotonic envelope sequenceNumber in {label}: {e.SequenceNumber} after {prev}");
        prev = e.SequenceNumber;
    }
}

static void PrintSummary(List<WsEnvelope> envs)
{
    foreach (var e in envs)
    {
        var tid = GetTurnId(e);
        var di = GetDeltaIndex(e);
        int? offset = null;

        if (e.Payload.ValueKind == JsonValueKind.Object)
        {
            if (e.Payload.TryGetProperty("offsetMs", out var o) && o.ValueKind == JsonValueKind.Number) offset = o.GetInt32();
            else if (e.Payload.TryGetProperty("OffsetMs", out var o2) && o2.ValueKind == JsonValueKind.Number) offset = o2.GetInt32();
        }

        Console.WriteLine(
            $"- {e.MessageType}  seq={e.SequenceNumber ?? -1} corr={e.CorrelationId}" +
            (tid is not null ? $" turnId={tid}" : "") +
            (di is not null ? $" deltaIndex={di}" : "") +
            (offset is not null ? $" offsetMs={offset}" : "")
        );
    }
}

static Dictionary<string, string> AssembleTranscripts(List<WsEnvelope> envs)
{
    // turnId -> list of (deltaIndex,text)
    var map = new Dictionary<string, List<(int idx, string text)>>();

    foreach (var e in envs)
    {
        if (!string.Equals(e.MessageType, "v1.classroom.teacher_text_delta", StringComparison.OrdinalIgnoreCase))
            continue;

        var tid = GetTurnId(e);
        var idx = GetDeltaIndex(e);
        if (tid is null || idx is null) continue;

        if (!map.TryGetValue(tid, out var list))
        {
            list = new List<(int, string)>();
            map[tid] = list;
        }

        list.Add((idx.Value, GetDeltaText(e)));
    }

    var result = new Dictionary<string, string>();
    foreach (var (tid, list) in map)
    {
        var assembled = string.Concat(list.OrderBy(x => x.idx).Select(x => x.text));
        result[tid] = assembled;
    }

    return result;
}

// ------------------------------
// 1) Read initial server message(s) (session.connected)
// ------------------------------
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var firstRaw = await ReceiveTextAsync(cts.Token);
    Console.WriteLine("RECV (connected):");
    Console.WriteLine(firstRaw);
    Console.WriteLine();
}

// ------------------------------
// 2) Send session_start
// ------------------------------
var connectionCorrelationId = NewId(); // you may choose to keep this stable for the run
var sessionStartEnvelope = new
{
    messageId = NewId(),
    correlationId = connectionCorrelationId,
    messageType = "v1.classroom.session_start",
    timestamp = DateTime.UtcNow,
    sequenceNumber = 1,
    payload = new { sessionId, lessonId = "lesson-smoke", studentId = "stu-smoke" }
};

Console.WriteLine("SEND (session_start)...");
await SendAsync(sessionStartEnvelope);

{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var startedRaw = await ReceiveTextAsync(cts.Token);
    Console.WriteLine("RECV (started):");
    Console.WriteLine(startedRaw);
    Console.WriteLine();
}

// ------------------------------
// 3) Send TWO student_input messages back-to-back (overlap test)
// ------------------------------
var inputTurnIdA = NewId();
var inputTurnIdB = NewId();

Console.WriteLine($"Client inputTurnIdA: {inputTurnIdA}");
Console.WriteLine($"Client inputTurnIdB: {inputTurnIdB}");

var studentInputA = new
{
    messageId = NewId(),
    correlationId = connectionCorrelationId, // keep same correlationId like your Node test (optional)
    messageType = "v1.classroom.student_input",
    timestamp = DateTime.UtcNow,
    sequenceNumber = 2,
    payload = new
    {
        sessionId,
        turnId = inputTurnIdA,
        content = "Hi Luna — can you explain fractions simply?",
        type = "Text"
    }
};

var studentInputB = new
{
    messageId = NewId(),
    correlationId = connectionCorrelationId,
    messageType = "v1.classroom.student_input",
    timestamp = DateTime.UtcNow,
    sequenceNumber = 3,
    payload = new
    {
        sessionId,
        turnId = inputTurnIdB,
        content = "Now explain decimals simply too.",
        type = "Text"
    }
};

Console.WriteLine("SEND (student_input A)...");
await SendAsync(studentInputA);
Console.WriteLine("SEND (student_input B)...");
await SendAsync(studentInputB);

// ------------------------------
// 4) Collect replies until we see teacher_turn_end for BOTH turnIds
// (idle timeout + hard timeout like Node)
// ------------------------------
var expected = new HashSet<string> { inputTurnIdA, inputTurnIdB };
var ended = new HashSet<string>();
var received = new List<WsEnvelope>();

var idleTimeout = TimeSpan.FromMilliseconds(900);
var hardTimeout = TimeSpan.FromSeconds(6);

var hardCts = new CancellationTokenSource(hardTimeout);
var idleCts = new CancellationTokenSource(idleTimeout);

void ResetIdle()
{
    idleCts.Cancel();
    idleCts.Dispose();
    idleCts = new CancellationTokenSource(idleTimeout);
}

Console.WriteLine("RECV (reply batch):");
try
{
    while (!hardCts.IsCancellationRequested && !idleCts.IsCancellationRequested)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(hardCts.Token, idleCts.Token);
        string raw;

        try
        {
            raw = await ReceiveTextAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        if (raw == "<CLOSE>") break;

        var env = ParseEnvelope(raw);
        if (env is null) continue;

        received.Add(env);

        // update end-tracking
        if (string.Equals(env.MessageType, "v1.classroom.teacher_turn_end", StringComparison.OrdinalIgnoreCase))
        {
            var tid = GetTurnId(env);
            if (!string.IsNullOrWhiteSpace(tid))
                ended.Add(tid);
        }

        // stop condition: both turnIds ended
        if (ended.Contains(inputTurnIdA) && ended.Contains(inputTurnIdB))
            break;

        ResetIdle();
    }
}
finally
{
    hardCts.Dispose();
    idleCts.Dispose();
}

AssertMonotonicSequence(received, "reply batch");

// Echo checks
var echoedA = received.Any(m => GetTurnId(m) == inputTurnIdA);
var echoedB = received.Any(m => GetTurnId(m) == inputTurnIdB);
Console.WriteLine($"Echoed inputTurnIdA? {(echoedA ? "✅ yes" : "❌ no")}");
Console.WriteLine($"Echoed inputTurnIdB? {(echoedB ? "✅ yes" : "❌ no")}");

// Streaming contract checks (Option A)
var hasTurnStart = received.Any(m => m.MessageType == "v1.classroom.teacher_turn_start");
var hasTurnEnd = received.Any(m => m.MessageType == "v1.classroom.teacher_turn_end");
var hasAnyDelta = received.Any(m => m.MessageType == "v1.classroom.teacher_text_delta");

if (!hasTurnStart || !hasTurnEnd || !hasAnyDelta)
{
    Console.Error.WriteLine("❌ Missing streaming messages (need turn_start, text_delta, turn_end).");
    Environment.ExitCode = 1;
}

if (!echoedA || !echoedB)
{
    Console.Error.WriteLine("❌ Server did not echo both input turnIds somewhere in streaming messages.");
    Environment.ExitCode = 1;
}

// Per-turn: must have start + end + >= 1 delta
foreach (var tid in expected)
{
    var hasS = received.Any(m => m.MessageType == "v1.classroom.teacher_turn_start" && GetTurnId(m) == tid);
    var hasE = received.Any(m => m.MessageType == "v1.classroom.teacher_turn_end" && GetTurnId(m) == tid);
    var hasD = received.Any(m => m.MessageType == "v1.classroom.teacher_text_delta" && GetTurnId(m) == tid);

    if (!hasS || !hasE || !hasD)
    {
        Console.Error.WriteLine($"❌ Turn {tid} missing start/end/deltas (start={hasS}, end={hasE}, delta={hasD})");
        Environment.ExitCode = 1;
    }
}

// Print assembled transcripts
var transcripts = AssembleTranscripts(received);
if (transcripts.Count > 0)
{
    Console.WriteLine("\nAssembled turn transcript(s):");
    foreach (var (tid, text) in transcripts)
    {
        Console.WriteLine($"\nTurn: {tid}");
        Console.WriteLine(text);
    }
    Console.WriteLine();
}

// Summary
PrintSummary(received);
Console.WriteLine();

var okTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "v1.classroom.teacher_turn_start",
    "v1.classroom.teacher_turn_end",
    "v1.classroom.teacher_text_delta",
    "v1.classroom.teacher_audio_chunk",
    "v1.classroom.teacher_mark",
    "v1.classroom.session_event",
    "v1.classroom.error",
    "v1.classroom.ping",
    "v1.classroom.pong"
};

var unexpected = received.Where(m => !okTypes.Contains(m.MessageType ?? "")).ToList();
if (unexpected.Count > 0)
{
    Console.Error.WriteLine("❌ Unexpected message types received:");
    foreach (var m in unexpected)
        Console.Error.WriteLine($"- {m.MessageType}");
    Environment.ExitCode = 1;
}

Console.WriteLine(Environment.ExitCode == 0
    ? "✅ WS streaming-ready smoke test passed."
    : "❌ WS streaming smoke test had failures.");

Console.WriteLine("Done. Closing...");
await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

// ------------------------------
// DTOs
// ------------------------------
public sealed record CreateSessionResponse(string SessionId, string WebSocketUrl);

public sealed class WsEnvelope
{
    public string? MessageId { get; set; }
    public string? CorrelationId { get; set; }
    public string? MessageType { get; set; }
    public DateTime? Timestamp { get; set; }
    public int? SequenceNumber { get; set; }
    public JsonElement Payload { get; set; }
}