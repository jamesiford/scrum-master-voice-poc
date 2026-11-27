import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { once } from 'node:events';
import { test } from 'node:test';
import { NodeTracerProvider } from '@opentelemetry/sdk-trace-node';
import { InMemorySpanExporter, SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { traceRequest } from './telemetry.js';

test('traceRequest_W3cParent_ExportsTeamAndStatusWithoutUrlOrContent', async () => {
  const exporter = new InMemorySpanExporter();
  const provider = new NodeTracerProvider({ spanProcessors: [new SimpleSpanProcessor(exporter)] });
  provider.register();
  const server = createServer((req, res) => {
    void traceRequest(req, res, () => {
      res.end('synthetic-private-output');
      return Promise.resolve();
    });
  });
  server.listen(0, '127.0.0.1');
  await once(server, 'listening');
  const address = server.address();
  assert.ok(address && typeof address !== 'string');
  try {
    await fetch(`http://127.0.0.1:${address.port}/cast/line?token=synthetic-private-token`, {
      method: 'POST',
      headers: {
        traceparent: '00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01',
        'x-zava-team': 'team-golden',
      },
      body: 'synthetic-private-utterance',
    });
    await provider.forceFlush();
    const spans = exporter.getFinishedSpans();
    assert.equal(spans.length, 1);
    const span = spans[0];
    assert.ok(span);
    assert.equal(span.spanContext().traceId, 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa');
    assert.equal(span.attributes['team.id'], 'team-golden');
    assert.equal(span.parentSpanContext?.spanId, 'bbbbbbbbbbbbbbbb');
    assert.equal(span.attributes['http.response.status_code'], 200);
    assert.doesNotMatch(JSON.stringify(span.attributes), /synthetic-private/);
    assert.equal(span.events.length, 0);
    await fetch(`http://127.0.0.1:${address.port}/healthz`);
    await provider.forceFlush();
    assert.equal(exporter.getFinishedSpans().length, 1);
  } finally {
    server.close();
    await once(server, 'close');
    await provider.shutdown();
  }
});
