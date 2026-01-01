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

export const WsTypes = {
  StudentInput: 'classroom.student_input',
  TeacherResponse: 'classroom.teacher_response',
  TeacherTurnStart: 'classroom.teacher_turn_start',
  TeacherTurnEnd: 'classroom.teacher_turn_end',
  SessionStart: 'classroom.session_start',
  SessionEnd: 'classroom.session_end',
  ControlSignal: 'classroom.control_signal',
  SessionEvent: 'classroom.session_event',
} as const

export enum ControlSignal {
  Repeat = 'repeat',
  Slower = 'slower',
  Faster = 'faster',
  Confused = 'confused',
  Understood = 'understood',
  Skip = 'skip',
}
