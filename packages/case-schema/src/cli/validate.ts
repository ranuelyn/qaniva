/**
 * Validate every `*.json` case fixture under one or more directories.
 * Usage: tsx src/cli/validate.ts <dir> [<dir> ...]
 * Exits non-zero if any file fails structural or semantic validation.
 */
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { validateCase } from '../validator';

function collectJsonFiles(dir: string): string[] {
  const out: string[] = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) out.push(...collectJsonFiles(full));
    else if (entry.endsWith('.json')) out.push(full);
  }
  return out;
}

const args = process.argv.slice(2);
const targets = (args.length ? args : ['fixtures']).map((d) => resolve(process.cwd(), d));

let failures = 0;
let checked = 0;

for (const target of targets) {
  for (const file of collectJsonFiles(target)) {
    checked += 1;
    let data: unknown;
    try {
      data = JSON.parse(readFileSync(file, 'utf8'));
    } catch (err) {
      failures += 1;
      console.error(`FAIL  ${file}\n      not valid JSON: ${(err as Error).message}`);
      continue;
    }
    const result = validateCase(data);
    if (result.valid) {
      console.error(`OK    ${file}`);
    } else {
      failures += 1;
      console.error(`FAIL  ${file}`);
      for (const issue of result.issues) {
        console.error(`      ${issue.path}: ${issue.message}`);
      }
    }
  }
}

console.error(`\n${checked} case file(s) checked, ${failures} failure(s).`);
process.exit(failures === 0 ? 0 : 1);
