import { createServer } from 'node:http';
import { loadConfig } from './config.js';
import { CastRoutes } from './routes/castRoutes.js';
import { createRouter } from './routes/router.js';
import { loadCast } from './services/castFile.js';
import { AzureSpeechSynthesizer } from './services/speech.js';

const config = loadConfig();
const cast = new CastRoutes(
  () => loadCast(config.castFile),
  new AzureSpeechSynthesizer(config.speechEndpoint, config.tenantId),
);

createServer(createRouter(config.publicDir, cast)).listen(config.port, () => {
  console.log(`meeting-bridge listening on http://localhost:${config.port}`);
});
