import { readFile, writeFile } from "node:fs/promises";
import { pathToFileURL } from "node:url";
import { setTimeout as delay } from "node:timers/promises";
import { createHash } from "node:crypto";

const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const cliClient = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
const requireValue = (condition, message) => {
  if (!condition) throw new Error(message);
};
const text = (value) => typeof value === "string" && value.trim().length > 0;
const unique = (values) => new Set(values).size === values.length;

/** Pins reports to the exact owner-supplied fixture, preventing retrospective golden changes. */
export function fixtureDigest(fixture) {
  return createHash("sha256").update(JSON.stringify(fixture)).digest("hex");
}

/** Validates owner-supplied goldens before any network call or sign-in. */
export function validateFixture(fixture) {
  requireValue(fixture?.version === 1, "Fixture version must be 1.");
  requireValue(
    guid.test(fixture.tenantId) && guid.test(fixture.portalClientId),
    "Tenant and portal client ids must be GUIDs.",
  );
  const api = new URL(fixture.api);
  requireValue(
    api.protocol === "https:" &&
      api.hostname.endsWith(".westus3.azurecontainerapps.io") &&
      !api.username &&
      !api.password &&
      !api.search &&
      !api.hash &&
      api.pathname === "/",
    "Use the approved westus3 Container Apps API origin, without credentials, path or query.",
  );
  requireValue(
    text(fixture.teamId) && /^[a-zA-Z0-9_-]+$/.test(fixture.teamId),
    "A concrete team id is required.",
  );
  const path = `/api/teams/${fixture.teamId}/`;
  requireValue(
    fixture.retrievalPath === `${path}knowledge/ask`,
    "Supply the real team-scoped application retrieval path, not a Graph/Search endpoint.",
  );
  requireValue(
    guid.test(fixture.personas?.positive) &&
      guid.test(fixture.personas?.negative) &&
      fixture.personas.positive.toLowerCase() !==
        fixture.personas.negative.toLowerCase(),
    "Supply two distinct real persona object ids.",
  );
  requireValue(
    Array.isArray(fixture.knowledgeBases) && fixture.knowledgeBases.length > 0,
    "Approved knowledge bases are required.",
  );
  requireValue(
    unique(fixture.knowledgeBases.map((base) => base.id)),
    "Knowledge base ids must be unique.",
  );
  for (const base of fixture.knowledgeBases) {
    requireValue(text(base.id), "Knowledge base id is required.");
    requireValue(
      Array.isArray(base.questions) && base.questions.length >= 40,
      "At least 40 questions per knowledge base are required.",
    );
    requireValue(
      unique(base.questions.map((question) => question.id)),
      "Question ids must be unique within a knowledge base.",
    );
    for (const question of base.questions) {
      requireValue(
        text(question.id) &&
          text(question.question) &&
          question.question.length <= 1500,
        "Each golden requires an id and an owner-approved question of at most 1500 characters.",
      );
      requireValue(
        Array.isArray(question.expectedSourceIds) &&
          question.expectedSourceIds.length > 0 &&
          question.expectedSourceIds.every(text) &&
          unique(question.expectedSourceIds),
        "Expected source ids are required and must be unique.",
      );
    }
    const permission = base.permission;
    requireValue(
      text(permission?.restrictedSourceId) &&
        Array.isArray(permission.deniedMarkers) &&
        permission.deniedMarkers.length > 0 &&
        permission.deniedMarkers.every(text),
      "Each knowledge base requires a restricted source id and exact owner-supplied leakage markers.",
    );
    const restricted = base.questions.find(
      (question) => question.id === permission.questionId,
    );
    const control = base.questions.find(
      (question) => question.id === permission.controlQuestionId,
    );
    requireValue(
      restricted?.expectedSourceIds.includes(permission.restrictedSourceId),
      "The permission question must expect the restricted source for the positive persona.",
    );
    requireValue(
      control &&
        !control.expectedSourceIds.includes(permission.restrictedSourceId) &&
        control.id !== restricted.id,
      "A distinct readable control question is required for the negative persona.",
    );
  }
  return fixture;
}

