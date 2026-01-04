/**
 * AudioWorklet processor for low-latency audio capture.
 * Posts Float32Array frames to main thread for downsampling/encoding.
 */

class PcmCaptureProcessor extends AudioWorkletProcessor {
  constructor() {
    super()
  }

  process(inputs, outputs, parameters) {
    const input = inputs[0]
    if (!input || !input.length) return true

    // Get first channel (we'll downsample to mono in main thread)
    const channelData = input[0]
    if (!channelData || channelData.length === 0) return true

    // Copy to new array and post to main thread
    const frame = new Float32Array(channelData)
    this.port.postMessage({ samples: frame.buffer }, [frame.buffer])

    return true
  }
}

registerProcessor('pcm-capture-processor', PcmCaptureProcessor)
