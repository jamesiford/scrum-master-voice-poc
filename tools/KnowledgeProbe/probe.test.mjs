import { test } from "node:test";
import assert from "node:assert/strict";
import {
  validateFixture,
  evaluate,
  probe,
  signIn,
  fixtureDigest,
} from "./probe.mjs";

// Synthetic tool-only records test the evaluator, never serve as customer questions or live evidence.
const fixture = () => ({
  version: 1,
  api: "https://unit.westus3.azurecontainerapps.io/",
  tenantId: "00000000-0000-0000-0000-000000000001",
  portalClientId: "00000000-0000-0000-0000-000000000002",
  teamId: "unit",
  retrievalPath: "/api/teams/unit/knowledge/ask",
  personas: {
    positive: "00000000-0000-0000-0000-000000000003",
    negative: "00000000-0000-0000-0000-000000000004",
  },
  knowledgeBases: [
    {
      id: "unit-kb",
      questions: Array.from({ length: 40 }, (_, index) => ({
        id: String(index),
        question: `Unit-only question ${index}`,
        expectedSourceIds: [index === 0 ? "restricted" : "allowed"],
      })),
      permission: {
        restrictedSourceId: "restricted",
        questionId: "0",
        controlQuestionId: "1",
        deniedMarkers: ["private-marker"],
      },
    },
  ],
});
const answer = (id) => ({
  knowledgeBaseId: "unit-kb",
  answer: "Unit-only answer",
  noneFound: false,
  refusalReason: null,
  citations: [
    {
      sourceId: id,
      title: "Unit-only source",
      snippet: "Unit-only snippet",
      uri: "https://unit.invalid/source",
      lastModified: null,
    },
  ],
});
const reports = (input) =>
  ["positive", "negative"].map((persona) => ({
    ...input,
    fixtureSha256: fixtureDigest(input),
    persona,
    objectId: input.personas[persona],
    samples: input.knowledgeBases[0].questions.map((question) => ({
      knowledgeBaseId: "unit-kb",
      questionId: question.id,
      status: 200,
      latencyMs: 10,
      body: answer(
        persona === "positive" && question.id === "0"
          ? "restricted"
          : "allowed",
      ),
    })),
  }));
const assertion = (input, persona) =>
  `unit.${Buffer.from(
    JSON.stringify({
      tid: input.tenantId,
      oid: input.personas[persona],
      aud: input.portalClientId,
      scp: "access_as_user",
    }),
  ).toString("base64url")}.unit`;

