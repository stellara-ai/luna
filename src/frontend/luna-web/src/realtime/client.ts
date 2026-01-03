/**
 * WebSocket connection manager for real-time classroom sessions.
 * Contract-aligned with LUNE_CONTEXT.md §2.1:
 * - streaming-first
 * - per-turn TurnId
 * - monotonic sequenceNumber
 * - reconnect + re-send session_start (idempotent server-side)
 */

import type { WsEnvelope } from '@/types/realtime'

type AnyHandler = (envelope: WsEnvelope<any>) => void
type PayloadHandler<T = any> = (payload: T, envelope: WsEnvelope<T>) => void

const NewId = () => crypto.randomUUID().replace(/-/g, '')

export class RealtimeClient {
  private ws: WebSocket | null = null
  private url: string
  private sessionId: string

  // Correlation:
  // - stable for the lifetime of the socket connection
  private connectionCorrelationId: string = NewId()

  // Monotonic per client lifetime (don’t reset on reconnect)
  private seq = 0

  // Handlers
  private handlersByType: Map<string, Set<PayloadHandler>> = new Map()
  private anyHandlers: Set<AnyHandler> = new Set()

  // Reconnect
  private reconnectAttempts = 0
  private maxReconnectAttempts = 5
  private reconnectTimer: number | null = null

  // Session start payload we can replay after reconnect
  private lastSessionStartPayload: any | null = null

  constructor(url: string, sessionId: string) {
    this.url = url
    this.sessionId = sessionId
  }

  isConnected(): boolean {
    return !!this.ws && this.ws.readyState === WebSocket.OPEN
  }

  async connect(): Promise<void> {
    // If already open, no-op
    if (this.isConnected()) return

    return new Promise((resolve, reject) => {
      let settled = false

      try {
        this.ws = new WebSocket(this.url)

        this.ws.onopen = () => {
          settled = true
          console.log('[RealtimeClient] Connected')
          this.reconnectAttempts = 0
          this.connectionCorrelationId = NewId()

          // If we have a prior session_start, re-send it (idempotent server-side).
          if (this.lastSessionStartPayload) {
            this.sendWithFlowId('v1.classroom.session_start', this.lastSessionStartPayload, {
              flowCorrelationId: NewId(),
            })
          }

          resolve()
        }

        this.ws.onmessage = (event) => {
          this.handleMessage(event.data)
        }

        this.ws.onerror = (error) => {
          console.error('[RealtimeClient] Error:', error)
          // Only reject connect() if not opened yet
          if (!settled) reject(error)
        }

        this.ws.onclose = () => {
          console.log('[RealtimeClient] Disconnected')
          this.attemptReconnect()
        }
      } catch (error) {
        reject(error)
      }
    })
  }

  async waitForOpen(timeoutMs = 2500): Promise<void> {
    if (this.isConnected()) return

    const start = Date.now()
    while (!this.isConnected()) {
      if (Date.now() - start > timeoutMs) {
        throw new Error('WebSocket did not open in time')
      }
      await new Promise((r) => setTimeout(r, 25))
    }
  }

  /**
   * Subscribe to a messageType. Multiple handlers allowed.
   * Returns an unsubscribe function.
   */
  on<T = any>(messageType: string, handler: PayloadHandler<T>): () => void {
    if (!this.handlersByType.has(messageType)) {
      this.handlersByType.set(messageType, new Set())
    }
    const set = this.handlersByType.get(messageType)!
    set.add(handler as PayloadHandler)

    return () => {
      set.delete(handler as PayloadHandler)
      if (set.size === 0) this.handlersByType.delete(messageType)
    }
  }

  /**
   * Subscribe to all messages (useful for logging/debugging).
   * Returns an unsubscribe function.
   */
  onAny(handler: AnyHandler): () => void {
    this.anyHandlers.add(handler)
    return () => this.anyHandlers.delete(handler)
  }

  private handleMessage(data: string | ArrayBuffer): void {
    try {
      const text =
        typeof data === 'string'
          ? data
          : new TextDecoder('utf-8').decode(new Uint8Array(data))

      const envelope: WsEnvelope<any> = JSON.parse(text)

      // fire "any" handlers first
      for (const h of this.anyHandlers) h(envelope)

      const set = this.handlersByType.get(envelope.messageType)
      if (!set || set.size === 0) return

      for (const handler of set) {
        try {
          handler(envelope.payload, envelope)
        } catch (err) {
          console.error('[RealtimeClient] handler error:', err)
        }
      }
    } catch (error) {
      console.error('[RealtimeClient] Failed to parse message:', error)
    }
  }

  /**
   * For session_start we store payload so we can replay on reconnect.
   */
  sendSessionStart(payload: any): void {
    this.lastSessionStartPayload = payload

    // Ensure sessionId is included if caller forgot
    if (payload && !payload.sessionId) payload.sessionId = this.sessionId

    this.sendWithFlowId('v1.classroom.session_start', payload, {
      flowCorrelationId: NewId(),
    })
  }

  /**
   * Create a client-side TurnId (recommended) for StudentInput.
   */
  newTurnId(): string {
    return NewId()
  }

  /**
   * Send student_input with an explicit TurnId.
   * Caller can pass a flowCorrelationId or we’ll generate one per turn.
   */
  sendStudentInput(payload: any, opts?: { flowCorrelationId?: string }): void {
    // Ensure sessionId is included if caller forgot
    if (payload && !payload.sessionId) payload.sessionId = this.sessionId

    const flowCorrelationId = opts?.flowCorrelationId ?? NewId()
    this.sendWithFlowId('v1.classroom.student_input', payload, { flowCorrelationId })
  }

  /**
   * Low-level send with explicit flowCorrelationId.
   */
  sendWithFlowId<T>(
    messageType: string,
    payload: T,
    opts: { flowCorrelationId: string }
  ): void {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
      console.error('[RealtimeClient] WebSocket not connected')
      return
    }

    const envelope: WsEnvelope<T> = {
      messageId: NewId(),
      correlationId: opts.flowCorrelationId,
      messageType,
      timestamp: new Date().toISOString(),
      sequenceNumber: ++this.seq,
      payload,
    }

    this.ws.send(JSON.stringify(envelope))
  }

  disconnect(): void {
    if (this.reconnectTimer) {
      window.clearTimeout(this.reconnectTimer)
      this.reconnectTimer = null
    }
    if (this.ws) {
      this.ws.close()
      this.ws = null
    }
  }

  private attemptReconnect(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) return

    this.reconnectAttempts++
    const delay = Math.pow(2, this.reconnectAttempts) * 250
    console.log(`[RealtimeClient] Reconnecting in ${delay}ms (attempt ${this.reconnectAttempts})`)

    if (this.reconnectTimer) window.clearTimeout(this.reconnectTimer)
    this.reconnectTimer = window.setTimeout(() => {
      this.connect().catch(console.error)
    }, delay)
  }
}