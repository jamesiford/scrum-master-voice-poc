import { useAzureMonitor } from '@azure/monitor-opentelemetry';
import { context, metrics, propagation, ROOT_CONTEXT, SpanKind, trace } from '@opentelemetry/api';
import type { IncomingMessage, ServerResponse } from 'node:http';

const duration = metrics
  .getMeter('Zava.Scrum.Bridge')
  .createHistogram('zava.scrum.hop.duration', { unit: 'ms' });
/** Enables metadata-only Azure Monitor export; no automatic URL, SDK or log-body capture. */
export function startTelemetry(): void {
  if (process.env['Telemetry__CaptureContent']?.toLowerCase() === 'true')
    throw new Error('Telemetry content capture is prohibited. Export metadata only.');
  if (!process.env['APPLICATIONINSIGHTS_CONNECTION_STRING']) return;
  process.env['OTEL_SERVICE_NAME'] = 'zava-meeting-bridge';
  useAzureMonitor({
    samplingRatio: 1,
    tracesPerSecond: 0,
    enableLiveMetrics: false,
    enableStandardMetrics: false,
    instrumentationOptions: {
      http: { enabled: false },
      azureSdk: { enabled: false },
      mongoDb: { enabled: false },
      mySql: { enabled: false },
      postgreSql: { enabled: false },
      redis: { enabled: false },
      redis4: { enabled: false },
      bunyan: { enabled: false },
      winston: { enabled: false },
    },
  });
}

/** Traces a fixed bridge route under the caller's W3C parent, never its URL, payload or error text. */
export function traceRequest(
  req: IncomingMessage,
  res: ServerResponse,
  route: () => Promise<void>,
): Promise<void> {
  const parent = propagation.extract(ROOT_CONTEXT, req.headers);
  const team = req.headers['x-zava-team'];
  if (
    typeof team !== 'string' ||
    !/^[a-zA-Z0-9_-]{1,128}$/.test(team) ||
    !trace.getSpanContext(parent)
  )
    return route();
  const attributes = { 'team.id': team, hop: 'bridge.http' };
  return context.with(parent, () =>
    trace
      .getTracer('Zava.Scrum.Bridge')
      .startActiveSpan('bridge.http', { kind: SpanKind.SERVER, attributes }, async (span) => {
        const started = performance.now();
        try {
          await route();
        } finally {
          span.setAttribute('http.response.status_code', res.statusCode);
          duration.record(performance.now() - started, attributes);
          span.end();
        }
      }),
  );
}
