import { existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

/** Bridge settings from environment variables (set by the Aspire AppHost, or by hand with `npm start`). */
export interface BridgeConfig {
  port: number;
  /** Repository-relative or absolute path of the demo cast file (`Demo__CastFile`). */
  castFile: string;
  /** Repository-relative or absolute path of the retrospective cast file (`Demo__RetroCastFile`). */
  retroCastFile: string;
  /** Azure Speech resource endpoint used for cast voices. */
  speechEndpoint: string;
  /** Entra tenant to sign in to, when several accounts are signed in. */
  tenantId: string | undefined;
  /** Directory holding the browser bundle. */
  publicDir: string;
  /** ACS connection string (secret), if used instead of Entra. */
  acsConnectionString: string | undefined;
  /** ACS endpoint for Entra sign-in (Acs__Endpoint). */
  acsEndpoint: string | undefined;
  /** Shared secret for API-to-bridge control calls (Bridge__ApiKey). */
  bridgeKey: string;
  /** Chromium executable inside the container; empty locally. */
  chromiumPath: string | undefined;
}

/**
 * Reads the configuration.
 * @param env - Environment variables.
 * @returns The settings.
 */
export function loadConfig(env: NodeJS.ProcessEnv = process.env): BridgeConfig {
  return {
    port: Number(env['PORT'] ?? 5090),
    castFile: env['Demo__CastFile'] ?? 'demo/cast/payments-standup.yaml',
    retroCastFile: env['Demo__RetroCastFile'] ?? 'demo/cast/payments-retro.yaml',
    speechEndpoint: (
      env['SPEECH_ENDPOINT'] ?? 'https://fdy-mcm-scrum-demo.cognitiveservices.azure.com'
    ).replace(/\/$/, ''),
    tenantId: env['AZURE_TENANT_ID'] || undefined,
    acsConnectionString: env['ACS_CONNECTION_STRING'] || undefined,
    acsEndpoint: env['Acs__Endpoint'] || undefined,
    bridgeKey: env['Bridge__ApiKey'] ?? '',
    chromiumPath: env['CHROMIUM_PATH'] || undefined,
    publicDir: resolve(dirname(fileURLToPath(import.meta.url)), '..', 'public'),
  };
}

/**
 * Finds a repo file by walking up from the working directory, so the bridge runs from any folder.
 * @param relativePath - For example `demo/cast/payments-standup.yaml`.
 * @returns The absolute path, or `undefined` when not found.
 */
export function findRepoFile(relativePath: string): string | undefined {
  if (resolve(relativePath) === relativePath)
    return existsSync(relativePath) ? relativePath : undefined;
  for (let dir = process.cwd(); ; dir = dirname(dir)) {
    const candidate = join(dir, relativePath);
    if (existsSync(candidate)) return candidate;
    if (dirname(dir) === dir) return undefined;
  }
}
