namespace Luna.Media.Tts;

/// <summary>
/// Text-to-Speech abstraction.
/// Multiple provider implementations (Azure, Google, ElevenLabs, etc.)
/// </summary>
public interface ITtsService
{
    Task<AudioStream> SynthesizeAsync(string text, TtsOptions options, CancellationToken ct);
}

public record TtsOptions(string Voice = "default", float Speed = 1.0f, float Pitch = 1.0f);
public record AudioStream(Stream Data, string MimeType);

public class AzureTtsService : ITtsService
{
    public async Task<AudioStream> SynthesizeAsync(string text, TtsOptions options, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
