import { createServer } from 'node:http';
import { loadConfig } from './config.js';
import { CastRoutes } from './routes/castRoutes.js';
import { createRouter } from './routes/router.js';
import { TeamsRoutes } from './routes/teamsRoutes.js';
import { createAcsTokenService } from './services/acs.js';
import { loadCast } from './services/castFile.js';
import { AzureSpeechSynthesizer } from './services/speech.js';
import { TeamsLauncher } from './services/teamsLauncher.js';
import { startTelemetry } from './services/telemetry.js';

startTelemetry();
const config = loadConfig();
const cast = new CastRoutes(
  (kind) => loadCast(kind === 'Retrospective' ? config.retroCastFile : config.castFile),
  new AzureSpeechSynthesizer(config.speechEndpoint, config.tenantId),
);
const teams = new TeamsRoutes(
  new TeamsLauncher(`http://localhost:${config.port}`, config.chromiumPath),
  createAcsTokenService(config.acsEndpoint, config.tenantId),
  config.bridgeKey,
);

createServer(createRouter(config.publicDir, cast, teams)).listen(config.port, () => {
  console.log(`meeting-bridge listening on http://localhost:${config.port}`);
});
