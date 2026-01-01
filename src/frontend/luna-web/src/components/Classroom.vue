<template>
  <div class="classroom">
    <div class="teacher-display">
      <p>{{ teacherMessage }}</p>
      <div v-if="isTeacherSpeaking" class="speaking-indicator">🎤 Teacher is speaking...</div>
    </div>

    <div class="student-controls">
      <input
        v-model="studentInput"
        type="text"
        placeholder="Type your answer..."
        @keyup.enter="sendInput"
      />
      <button @click="sendInput">Send</button>

      <div class="control-buttons">
        <button @click="sendControlSignal('repeat')">🔁 Repeat</button>
        <button @click="sendControlSignal('slower')">🐢 Slower</button>
        <button @click="sendControlSignal('faster')">🐇 Faster</button>
        <button @click="sendControlSignal('confused')">❓ Confused</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { RealtimeClient } from '@/realtime/client'
import { WsTypes, ControlSignal } from '@/types/realtime'

const studentInput = ref('')
const teacherMessage = ref('Welcome to the lesson!')
const isTeacherSpeaking = ref(false)
let client: RealtimeClient | null = null

const sendInput = () => {
  if (!studentInput.value.trim()) return

  if (client) {
    client.send(WsTypes.StudentInput, {
      content: studentInput.value,
      type: 'text',
    })
  }

  studentInput.value = ''
}

const sendControlSignal = (signal: string) => {
  if (client) {
    client.send(WsTypes.ControlSignal, { signal })
  }
}

// In a real app, initialize the WebSocket connection
// onMounted(() => {
//   client = new RealtimeClient('ws://localhost:5000/ws', 'session-id')
//   client.on(WsTypes.TeacherResponse, (payload) => {
//     teacherMessage.value = payload.content
//   })
// })
</script>

<style scoped>
.classroom {
  display: flex;
  flex-direction: column;
  gap: 2rem;
  max-width: 600px;
  margin: 0 auto;
}

.teacher-display {
  background: #f5f5f5;
  padding: 2rem;
  border-radius: 8px;
  min-height: 150px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.speaking-indicator {
  margin-top: 1rem;
  color: #667eea;
  font-weight: bold;
  animation: pulse 1s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.student-controls {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

input {
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

button {
  padding: 0.5rem 1rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 1rem;
}

button:hover {
  background: #764ba2;
}

.control-buttons {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(100px, 1fr));
  gap: 0.5rem;
}
</style>
