import { ref, computed } from 'vue'

/**
 * REST response from POST /api/classroom/sessions
 */
interface CreateSessionResponse {
  sessionId: string
  webSocketUrl: string
}

/**
 * Client-side session model
 */
interface StudentSession {
  sessionId: string
  webSocketUrl: string
  studentId: string
  lessonId: string
  startedAt: string
  state: 'created' | 'active' | 'paused' | 'ended'
}

export const useSessionStore = () => {
  const currentSession = ref<StudentSession | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  /**
   * Create a classroom session (REST)
   * WS connection happens later.
   */
  const startSession = async (
    studentId: string,
    lessonId: string
  ): Promise<StudentSession> => {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch('/api/classroom/sessions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ studentId, lessonId }),
      })

      if (!response.ok) {
        const text = await response.text().catch(() => '')
        throw new Error(`Failed to create session (${response.status}): ${text}`)
      }

      const data = (await response.json()) as CreateSessionResponse

      const session: StudentSession = {
        sessionId: data.sessionId,
        webSocketUrl: data.webSocketUrl,
        studentId,
        lessonId,
        startedAt: new Date().toISOString(),
        state: 'created',
      }

      currentSession.value = session
      return session
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Mark session active AFTER:
   * - WS connected
   * - session.started event received
   */
  const markStarted = () => {
    if (!currentSession.value) return
    currentSession.value = {
      ...currentSession.value,
      state: 'active',
    }
  }

  /**
   * Alias for clarity if you want to call it explicitly
   */
  const markActive = () => {
    markStarted()
  }

  /**
   * Local-only state update (used on disconnect, etc.)
   */
  const markEnded = () => {
    if (!currentSession.value) return
    currentSession.value = {
      ...currentSession.value,
      state: 'ended',
    }
  }

  /**
   * End session (REST)
   */
  const endSession = async (reason: string): Promise<void> => {
    if (!currentSession.value) return
    isLoading.value = true
    error.value = null

    try {
      const res = await fetch(
        `/api/classroom/sessions/${currentSession.value.sessionId}/end`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ reason }),
        }
      )

      if (!res.ok) {
        const text = await res.text().catch(() => '')
        throw new Error(`Failed to end session (${res.status}): ${text}`)
      }

      currentSession.value = null
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  return {
    // state
    currentSession: computed(() => currentSession.value),
    isLoading: computed(() => isLoading.value),
    error: computed(() => error.value),

    // actions
    startSession,
    markStarted,
    markActive,
    markEnded,
    endSession,
  }
}