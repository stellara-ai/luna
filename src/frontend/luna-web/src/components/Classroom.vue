<template>
  <div class="classroom">
    <!-- Status bar -->
    <div class="statusbar">
      <div class="status-left">
        <span class="pill" :class="connClass">
          <span class="dot" /> {{ connLabel }}
        </span>

        <span class="pill subtle" v-if="sessionMeta.sessionId">
          session: {{ sessionMeta.sessionId.slice(0, 8) }}
        </span>

        <span class="pill subtle" v-if="sessionMeta.lessonId">
          lesson: {{ sessionMeta.lessonId }}
        </span>
      </div>

      <div class="status-right">
        <button class="btn secondary" @click="reconnect" :disabled="connState === 'connecting'">
          Reconnect
        </button>
        <button class="btn danger" @click="clearTurns" :disabled="orderedTurns.length === 0">
          Clear
        </button>
      </div>
    </div>

    <!-- Teacher display -->
    <div class="teacher-display">
      <div class="teacher-header">
        <div class="title">Teacher</div>

        <div class="presence">
          <span v-if="isTeacherSpeaking" class="speaking-indicator">🎤 speaking…</span>
          <span v-else-if="isTeacherPresent" class="thinking-indicator">🧠 thinking…</span>
          <span v-else class="idle-indicator">idle</span>
        </div>
      </div>

      <!-- Scrollable transcript -->
      <div
        ref="scrollEl"
        class="turns"
        @scroll="onScroll"
      >
        <div v-if="orderedTurns.length === 0" class="empty">
          {{ teacherMessage }}
        </div>

        <div
          v-for="t in orderedTurns"
          :key="t.turnId"
          class="turn"
          :class="{ active: t.status === 'streaming', done: t.status === 'done', errored: t.status === 'error' }"
        >
          <div class="turn-meta">
            <span class="turn-id">turn: {{ t.turnId.slice(0, 8) }}</span>
            <span class="turn-status">
              {{ t.statusLabel }}
            </span>
          </div>

          <p class="turn-text">
            <!-- streaming cursor -->
            <span>{{ t.text }}</span>
            <span v-if="t.status === 'streaming'" class="cursor">▍</span>
            <span v-if="t.status === 'streaming' && !t.text" class="ghost">…</span>
          </p>

          <div v-if="t.debug" class="turn-debug">
            deltas: {{ t.debug.deltaCount }} · lastIndex: {{ t.debug.lastDeltaIndex }}
          </div>
        </div>

        <!-- anchor for autoscroll -->
        <div ref="bottomEl" class="bottom-anchor" />
      </div>

      <div v-if="lastError" class="error-banner">
        ⚠️ {{ lastError }}
      </div>
    </div>

    <!-- Student controls -->
    <div class="student-controls">
      <div class="input-row">
        <input
          v-model="studentInput"
          type="text"
          placeholder="Type your message…"
          @keyup.enter="sendInput"
          :disabled="connState !== 'connected'"
        />
        <button class="btn" @click="sendInput" :disabled="connState !== 'connected'">
          Send
        </button>
      </div>

      <div class="control-buttons">
        <button class="btn secondary" @click="sendControlSignal(ControlSignal.Repeat)" :disabled="connState !== 'connected'">
          🔁 Repeat
        </button>
        <button class="btn secondary" @click="sendControlSignal(ControlSignal.Slower)" :disabled="connState !== 'connected'">
          🐢 Slower
        </button>
        <button class="btn secondary" @click="sendControlSignal(ControlSignal.Faster)" :disabled="connState !== 'connected'">
          🐇 Faster
        </button>
        <button class="btn secondary" @click="sendControlSignal(ControlSignal.Confused)" :disabled="connState !== 'connected'">
          ❓ Confused
        </button>
      </div>

      <div class="hint">
        Tip: the UI supports overlapping turns; each response is keyed by <code>turnId</code> and streamed via deltas.
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useSessionStore } from '@/app/store'
import { RealtimeClient } from '@/realtime/client'
import { WsTypes, ControlSignal } from '@/types/realtime'

type TurnStatus = 'streaming' | 'done' | 'error'

type TurnState = {
  turnId: string
  text: string
  status: TurnStatus
  startedAtMs: number
  // delta safety
  lastDeltaIndex: number
  // light debug (helpful during integration, harmless in prod)
  debug?: { deltaCount: number; lastDeltaIndex: number }
}