/** Validates a cited application response without manufacturing citations or unknown answers. */
export function validAnswer(body, baseId) {
  return (
    body?.knowledgeBaseId === baseId &&
    typeof body.noneFound === "boolean" &&
    typeof body.answer === "string" &&
    Array.isArray(body.citations) &&
    body.citations.every((citation) => {
      if (
        !text(citation?.sourceId) ||
        !text(citation.title) ||
        !text(citation.snippet) ||
        !text(citation.uri)
      )
        return false;
      try {
        const uri = new URL(citation.uri);
        return (
          uri.protocol === "https:" &&
          !uri.username &&
          !uri.password &&
          (citation.lastModified === null ||
            (text(citation.lastModified) &&
              Number.isFinite(Date.parse(citation.lastModified))))
        );
      } catch {
        return false;
      }
    }) &&
    (body.noneFound
      ? !text(body.answer) &&
        body.citations.length === 0 &&
        text(body.refusalReason)
      : text(body.answer) && body.citations.length > 0)
  );
}

/** Evaluates complete per-persona reports; failures and empty retrieval never prove permission trimming. */
export function evaluate(fixture, reports) {
  validateFixture(fixture);
  const selected = {};
  for (const persona of ["positive", "negative"]) {
    const matches = reports.filter((report) => report.persona === persona);
    requireValue(
      matches.length === 1,
      `Exactly one complete ${persona} report is required.`,
    );
    const report = matches[0];
    requireValue(
      report.teamId === fixture.teamId &&
        report.tenantId === fixture.tenantId &&
        report.fixtureSha256 === fixtureDigest(fixture) &&
        report.portalClientId === fixture.portalClientId &&
        report.api === fixture.api &&
        report.retrievalPath === fixture.retrievalPath &&
        report.objectId?.toLowerCase() ===
          fixture.personas[persona].toLowerCase(),
      "Report environment, team, path or persona does not match the approved fixture.",
    );
    const expected = fixture.knowledgeBases.flatMap((base) =>
      base.questions.map((question) => `${base.id}\0${question.id}`),
    );
    const actual = report.samples.map(
      (sample) => `${sample.knowledgeBaseId}\0${sample.questionId}`,
    );
    requireValue(
      unique(actual) &&
        actual.length === expected.length &&
        expected.every((key) => actual.includes(key)),
      "Reports must contain every golden exactly once; partial reports cannot establish G10.",
    );
    selected[persona] = report.samples;
  }
  const bases = fixture.knowledgeBases.map((base) => {
    const find = (persona, id) =>
      selected[persona].find(
        (sample) =>
          sample.knowledgeBaseId === base.id && sample.questionId === id,
      );
    const answered = (sample) =>
      sample?.status === 200 &&
      validAnswer(sample.body, base.id) &&
      !sample.body.noneFound;
    let correct = 0;
    let uncited = 0;
    let leakage = 0;
    const latency = [];
    for (const question of base.questions) {
      const positive = find("positive", question.id);
      if (
        answered(positive) &&
        question.expectedSourceIds.every((id) =>
          positive.body.citations.some((citation) => citation.sourceId === id),
        )
      )
        correct++;
      for (const persona of ["positive", "negative"]) {
        const sample = find(persona, question.id);
        if (
          sample.status === 200 &&
          text(sample.body?.answer) &&
          (!Array.isArray(sample.body?.citations) ||
            sample.body.citations.length === 0)
        )
          uncited++;
        if (
          sample.status === 200 &&
          validAnswer(sample.body, base.id) &&
          Number.isFinite(sample.latencyMs) &&
          sample.latencyMs >= 0
        )
          latency.push(sample.latencyMs);
        if (persona === "negative") {
          const content = JSON.stringify(sample.body ?? {}).toLowerCase();
          if (
            (Array.isArray(sample.body?.citations) &&
              sample.body.citations.some(
                (citation) =>
                  citation?.sourceId === base.permission.restrictedSourceId,
              )) ||
            base.permission.deniedMarkers.some((marker) =>
              content.includes(marker.toLowerCase()),
            )
          )
            leakage++;
        }
      }
    }
    const restricted = find("positive", base.permission.questionId);
    const denied = find("negative", base.permission.questionId);
    const control = find("negative", base.permission.controlQuestionId);
    const controlQuestion = base.questions.find(
      (question) => question.id === base.permission.controlQuestionId,
    );
    const permissionMeasured =
      answered(restricted) &&
      restricted.body.citations.some(
        (citation) => citation.sourceId === base.permission.restrictedSourceId,
      ) &&
      denied.status === 200 &&
      validAnswer(denied.body, base.id) &&
      answered(control) &&
      controlQuestion.expectedSourceIds.every((id) =>
        control.body.citations.some((citation) => citation.sourceId === id),
      );
    latency.sort((left, right) => left - right);
    const percentile = (fraction) =>
      latency.length ? latency[Math.ceil(latency.length * fraction) - 1] : null;
    const validResponses = ["positive", "negative"]
      .flatMap((persona) => selected[persona])
      .filter(
        (sample) =>
          sample.knowledgeBaseId === base.id &&
          sample.status === 200 &&
          validAnswer(sample.body, base.id),
      ).length;
    return {
      knowledgeBaseId: base.id,
      questions: base.questions.length,
      correct,
      correctCitationRate: correct / base.questions.length,
      uncited,
      leakage,
      permission: leakage
        ? "failed"
        : permissionMeasured
          ? "measured"
          : "not_run",
      latency: {
        n: latency.length,
        p50Ms: percentile(0.5),
        p95Ms: percentile(0.95),
      },
      status:
        validResponses === base.questions.length * 2 &&
        correct / base.questions.length >= 0.9 &&
        uncited === 0 &&
        leakage === 0 &&
        permissionMeasured
          ? "measured"
          : "failed",
    };
  });
  return {
    teamId: fixture.teamId,
    knowledgeBases: bases,
    retrieval: bases.every((base) => base.status === "measured")
      ? "measured"
      : "failed",
    preBriefAddedTime: { status: "not_run", n: 0 },
    liveVoiceG1: { status: "not_run", n: 0 },
  };
}

