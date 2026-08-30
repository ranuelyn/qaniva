import 'reflect-metadata';
import { Logger } from '@nestjs/common';
import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { loadConfig } from './config';

async function bootstrap(): Promise<void> {
  const config = loadConfig();
  const app = await NestFactory.create(AppModule, { logger: ['log', 'warn', 'error'] });
  app.enableShutdownHooks();
  await app.listen(config.API_PORT, config.API_HOST);
  Logger.log(`qaniva-api listening on http://${config.API_HOST}:${config.API_PORT}`, 'Bootstrap');
}

void bootstrap();
