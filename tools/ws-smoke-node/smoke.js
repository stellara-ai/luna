#!/usr/bin/env node
/* eslint-disable no-console */

const fs = require("fs");
const path = require("path");
const WebSocket = require("ws");
const crypto = require("crypto");

const NewId = () => crypto.randomUUID().replaceAll("-", "");

function firstHttpUrlFromLaunchSettings(launchSettingsPath) {
  if (!fs.existsSync(launchSettingsPath)) return null;

  const json = JSON.parse(fs.readFileSync(launchSettingsPath, "utf8"));
  const profiles = json?.profiles ?? {};

  const profile =
    profiles["http"] ||
    profiles["Luna.ApiGateway"] ||
    profiles[Object.keys(profiles)[0]];

  const appUrl = profile?.applicationUrl;
  if (!appUrl) return null;

  const urls = appUrl.split(";").map((s) => s.trim());
  const http = urls.find((u) => u.startsWith("http://"));
  return http ?? urls[0] ?? null;
}

async function postJson(url, body) {
  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`HTTP ${res.status} ${res.statusText} - ${text}`);
  }

  return res.json();
}

function parseWsMessage(data) {
  const text = Buffer.isBuffer(data) ? data.toString("utf8") : String(data);
  return JSON.parse(text);
}

/**
 * Collect WS messages until:
 *  - stopWhen(msg) returns true (then includes that msg), OR
 *  - idleTimeoutMs elapses with no new messages (idle stop), OR
 *  - hardTimeoutMs elapses (hard stop)
 *
 * If the socket errors/closes before we stop, rejects.
 */
function wsCollect(ws, {
  stopWhen,
  idleTimeoutMs = 600,
  hardTimeoutMs = 2500,
  maxMessages = 50,
} = {}) {
  return new Promise((resolve, reject) => {
    const messages = [];
    let idleTimer = null;
    let hardTimer = null;
    let done = false;

    function finish() {
      if (done) return;
      done = true;
      cleanup();
      resolve(messages);
    }

    function fail(err) {
      if (done) return;
      done = true;
      cleanup();
      reject(err);
    }

    function resetIdle() {
      if (idleTimer) clearTimeout(idleTimer);
      idleTimer = setTimeout(() => finish(), idleTimeoutMs);
    }

    function onMessage(data) {
      try {
        const obj = parseWsMessage(data);
        messages.push(obj);

        // safety: don't let a runaway stream hang the test forever
        if (messages.length >= maxMessages) return finish();

        if (typeof stopWhen === "function" && stopWhen(obj, messages)) {
          return finish();
        }

        // otherwise keep collecting, but idle-stop if things go quiet
        resetIdle();
      } catch (e) {
        return fail(e);
      }
    }

    function onError(err) {
      fail(err);
    }

    function onClose(code, reason) {
      fail(new Error(`WS closed early: ${code} ${reason?.toString?.() ?? ""}`));
    }

    function cleanup() {
      if (idleTimer) clearTimeout(idleTimer);
      if (hardTimer) clearTimeout(hardTimer);
      ws.off("message", onMessage);
      ws.off("error", onError);
      ws.off("close", onClose);
    }

    ws.on("message", onMessage);
    ws.on("error", onError);
    ws.on("close", onClose);

    hardTimer = setTimeout(() => finish(), hardTimeoutMs);
    resetIdle();
  });
}

function summarizeMessages(msgs) {
  const lines = [];
  for (const m of msgs) {
    const type = m?.messageType ?? "<no-type>";
    const seq = m?.sequenceNumber ?? "?";
    const corr = m?.correlationId ?? "<no-corr>";
    const payload = m?.payload ?? {};
    const turnId = payload?.turnId || payload?.TurnId;
    const offsetMs = payload?.offsetMs ?? payload?.OffsetMs;
    lines.push(
      `- ${type}  seq=${seq} corr=${corr}` +
      (turnId ? ` turnId=${turnId}` : "") +
      (offsetMs !== undefined ? ` offsetMs=${offsetMs}` : "")
    );
  }
  return lines.join("\n");
}

