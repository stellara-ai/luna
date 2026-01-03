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

function getTurnId(msg) {
  const p = msg?.payload ?? {};
  return p.turnId ?? p.TurnId ?? null;
}

function getDeltaIndex(msg) {
  const p = msg?.payload ?? {};
  return p.deltaIndex ?? p.DeltaIndex ?? null;
}

function getDeltaText(msg) {
  const p = msg?.payload ?? {};
  return p.delta ?? p.Delta ?? null;
}

function assertMonotonicSequence(msgs, label = "batch") {
  let prev = -Infinity;
  for (const m of msgs) {
    const seq = m?.sequenceNumber;
    if (typeof seq !== "number") continue;
    if (seq <= prev) {
      throw new Error(
        `Non-monotonic envelope sequenceNumber in ${label}: ${seq} after ${prev}`
      );
    }
    prev = seq;
  }
}

function buildTurnTranscript(msgs) {
  // Group deltas by turnId
  const turns = new Map(); // turnId -> { deltas: [{i,text,offset}], start, end, legacyResponse }
  for (const m of msgs) {
    const type = m?.messageType;
    const tid = getTurnId(m);

    if (type === "v1.classroom.teacher_response") {
      // legacy aggregate fallback (no turnId)
      const content = m?.payload?.content ?? m?.payload?.Content;
      if (!content) continue;
      const key = tid ?? "__legacy__";
      if (!turns.has(key)) turns.set(key, { deltas: [], legacyResponse: null });
      turns.get(key).legacyResponse = content;
      continue;
    }

    // For turn-scoped messages, skip if no turnId
    if (!tid) continue;

    if (!turns.has(tid)) turns.set(tid, { deltas: [], start: null, end: null, legacyResponse: null });
    const t = turns.get(tid);

    if (type === "v1.classroom.teacher_turn_start") t.start = m;
    if (type === "v1.classroom.teacher_turn_end") t.end = m;

    if (type === "v1.classroom.teacher_text_delta") {
      const i = getDeltaIndex(m);
      const text = getDeltaText(m) ?? "";
      const offset = m?.payload?.offsetMs ?? m?.payload?.OffsetMs ?? 0;
      if (typeof i === "number") t.deltas.push({ i, text, offset });
    }
  }

  // Build assembled text per turn
  const result = [];
  for (const [turnId, t] of turns.entries()) {
    const deltasSorted = [...t.deltas].sort((a, b) => a.i - b.i);
    const assembled = deltasSorted.map(d => d.text).join("");
    result.push({
      turnId,
      hasStart: !!t.start,
      hasEnd: !!t.end,
      deltaCount: deltasSorted.length,
      assembledText: assembled,
      legacyResponse: t.legacyResponse,
    });
  }

  // Prefer real turnIds over "__legacy__"
  result.sort((a, b) => (a.turnId === "__legacy__") - (b.turnId === "__legacy__"));
  return result;
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
    const deltaIndex = payload?.deltaIndex ?? payload?.DeltaIndex;

    lines.push(
      `- ${type}  seq=${seq} corr=${corr}` +
      (turnId ? ` turnId=${turnId}` : "") +
      (deltaIndex !== undefined ? ` deltaIndex=${deltaIndex}` : "") +
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
    correlationId: correlationId,
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

    const inputTurnIdA = NewId();
    const inputTurnIdB = NewId();

    console.log(`Client inputTurnIdA: ${inputTurnIdA}`);
    console.log(`Client inputTurnIdB: ${inputTurnIdB}`);
  // 6) Send student_input
  const studentInputEnvelopeA = {
    messageId: NewId(),
    correlationId: correlationId,
    messageType: "v1.classroom.student_input",
    timestamp: new Date().toISOString(),
    sequenceNumber: 2,
    payload: {
      sessionId,
      turnId: inputTurnIdA,
      content: "Hi Luna — can you explain fractions simply?",
      type: "Text", // if enum string ever fails, use: type: 0
    },
  };

const studentInputEnvelopeB = {
    messageId: NewId(),
    correlationId: correlationId,
    messageType: "v1.classroom.student_input",
    timestamp: new Date().toISOString(),
    sequenceNumber: 3,
    payload: {
      sessionId,
      turnId: inputTurnIdB,
      content: "Now explain decimals simply too.",
      type: "Text", // if enum string ever fails, use: type: 0
    },
  };


  console.log("SEND (student_input A)...");
  ws.send(JSON.stringify(studentInputEnvelopeA));
  console.log("SEND (student_input B)...");
  ws.send(JSON.stringify(studentInputEnvelopeB));

  // 7) Collect response stream for TWO turns
// Stop when we have teacher_turn_end for BOTH input turnIds.
// Legacy fallback: if server sends teacher_response only, stop on first teacher_response.
const expectedTurnIds = new Set([inputTurnIdA, inputTurnIdB]);
const endedTurnIds = new Set();


    const replyMsgs = await wsCollect(ws, {
    stopWhen: (m, all) => {
        const ended = new Set(
        all
            .filter(x => x?.messageType === "v1.classroom.teacher_turn_end")
            .map(getTurnId)
            .filter(Boolean)
        );

        return ended.has(inputTurnIdA) && ended.has(inputTurnIdB);
    },
    idleTimeoutMs: 900,   // give it a bit more breathing room for 2 turns
    hardTimeoutMs: 6000,  // overlap test needs more time than single turn
    maxMessages: 300,
    });

console.log("RECV (reply batch):");

    // Validate envelope sequence monotonicity for the reply burst
    assertMonotonicSequence(replyMsgs, "reply batch");

    // Echo checks (each input turnId must appear in streaming messages)
    const echoedA = replyMsgs.some(m => getTurnId(m) === inputTurnIdA);
    const echoedB = replyMsgs.some(m => getTurnId(m) === inputTurnIdB);

    console.log(`Echoed inputTurnIdA? ${echoedA ? "✅ yes" : "❌ no"}`);
    console.log(`Echoed inputTurnIdB? ${echoedB ? "✅ yes" : "❌ no"}`);

    // Option A: streaming-only contract
    const hasAnyDelta = replyMsgs.some(m => m?.messageType === "v1.classroom.teacher_text_delta");
    const hasTurnStart = replyMsgs.some(m => m?.messageType === "v1.classroom.teacher_turn_start");
    const hasTurnEnd = replyMsgs.some(m => m?.messageType === "v1.classroom.teacher_turn_end");

    // 1) Must have streaming framing + content
    if (!hasAnyDelta || !hasTurnStart || !hasTurnEnd) {
    console.error("❌ Missing streaming messages (need turn_start, text_delta, turn_end).");
    process.exitCode = 1;
    }

    // 2) Must echo both input turnIds somewhere in the stream
    if (!echoedA || !echoedB) {
        console.error("❌ Streaming deltas received but server did not echo both input turnIds.");
        process.exitCode = 1;
    }

    // Build/print assembled turn text (from deltas)
    const turns = buildTurnTranscript(replyMsgs);

    if (turns.length) {
        console.log("Assembled turn transcript(s):");
        for (const t of turns) {
            console.log(`\nTurn: ${t.turnId}`);
            console.log(`  start: ${t.hasStart ? "✅" : "❌"}  end: ${t.hasEnd ? "✅" : "❌"}  deltas: ${t.deltaCount}`);
            if (t.assembledText) {
            console.log("  assembled (deltas):");
            console.log(`  ${t.assembledText}`);
            } else if (t.legacyResponse) {
            console.log("  legacy response:");
            console.log(`  ${t.legacyResponse}`);
            } else {
            console.log("  (no text assembled)");
            }
        }
        console.log("");
    }

    // Per-turn streaming contract checks (only for expected turns)
    if (hasAnyDelta) {
    const turnMap = new Map(turns.map(t => [t.turnId, t]));

    for (const tid of expectedTurnIds) {
        const t = turnMap.get(tid);
        if (!t) {
        console.error(`❌ Missing any assembled turn state for expected turnId ${tid}`);
        process.exitCode = 1;
        continue;
        }

        if (!t.hasStart || !t.hasEnd) {
        console.error(`❌ Missing teacher_turn_start or teacher_turn_end for turnId ${tid}`);
        process.exitCode = 1;
        }

        if (t.deltaCount < 1) {
        console.error(`❌ No text deltas received for turnId ${tid}`);
        process.exitCode = 1;
        }
    }
    }

    console.log(summarizeMessages(replyMsgs));
    console.log("");

  // Assertions
  const okTypes = new Set([
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