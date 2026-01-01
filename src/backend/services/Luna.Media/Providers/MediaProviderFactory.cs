using Luna.Media.Tts;
using Luna.Media.Stt;
using System;

namespace Luna.Media.Providers
{
    /// <summary>
    /// Provider factory for flexible TTS/STT implementation swapping.
    /// </summary>
    public interface IMediaProviderFactory
    {
        ITtsService CreateTtsProvider(string providerName);
        ISttService CreateSttProvider(string providerName);
    }

    public class MediaProviderFactory : IMediaProviderFactory
    {
        public ITtsService CreateTtsProvider(string providerName) =>
            providerName.ToLower() switch
            {
                "azure" => new AzureTtsService(),
                _ => throw new ArgumentException($"Unknown TTS provider: {providerName}")
            };

        public ISttService CreateSttProvider(string providerName) =>
            providerName.ToLower() switch
            {
                "azure" => new AzureSttService(),
                _ => throw new ArgumentException($"Unknown STT provider: {providerName}")
            };
    }
}
