import { readFile } from 'node:fs/promises';
import { parse } from 'yaml';
import type { CastScript } from '../domain/cast.js';
import { findRepoFile } from '../config.js';

/**
 * Loads the demo cast (SPEC 7.3). The file is re-read each call so lines can be edited during a demo without a restart.
 * @param relativePath - Path of the YAML file.
 * @returns The cast script.
 * @throws When the file is missing or has no members.
 */
export async function loadCast(relativePath: string): Promise<CastScript> {
  const path = findRepoFile(relativePath);
  if (!path) throw new Error(`Cast file '${relativePath}' was not found.`);
  const raw = parse(await readFile(path, 'utf8')) as Partial<CastScript> | null;
  if (!raw?.members?.length) throw new Error(`Cast file '${relativePath}' has no members.`);
  return { members: raw.members, sidebar: raw.sidebar };
}