async function jsonRequest(url, options = {}) {
  const response = await fetch(url, {
    ...options,
    redirect: "error",
    signal: AbortSignal.timeout(120_000),
  });
  const raw = await response.text();
  requireValue(raw.length <= 1_000_000, "Response exceeds the probe limit.");
  let body;
  try {
    body = JSON.parse(raw);
  } catch {
    if (response.status !== 200) return { status: response.status, body: null };
    throw new Error(
      `Application HTTP ${response.status} returned invalid JSON.`,
    );
  }
  return { status: response.status, body };
}

/** Uses the existing pre-authorised CLI public client for the portal API scope, never a direct Graph token. */
export async function signIn(
  fixture,
  request = jsonRequest,
  sleep = delay,
  announce = console.error,
) {
  const authority = `https://login.microsoftonline.com/${fixture.tenantId}/oauth2/v2.0`;
  const post = (url, values) =>
    request(url, { method: "POST", body: new URLSearchParams(values) });
  const issued = await post(`${authority}/devicecode`, {
    client_id: cliClient,
    scope: `api://${fixture.portalClientId}/access_as_user`,
  });
  requireValue(
    issued.status === 200 &&
      text(issued.body.device_code) &&
      text(issued.body.message) &&
      Number.isFinite(issued.body.interval) &&
      issued.body.interval > 0 &&
      Number.isFinite(issued.body.expires_in) &&
      issued.body.expires_in > 0,
    `Device sign-in refused (${issued.body?.error ?? issued.status}).`,
  );
  announce(issued.body.message);
  const deadline = Date.now() + issued.body.expires_in * 1000;
  let interval = issued.body.interval;
  while (Date.now() < deadline) {
    await sleep(interval * 1000);
    if (Date.now() >= deadline) break;
    const result = await post(`${authority}/token`, {
      client_id: cliClient,
      grant_type: "urn:ietf:params:oauth:grant-type:device_code",
      device_code: issued.body.device_code,
    });
    if (result.status === 200 && text(result.body.access_token))
      return result.body.access_token;
    if (result.body?.error === "authorization_pending") continue;
    if (result.body?.error === "slow_down") {
      interval += 5;
      continue;
    }
    throw new Error(
      `Device sign-in refused (${result.body?.error ?? result.status}). No alternate identity is used.`,
    );
  }
  throw new Error(
    "Device sign-in expired. Start again only when the intended persona is ready.",
  );
}

