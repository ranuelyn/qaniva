import { z } from 'zod';

/**
 * Backend configuration. No secrets are committed — see `.env.example` at the repo
 * root. Auth is not implemented in the MVP foundation; the JWT_* values are parsed
 * so the shape is ready (ADR-005 / blueprint §19 "auth-ready structure").
 */
const configSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
  API_PORT: z.coerce.number().int().positive().default(3000),
  API_HOST: z.string().default('0.0.0.0'),
  DATABASE_URL: z.string().optional(),
  JWT_SECRET: z.string().optional(),
  JWT_EXPIRES_IN: z.coerce.number().int().positive().default(3600),
  AI_PROVIDER: z.enum(['stub', 'openai', 'anthropic']).default('stub'),
  AI_REQUEST_TIMEOUT_MS: z.coerce.number().int().positive().default(8000),
  ANALYTICS_SINK: z.enum(['stdout', 'http']).default('stdout'),
  ANALYTICS_HTTP_ENDPOINT: z.string().url().optional(),
});

export type AppConfig = z.infer<typeof configSchema>;

export function loadConfig(env: NodeJS.ProcessEnv = process.env): AppConfig {
  const parsed = configSchema.safeParse(env);
  if (!parsed.success) {
    throw new Error(`Invalid environment configuration: ${parsed.error.message}`);
  }
  return parsed.data;
}

export const CONFIG = Symbol('QANIVA_APP_CONFIG');
