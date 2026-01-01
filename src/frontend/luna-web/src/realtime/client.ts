/**
 * WebSocket connection manager for real-time classroom sessions.
 * Handles message serialization, envelope validation, and reconnection.
 */

import { WsEnvelope, WsTypes } from '@/types/realtime'

export class RealtimeClient {
  private ws: WebSocket | null = null
  private url: string
  private sessionId: string
  private correlationId: string
  private messageHandlers: Map<string, (payload: any) => void> = new Map()
  private reconnectAttempts = 0
  private maxReconnectAttempts = 5

  constructor(url: string, sessionId: string) {
    this.url = url
    this.sessionId = sessionId
    this.correlationId = crypto.randomUUID()
  }

  async connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        this.ws = new WebSocket(this.url)

        this.ws.onopen = () => {
          console.log('[RealtimeClient] Connected')
          this.reconnectAttempts = 0
          resolve()
        }

        this.ws.onmessage = (event) => {
          this.handleMessage(event.data)
        }

        this.ws.onerror = (error) => {
          console.error('[RealtimeClient] Error:', error)
          reject(error)
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

  private handleMessage(data: string): void {
    try {
      const envelope: WsEnvelope<any> = JSON.parse(data)
      const handler = this.messageHandlers.get(envelope.messageType)
      if (handler) {
        handler(envelope.payload)
      }
    } catch (error) {
      console.error('[RealtimeClient] Failed to parse message:', error)
    }
  }

  on(messageType: string, handler: (payload: any) => void): void {
    this.messageHandlers.set(messageType, handler)
  }

  send<T>(messageType: string, payload: T): void {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
      console.error('[RealtimeClient] WebSocket not connected')
      return
    }

    const envelope: WsEnvelope<T> = {
      messageId: crypto.randomUUID(),
      correlationId: this.correlationId,
      messageType,
      timestamp: new Date().toISOString(),
      payload,
    }

    this.ws.send(JSON.stringify(envelope))
  }

  disconnect(): void {
    if (this.ws) {
      this.ws.close()
      this.ws = null
    }
  }

  private attemptReconnect(): void {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++
      const delay = Math.pow(2, this.reconnectAttempts) * 1000
      console.log(`[RealtimeClient] Reconnecting in ${delay}ms (attempt ${this.reconnectAttempts})`)
      setTimeout(() => this.connect().catch(console.error), delay)
    }
  }
}