/** Runs owner-supplied questions against the application, not a surrogate Graph/Search call. */
export async function probe(
  fixture,
  persona,
  request = jsonRequest,
  authenticate = signIn,
) {
  validateFixture(fixture);
  requireValue(
    ["positive", "negative"].includes(persona),
    "Persona must be positive or negative.",
  );
  const token = await authenticate(fixture);
  const parts = token.split(".");
  requireValue(
    parts.length === 3,
    "Expected an app-audience portal assertion.",
  );
  const claims = JSON.parse(
    Buffer.from(parts[1], "base64url").toString("utf8"),
  );
  requireValue(
    claims.tid?.toLowerCase() === fixture.tenantId.toLowerCase() &&
      claims.oid?.toLowerCase() === fixture.personas[persona].toLowerCase() &&
      [fixture.portalClientId, `api://${fixture.portalClientId}`].includes(
        claims.aud,
      ) &&
      claims.scp?.split(" ").includes("access_as_user"),
    "Wrong persona, tenant, audience or scope; no retrieval was attempted.",
  );
  const headers = {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
  // The local claims check is not signature validation. The actual API must authenticate and authorise the team.
  const team = await request(
    new URL(`/api/teams/${fixture.teamId}`, fixture.api),
    { headers },
  );
  requireValue(
    team.status === 200 && team.body?.id === fixture.teamId,
    `Application team authorisation refused (${team.status}).`,
  );
  const report = {
    version: 1,
    fixtureSha256: fixtureDigest(fixture),
    api: fixture.api,
    retrievalPath: fixture.retrievalPath,
    portalClientId: fixture.portalClientId,
    tenantId: fixture.tenantId,
    teamId: fixture.teamId,
    persona,
    objectId: claims.oid,
    startedAt: new Date().toISOString(),
    samples: [],
  };
  for (const base of fixture.knowledgeBases) {
    for (const question of base.questions) {
      const start = performance.now();
      let sample;
      try {
        const result = await request(
          new URL(fixture.retrievalPath, fixture.api),
          {
            method: "POST",
            headers,
            body: JSON.stringify({
              knowledgeBaseId: base.id,
              question: question.question,
            }),
          },
        );
        sample = {
          status: result.status,
          body: result.status === 200 ? result.body : null,
          refusalReason:
            result.status === 200
              ? (result.body?.refusalReason ?? null)
              : `Application HTTP ${result.status}; not permission-trimming evidence.`,
        };
      } catch (error) {
        sample = {
          status: null,
          body: null,
          refusalReason: `Transport or response failure (${error.name}); not permission-trimming evidence.`,
        };
      }
      report.samples.push({
        knowledgeBaseId: base.id,
        questionId: question.id,
        latencyMs: performance.now() - start,
        ...sample,
      });
      if (sample.status !== 200 || !validAnswer(sample.body, base.id)) {
        report.stoppedReason =
          sample.refusalReason ??
          "Invalid or uncited application response; retrieval stopped without retry.";
        report.finishedAt = new Date().toISOString();
        return report;
      }
    }
  }
  report.finishedAt = new Date().toISOString();
  return report;
}

async function main(args) {
  const [command, fixturePath, ...rest] = args;
  requireValue(
    ["validate", "probe", "evaluate"].includes(command) && fixturePath,
    "Usage: node tools\\KnowledgeProbe\\probe.mjs validate <goldens.json> | probe <goldens.json> <positive|negative> <result.json> | evaluate <goldens.json> <positive.json> <negative.json> <evaluation.json>",
  );
  const fixture = validateFixture(
    JSON.parse(await readFile(fixturePath, "utf8")),
  );
  if (command === "validate") {
    console.log(
      "Approved fixture structure is valid. No retrieval or permission claim.",
    );
    return;
  }
  const output = rest.at(-1);
  requireValue(
    text(output) &&
      (command === "probe" ? rest.length === 2 : rest.length === 3),
    "Supply persona/report inputs and an output path.",
  );
  requireValue(
    ![fixturePath, ...rest.slice(0, -1)].includes(output),
    "Output must not overwrite an input.",
  );
  let result;
  try {
    result =
      command === "probe"
        ? await probe(fixture, rest[0])
        : evaluate(
            fixture,
            await Promise.all(
              rest
                .slice(0, -1)
                .map(async (path) => JSON.parse(await readFile(path, "utf8"))),
            ),
          );
  } catch (error) {
    result = {
      status: "not_run",
      n: 0,
      teamId: fixture.teamId,
      reason: error.message,
    };
    process.exitCode = 1;
  }
  await writeFile(output, `${JSON.stringify(result, null, 2)}\n`, {
    flag: "wx",
  });
  if (result.stoppedReason || result.retrieval === "failed")
    process.exitCode = 1;
}

if (
  process.argv[1] &&
  import.meta.url === pathToFileURL(process.argv[1]).href
) {
  main(process.argv.slice(2)).catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