const store = useSessionStore()

const studentInput = ref('')
const teacherMessage = ref('Welcome to Luna. Start the lesson when ready.')
const lastError = ref<string | null>(null)

const connState = ref<'idle' | 'connecting' | 'connected' | 'disconnected'>('idle')

const sessionMeta = ref({
  sessionId: '',
  lessonId: 'lesson-web',
  studentId: 'stu-web',
  webSocketUrl: '',
})

const turns = ref<Record<string, TurnState>>({})

let client: RealtimeClient | null = null

// ----- UX: autoscroll that respects user scroll position -----
const scrollEl = ref<HTMLElement | null>(null)
const bottomEl = ref<HTMLElement | null>(null)
const shouldAutoScroll = ref(true)

function onScroll() {
  const el = scrollEl.value
  if (!el) return
  const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight
  // if the user is close to bottom, keep autoscrolling
  shouldAutoScroll.value = distanceFromBottom < 80
}

async function maybeAutoScroll() {
  if (!shouldAutoScroll.value) return
  await nextTick()
  bottomEl.value?.scrollIntoView({ behavior: 'smooth', block: 'end' })
}

// ----- helpers -----
function newTurnId() {
  // keep backend style: 32 hex chars (no dashes)
  return crypto.randomUUID().replaceAll('-', '')
}

function getTurnId(payload: any): string | null {
  return payload?.turnId ?? payload?.TurnId ?? null
}

function getDeltaIndex(payload: any): number | null {
  const v = payload?.deltaIndex ?? payload?.DeltaIndex
  return typeof v === 'number' ? v : null
}

function getDeltaText(payload: any): string {
  return String(payload?.delta ?? payload?.Delta ?? '')
}

function getOp(payload: any): string {
  return String(payload?.operation ?? payload?.Operation ?? 'append').toLowerCase()
}

function ensureTurn(turnId: string): TurnState {
  const existing = turns.value[turnId]
  if (existing) return existing

  const t: TurnState = {
    turnId,
    text: '',
    status: 'streaming',
    startedAtMs: Date.now(),
    lastDeltaIndex: 0,
    debug: { deltaCount: 0, lastDeltaIndex: 0 },
  }
  turns.value = { ...turns.value, [turnId]: t }
  return t
}

function upsertTurn(t: TurnState) {
  // Keep transcript bounded (prevents UI bloat)
  const MAX_TURNS = 50
  turns.value = { ...turns.value, [t.turnId]: { ...t } }

  const ids = Object.keys(turns.value)
  if (ids.length > MAX_TURNS) {
    const ordered = ids
      .map(id => turns.value[id]!)
      .sort((a, b) => a.startedAtMs - b.startedAtMs)
    const toRemove = ordered.slice(0, Math.max(0, ordered.length - MAX_TURNS))
    const next: Record<string, TurnState> = { ...turns.value }
    for (const r of toRemove) delete next[r.turnId]
    turns.value = next
  }
}

const orderedTurns = computed(() => {
  return Object.values(turns.value).sort((a, b) => a.startedAtMs - b.startedAtMs).map(t => {
    const statusLabel =
      t.status === 'streaming' ? 'streaming' :
      t.status === 'done' ? 'done' : 'error'
    return { ...t, statusLabel }
  })
})

// §2.1 presence signals:
// - "present" means a teacher turn has started but may not have text yet.
// - "speaking" means we are actively streaming deltas for any active turn.
const isTeacherPresent = computed(() => orderedTurns.value.some(t => t.status === 'streaming'))
const isTeacherSpeaking = computed(() => orderedTurns.value.some(t => t.status === 'streaming' && t.lastDeltaIndex > 0))

const connLabel = computed(() => {
  switch (connState.value) {
    case 'connected': return 'connected'
    case 'connecting': return 'connecting…'
    case 'disconnected': return 'disconnected'
    default: return 'idle'
  }
})

const connClass = computed(() => {
  switch (connState.value) {
    case 'connected': return 'ok'
    case 'connecting': return 'warn'
    case 'disconnected': return 'bad'
    default: return 'subtle'
  }
})

