/**
 * WebSocket message envelope types.
 * Mirrors Luna.Contracts.Realtime from backend.
 */

export interface WsEnvelope<T> {
  messageId: string
  correlationId: string
  messageType: string
  timestamp: string
  sequenceNumber?: number
  payload: T
}

// IMPORTANT: must match server-emitted messageType strings
export const WsTypes = {
  SessionStart: 'v1.classroom.session_start',
  SessionEnd: 'v1.classroom.session_end',
  SessionEvent: 'v1.classroom.session_event',

  StudentInput: 'v1.classroom.student_input',
  ControlSignal: 'v1.classroom.control_signal',

  TeacherTurnStart: 'v1.classroom.teacher_turn_start',
  TeacherTextDelta: 'v1.classroom.teacher_text_delta',
  TeacherTurnEnd: 'v1.classroom.teacher_turn_end',

  // legacy (you removed it server-side, but keep type for safety)
  TeacherResponse: 'v1.classroom.teacher_response',

  // optional streaming types (future)
  TeacherAudioChunk: 'v1.classroom.teacher_audio_chunk',
  TeacherMark: 'v1.classroom.teacher_mark',

  // infra
  Error: 'v1.classroom.error',
  Ping: 'v1.classroom.ping',
  Pong: 'v1.classroom.pong',
} as const

export enum ControlSignal {
  Repeat = 'repeat',
  Slower = 'slower',
  Faster = 'faster',
  Confused = 'confused',
  Understood = 'understood',
  Skip = 'skip',
}