test("Validate_FewerThanFortyQuestions_RefusesBeforeNetwork", () => {
  const input = fixture();
  input.knowledgeBases[0].questions.pop();
  assert.throws(() => validateFixture(input), /40 questions/);
});
test("Validate_UnsafeOrCrossTeamPath_Refuses", () => {
  for (const path of [
    "/api/teams/other/knowledge",
    "/api/teams/unit/../other",
    "/api/teams/unit/%2e%2e",
    "/api/teams/unit/knowledge?token=x",
  ]) {
    assert.throws(
      () => validateFixture({ ...fixture(), retrievalPath: path }),
      /real team-scoped/,
    );
  }
  assert.throws(
    () => validateFixture({ ...fixture(), api: "https://unit.invalid/" }),
    /approved westus3/,
  );
});
test("Evaluate_CompleteCitedReports_MeasuresRetrievalButNotVoiceOrPreBrief", () => {
  const input = fixture();
  const result = evaluate(input, reports(input));
  assert.equal(result.retrieval, "measured");
  assert.equal(result.knowledgeBases[0].correct, 40);
  assert.equal(result.knowledgeBases[0].latency.n, 80);
  assert.equal(result.liveVoiceG1.status, "not_run");
  assert.equal(result.preBriefAddedTime.n, 0);
});
test("Evaluate_UncitedAnswer_Fails", () => {
  const input = fixture();
  const results = reports(input);
  results[0].samples[1].body.citations = [];
  assert.equal(evaluate(input, results).knowledgeBases[0].uncited, 1);
  assert.equal(evaluate(input, results).retrieval, "failed");
});
test("Evaluate_MalformedCitations_FailsWithoutPermissionClaim", () => {
  const input = fixture();
  const results = reports(input);
  results[1].samples[0].body.citations = {};
  assert.equal(evaluate(input, results).retrieval, "failed");
  assert.equal(
    evaluate(input, results).knowledgeBases[0].permission,
    "not_run",
  );
});
test("Evaluate_NoneFoundWithUncitedAnswer_Fails", () => {
  const input = fixture();
  const results = reports(input);
  results[0].samples[1].body = {
    ...answer("allowed"),
    noneFound: true,
    citations: [],
    refusalReason: "No source",
  };
  assert.equal(evaluate(input, results).knowledgeBases[0].uncited, 1);
  assert.equal(evaluate(input, results).retrieval, "failed");
});
test("Evaluate_ChangedGolden_Refuses", () => {
  const input = fixture();
  const results = reports(input);
  input.knowledgeBases[0].questions[2].question = "Changed after retrieval";
  assert.throws(() => evaluate(input, results), /does not match/);
});
test("Evaluate_LeakedMarkerWithoutCitation_Fails", () => {
  const input = fixture();
  const results = reports(input);
  results[1].samples[3].body.answer = "private-marker";
  assert.equal(evaluate(input, results).knowledgeBases[0].leakage, 1);
  assert.equal(evaluate(input, results).knowledgeBases[0].permission, "failed");
});
test("Evaluate_EmptyControlOrDeniedFailure_DoesNotProveTrimming", () => {
  for (const index of [0, 1]) {
    const input = fixture();
    const results = reports(input);
    results[1].samples[index] = {
      ...results[1].samples[index],
      status: 403,
      body: null,
    };
    assert.equal(
      evaluate(input, results).knowledgeBases[0].permission,
      "not_run",
    );
    assert.equal(evaluate(input, results).retrieval, "failed");
  }
});
test("Evaluate_MissingDuplicateOrWrongPersona_Refuses", () => {
  const input = fixture();
  for (const mutate of [
    (result) => result[0].samples.pop(),
    (result) => {
      result[0].samples[1] = result[0].samples[0];
    },
    (result) => {
      result[0].objectId = input.personas.negative;
    },
  ]) {
    const result = reports(input);
    mutate(result);
    assert.throws(() => evaluate(input, result));
  }
});
test("Probe_WrongPersona_ZeroApplicationCalls", async () => {
  const input = fixture();
  let calls = 0;
  await assert.rejects(
    () =>
      probe(
        input,
        "negative",
        async () => {
          calls++;
        },
        async () => assertion(input, "positive"),
      ),
    /Wrong persona/,
  );
  assert.equal(calls, 0);
});
test("Probe_TeamAuthorisationFails_ZeroRetrievalCalls", async () => {
  const input = fixture();
  let calls = 0;
  await assert.rejects(
    () =>
      probe(
        input,
        "positive",
        async () => {
          calls++;
          return { status: 403 };
        },
        async () => assertion(input, "positive"),
      ),
    /authorisation refused/,
  );
  assert.equal(calls, 1);
});
test("Probe_UnbuiltBoundary_Records404AndStopsWithoutRetry", async () => {
  const input = fixture();
  let calls = 0;
  const result = await probe(
    input,
    "positive",
    async () => {
      calls++;
      return calls === 1
        ? { status: 200, body: { id: input.teamId } }
        : { status: 404 };
    },
    async () => assertion(input, "positive"),
  );
  assert.equal(calls, 2);
  assert.equal(result.samples.length, 1);
  assert.match(result.stoppedReason, /not permission-trimming/);
  assert.ok(!JSON.stringify(result).includes(assertion(input, "positive")));
});
test("Probe_ServiceOrUncitedFailure_StopsWithoutRetry", async () => {
  for (const response of [
    { status: 429, body: null },
    { status: 500, body: null },
    { status: 200, body: { ...answer("allowed"), citations: [] } },
  ]) {
    const input = fixture();
    let calls = 0;
    const result = await probe(
      input,
      "positive",
      async () => {
        calls++;
        return calls === 1
          ? { status: 200, body: { id: input.teamId } }
          : response;
      },
      async () => assertion(input, "positive"),
    );
    assert.equal(calls, 2);
    assert.equal(result.samples.length, 1);
    assert.ok(result.stoppedReason);
  }
});
test("SignIn_PendingAndSlowDown_UsesPortalScopeAndRequiredIntervals", async () => {
  const input = fixture();
  const intervals = [];
  const requests = [];
  const responses = [
    {
      status: 200,
      body: {
        device_code: "unit-code",
        message: "Unit-only sign-in",
        expires_in: 900,
        interval: 5,
      },
    },
    { status: 400, body: { error: "authorization_pending" } },
    { status: 400, body: { error: "slow_down" } },
    { status: 200, body: { access_token: "unit-token" } },
  ];
  assert.equal(
    await signIn(
      input,
      async (url, options) => {
        requests.push([url, options]);
        return responses.shift();
      },
      async (milliseconds) => intervals.push(milliseconds),
      () => {},
    ),
    "unit-token",
  );
  assert.deepEqual(intervals, [5000, 5000, 10000]);
  assert.equal(
    requests[0][1].body.get("scope"),
    `api://${input.portalClientId}/access_as_user`,
  );
  assert.ok(!requests[0][1].body.has("client_secret"));
});
test("SignIn_ConsentOrPolicyRefusal_NoAlternateIdentityOrRetry", async () => {
  let calls = 0;
  await assert.rejects(
    () =>
      signIn(
        fixture(),
        async () => {
          calls++;
          return calls === 1
            ? {
                status: 200,
                body: {
                  device_code: "unit",
                  message: "unit",
                  expires_in: 900,
                  interval: 5,
                },
              }
            : { status: 400, body: { error: "authorization_declined" } };
        },
        async () => {},
        () => {},
      ),
    /No alternate identity/,
  );
  assert.equal(calls, 2);
});