// ----- inbound handlers -----
function handleSessionEvent(payload: any) {
  const eventType = payload?.eventType ?? payload?.EventType
  if (!eventType) return

  if (eventType === 'session.connected') {
    // could show connectionCorrelationId if you want
    return
  }
  if (eventType === 'session.started') {
    return
  }
}

function handleError(payload: any) {
  const msg = payload?.message ?? payload?.Message ?? 'Unknown error'
  lastError.value = msg
}

function handleTurnStart(payload: any) {
  const tid = getTurnId(payload)
  if (!tid) return

  const t = ensureTurn(tid)
  t.status = 'streaming'
  // Important: do NOT clear text on start. (Allows server retries / reconnect replays later.)
  upsertTurn(t)
  void maybeAutoScroll()
}

function handleTextDelta(payload: any) {
  const tid = getTurnId(payload)
  if (!tid) return

  const deltaIndex = getDeltaIndex(payload)
  if (deltaIndex == null) return

  const deltaText = getDeltaText(payload)
  const op = getOp(payload)

  const t = ensureTurn(tid)

  // Ignore duplicates / out-of-order (keeps UI deterministic)
  if (deltaIndex <= t.lastDeltaIndex) return

  if (op === 'append') {
    t.text += deltaText
  } else {
    // future-proof: treat unknown ops as append
    t.text += deltaText
  }

  t.lastDeltaIndex = deltaIndex
  if (t.debug) {
    t.debug.deltaCount += 1
    t.debug.lastDeltaIndex = t.lastDeltaIndex
  }

  upsertTurn(t)
  void maybeAutoScroll()
}

function handleTurnEnd(payload: any) {
  const tid = getTurnId(payload)
  if (!tid) return

  const t = ensureTurn(tid)
  t.status = 'done'
  upsertTurn(t)
  void maybeAutoScroll()
}

// ----- outbound -----
function sendInput() {
  const text = studentInput.value.trim()
  if (!text || !client || connState.value !== 'connected') return

  lastError.value = null

  const turnId = newTurnId()

  // Zero-latency presence comes from server’s immediate turn_start + micro-ack delta.
  // We still generate a client turnId so the response can be correlated.
  client.sendStudentInput({
    sessionId: sessionMeta.value.sessionId,
    turnId,
    content: text,
    type: 'Text',
  })

  studentInput.value = ''
}

function sendControlSignal(signal: ControlSignal) {
  if (!client || connState.value !== 'connected') return

  client.sendWithFlowId(
    WsTypes.ControlSignal,
    {
      sessionId: sessionMeta.value.sessionId,
      signal,
    },
    {
      flowCorrelationId: client.newTurnId(),
    }
  )
}

function clearTurns() {
  turns.value = {}
  lastError.value = null
}

async function connectSession() {
  connState.value = 'connecting'
  lastError.value = null

  // 1) Create session via REST (store)
  const session = await store.startSession(sessionMeta.value.studentId, sessionMeta.value.lessonId)

  sessionMeta.value.sessionId = session.sessionId
  sessionMeta.value.lessonId = session.lessonId
  sessionMeta.value.studentId = session.studentId
  sessionMeta.value.webSocketUrl = session.webSocketUrl

  // 2) Connect WS
  client = new RealtimeClient(session.webSocketUrl, session.sessionId)

  // register inbound handlers (streaming-first)
  client.on(WsTypes.SessionEvent, handleSessionEvent)
  client.on(WsTypes.TeacherTurnStart, handleTurnStart)
  client.on(WsTypes.TeacherTextDelta, handleTextDelta)
  client.on(WsTypes.TeacherTurnEnd, handleTurnEnd)

  // infra
  client.on(WsTypes.Error, handleError)

  await client.connect()
  connState.value = 'connected'

  // 3) Send session_start
  client.sendSessionStart({
    sessionId: session.sessionId,
    lessonId: session.lessonId,
    studentId: session.studentId,
  })

  // If your store has this, keep it — otherwise remove (no-op safe).
  // @ts-ignore
  store.markActive?.()
}

async function reconnect() {
  try {
    connState.value = 'connecting'
    client?.disconnect()
    client = null
    await connectSession()
  } catch (e: any) {
    connState.value = 'disconnected'
    lastError.value = e?.message ?? 'Reconnect failed'
  }
}

