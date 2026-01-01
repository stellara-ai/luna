/**
 * Application state management (Composition API)
 */

import { ref, computed } from 'vue'

interface StudentSession {
  sessionId: string
  studentId: string
  lessonId: string
  startedAt: string
  state: 'created' | 'active' | 'paused' | 'ended'
}

export const useSessionStore = () => {
  const currentSession = ref<StudentSession | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  const startSession = async (studentId: string, lessonId: string): Promise<StudentSession> => {
    isLoading.value = true
    error.value = null
    try {
      const response = await fetch('/api/classroom/sessions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ studentId, lessonId }),
      })
      const data = await response.json()
      currentSession.value = data
      return data
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const endSession = async (reason: string): Promise<void> => {
    if (!currentSession.value) return
    isLoading.value = true
    try {
      await fetch(`/api/classroom/sessions/${currentSession.value.sessionId}/end`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason }),
      })
      currentSession.value = null
    } finally {
      isLoading.value = false
    }
  }

  return {
    currentSession: computed(() => currentSession.value),
    isLoading: computed(() => isLoading.value),
    error: computed(() => error.value),
    startSession,
    endSession,
  }
}