(async function main() {
  // Node 18+ has fetch. If you're on older Node, upgrade or add node-fetch.
  if (typeof fetch !== "function") {
    throw new Error("This script requires Node 18+ (built-in fetch).");
  }

  // 1) Determine API base URL:
  const envBase = process.env.LUNA_API_BASE?.trim();

  const launchSettingsGuess = path.resolve(
    __dirname,
    "../../apps/Luna.ApiGateway/Properties/launchSettings.json"
  );

  const apiBase =
    envBase ||
    firstHttpUrlFromLaunchSettings(launchSettingsGuess) ||
    "http://localhost:5050";

  console.log(`API Base URL: ${apiBase}`);

  // 2) Create session
  console.log("Creating classroom session...");
  const create = await postJson(`${apiBase}/api/classroom/sessions`, {
    studentId: "stu-node",
    lessonId: "lesson-node",
  });

  const sessionId = create.sessionId;
  const wsUrl = create.webSocketUrl;

  console.log(`SessionId: ${sessionId}`);
  console.log(`WS URL:    ${wsUrl}\n`);

  // 3) Connect WebSocket
  const ws = new WebSocket(wsUrl);
  await new Promise((resolve, reject) => {
    ws.once("open", resolve);
    ws.once("error", reject);
  });

  console.log(`Connected: ${wsUrl}\n`);

  // 4) Read initial server message(s) (you send "session.connected" on connect)
  const connectedMsgs = await wsCollect(ws, {
    stopWhen: (m) => m?.messageType === "v1.classroom.session_event",
    idleTimeoutMs: 400,
    hardTimeoutMs: 1200,
  });

  console.log("RECV (connected batch):");
  console.log(summarizeMessages(connectedMsgs));
  if (connectedMsgs[0]) {
    console.log("\nFirst connected message (full):");
    console.log(JSON.stringify(connectedMsgs[0], null, 2));
  }
  console.log("");

  // 5) Send session_start
  const correlationId = NewId();
  const sessionStartEnvelope = {
    messageId: NewId(),
    correlationId,
    messageType: "v1.classroom.session_start",
    timestamp: new Date().toISOString(),
    sequenceNumber: 1,
    payload: {
      sessionId,
      lessonId: "lesson-node",
      studentId: "stu-node",
    },
  };

  console.log("SEND (session_start)...");
  ws.send(JSON.stringify(sessionStartEnvelope));

  // Expect at least one session_event response for started
  const startedMsgs = await wsCollect(ws, {
    stopWhen: (m) =>
      m?.messageType === "v1.classroom.session_event" &&
      m?.payload?.eventType === "session.started",
    idleTimeoutMs: 500,
    hardTimeoutMs: 2000,
  });

  console.log("RECV (started batch):");
  console.log(summarizeMessages(startedMsgs));
  const started = startedMsgs.find((m) => m?.payload?.eventType === "session.started");
  if (started) {
    console.log("\nStarted message (full):");
    console.log(JSON.stringify(started, null, 2));
  }
  console.log("");

  // 6) Send student_input
  const studentInputEnvelope = {
    messageId: NewId(),
    correlationId,
    messageType: "v1.classroom.student_input",
    timestamp: new Date().toISOString(),
    sequenceNumber: 2,
    payload: {
      sessionId,
      content: "Hi Luna — can you explain fractions simply?",
      type: "Text", // if enum string ever fails, use: type: 0
    },
  };

  console.log("SEND (student_input)...");
  ws.send(JSON.stringify(studentInputEnvelope));

  // 7) Collect response stream:
  // Stop conditions (any one):
  // - teacher_response arrives (current behavior)
  // - teacher_turn_end arrives (future streaming behavior)
  //
  // Otherwise stop on idle/hard timeout.
  const replyMsgs = await wsCollect(ws, {
    stopWhen: (m) =>
      m?.messageType === "v1.classroom.teacher_response" ||
      m?.messageType === "v1.classroom.teacher_turn_end",
    idleTimeoutMs: 650,
    hardTimeoutMs: 3500,
    maxMessages: 100,
  });

  console.log("RECV (reply batch):");
  console.log(summarizeMessages(replyMsgs));
  console.log("");

  // Assertions
  const okTypes = new Set([
    "v1.classroom.teacher_response",
    "v1.classroom.teacher_turn_start",
    "v1.classroom.teacher_turn_end",
    "v1.classroom.teacher_text_delta",
    "v1.classroom.teacher_audio_chunk",
    "v1.classroom.teacher_mark",
    "v1.classroom.session_event",
    "v1.classroom.error",
    "v1.classroom.ping",
    "v1.classroom.pong",
  ]);

  const unexpected = replyMsgs.filter((m) => !okTypes.has(m?.messageType));
  if (unexpected.length) {
    console.error("❌ Unexpected message types received:");
    for (const m of unexpected) console.error(`- ${m?.messageType}`);
    process.exitCode = 1;
  }

  // Must receive at least one "teacher-ish" response
  const hasTeacher =
    replyMsgs.some((m) => m?.messageType === "v1.classroom.teacher_response") ||
    replyMsgs.some((m) => m?.messageType === "v1.classroom.teacher_text_delta") ||
    replyMsgs.some((m) => m?.messageType === "v1.classroom.teacher_turn_start");

  if (!hasTeacher) {
    console.error("❌ Did not receive any teacher response messages.");
    process.exitCode = 1;
  } else {
    console.log("✅ WS streaming-ready smoke test passed.");
  }

  ws.close(1000, "bye");
})().catch((err) => {
  console.error("❌ Smoke test failed:");
  console.error(err);
  process.exitCode = 1;
});