// lifecycle
onMounted(async () => {
  try {
    await connectSession()
  } catch (e: any) {
    connState.value = 'disconnected'
    lastError.value = e?.message ?? 'Failed to start session'
  }
})

onBeforeUnmount(() => {
  client?.disconnect()
  client = null
  connState.value = 'disconnected'
})
</script>

<style scoped>
.classroom {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  max-width: 860px;
  margin: 0 auto;
  padding: 0 0.5rem;
}

.statusbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.25rem;
}

.status-left, .status-right {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.pill {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.28rem 0.55rem;
  border-radius: 999px;
  font-size: 0.85rem;
  border: 1px solid #e6e6e6;
  background: #fff;
}

.pill .dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: #bbb;
}

.pill.ok {
  border-color: #d7f4df;
  background: #f3fff6;
}
.pill.ok .dot { background: #25b35a; }

.pill.warn {
  border-color: #ffe7c2;
  background: #fff9ef;
}
.pill.warn .dot { background: #f0a202; }

.pill.bad {
  border-color: #ffd0d0;
  background: #fff3f3;
}
.pill.bad .dot { background: #e53935; }

.pill.subtle {
  background: #fafafa;
  color: #555;
}

.teacher-display {
  background: #f5f5f5;
  padding: 1rem;
  border-radius: 12px;
  min-height: 320px;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.teacher-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.title {
  font-weight: 800;
  letter-spacing: 0.2px;
}

.presence {
  font-size: 0.9rem;
  font-weight: 700;
  color: #444;
}

.speaking-indicator {
  color: #667eea;
  animation: pulse 1s infinite;
}

.thinking-indicator {
  color: #764ba2;
  animation: pulse 1.2s infinite;
}

.idle-indicator {
  color: #666;
}

.turns {
  flex: 1;
  background: #ffffffaa;
  border: 1px solid #eee;
  border-radius: 12px;
  padding: 0.75rem;
  overflow: auto;
  max-height: 420px;
}

.turn {
  background: white;
  border-radius: 12px;
  padding: 0.75rem 0.9rem;
  border: 1px solid #eee;
  margin-bottom: 0.65rem;
}

.turn.active {
  border-color: #667eea33;
  box-shadow: 0 1px 0 rgba(0,0,0,0.03);
}

.turn.done {
  opacity: 0.98;
}

.turn.errored {
  border-color: #ffd0d0;
  background: #fff8f8;
}

.turn-meta {
  display: flex;
  justify-content: space-between;
  font-size: 0.8rem;
  color: #666;
  margin-bottom: 0.35rem;
}

.turn-text {
  margin: 0;
  white-space: pre-wrap;
  line-height: 1.45;
}

.cursor {
  display: inline-block;
  margin-left: 2px;
  animation: blink 0.9s infinite;
}

.ghost {
  color: #888;
}

.turn-debug {
  margin-top: 0.45rem;
  font-size: 0.75rem;
  color: #888;
}

.empty {
  color: #666;
  padding: 0.25rem 0;
}

.bottom-anchor {
  height: 1px;
}

.error-banner {
  border: 1px solid #ffd0d0;
  background: #fff3f3;
  color: #9b1c1c;
  padding: 0.6rem 0.75rem;
  border-radius: 10px;
  font-weight: 600;
}

.student-controls {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 1rem;
}

.input-row {
  display: flex;
  gap: 0.5rem;
}

input {
  flex: 1;
  padding: 0.65rem 0.75rem;
  border: 1px solid #ddd;
  border-radius: 10px;
  font-size: 1rem;
}

.btn {
  padding: 0.65rem 1rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 10px;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 700;
}

.btn:hover {
  background: #764ba2;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn.secondary {
  background: #ffffff;
  color: #333;
  border: 1px solid #ddd;
}

.btn.secondary:hover {
  background: #f7f7f7;
}

.btn.danger {
  background: #fff3f3;
  color: #9b1c1c;
  border: 1px solid #ffd0d0;
}

.btn.danger:hover {
  background: #ffecec;
}

.control-buttons {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 0.5rem;
}

.hint {
  font-size: 0.9rem;
  color: #666;
}

.hint code {
  background: #f2f2f2;
  border-radius: 6px;
  padding: 0.1rem 0.35rem;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.55; }
}

@keyframes blink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0; }
}
</style>