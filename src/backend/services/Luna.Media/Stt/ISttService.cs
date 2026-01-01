namespace Luna.Media.Stt;

/// <summary>
/// Speech-to-Text abstraction.
/// Multiple provider implementations (Azure, Google, Whisper, etc.)
/// </summary>
public interface ISttService
{
    Task<string> TranscribeAsync(Stream audioStream, SttOptions options, CancellationToken ct);
}

public record SttOptions(string Language = "en-US", string? Model = null);

public class AzureSttService : ISttService
{
    public async Task<string> TranscribeAsync(Stream audioStream, SttOptions options, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
