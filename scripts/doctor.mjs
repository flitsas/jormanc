#!/usr/bin/env node
/**
 * doctor.mjs — Diagnóstico del entorno local FLIT (puertos esquema 4xxx DEV).
 */
import { execSync } from 'node:child_process';
import net from 'node:net';

const OK = '✓';
const WARN = '⚠';
const FAIL = '✗';
const DIM = '·';

const DEV_PORTS = [
  { port: 4001, label: 'frontend (Vite)' },
  { port: 4002, label: 'gateway (YARP)' },
  { port: 4003, label: 'core-api interno (Flit.Api)' },
  { port: 4012, label: 'python-ml (opcional)' },
  { port: 5432, label: 'postgres (docker)' },
  { port: 4006, label: 'redis (docker)' },
  { port: 4007, label: 'rabbitmq amqp (docker)' },
  { port: 4008, label: 'rabbitmq UI (docker)' },
  { port: 4009, label: 'minio S3 (docker)' },
  { port: 4010, label: 'minio console (docker)' },
  { port: 4011, label: 'mailhog smtp (docker)' },
];

function run(cmd) {
  try {
    return execSync(cmd, { stdio: ['ignore', 'pipe', 'pipe'] }).toString().trim();
  } catch {
    return null;
  }
}

function checkTool(name, cmd) {
  const out = run(cmd);
  if (out) console.log(`  ${OK} ${name.padEnd(10)} ${out.split('\n')[0]}`);
  else console.log(`  ${FAIL} ${name.padEnd(10)} no encontrado`);
}

function probePort(port) {
  return new Promise((resolve) => {
    const sock = net.createConnection({ host: '127.0.0.1', port, timeout: 400 });
    sock.once('connect', () => { sock.end(); resolve('busy'); });
    sock.once('timeout', () => { sock.destroy(); resolve('free'); });
    sock.once('error', () => resolve('free'));
  });
}

async function checkPorts() {
  for (const t of DEV_PORTS) {
    const state = await probePort(t.port);
    const icon = state === 'busy' ? OK : DIM;
    console.log(`  ${icon} :${String(t.port).padEnd(6)} ${state === 'busy' ? 'en uso' : 'libre '}  ${t.label}`);
  }
}

function checkDockerInfra() {
  const out = run('docker ps --format "{{.Names}}|{{.Status}}"');
  if (out === null) {
    console.log(`  ${WARN} docker no disponible`);
    return;
  }
  const required = ['postgres', 'redis', 'rabbitmq', 'minio', 'mailhog'];
  const running = new Set(
    out.split('\n').map((l) => l.split('|')[0]?.toLowerCase() || '').filter(Boolean),
  );
  for (const svc of required) {
    const hit = [...running].find((n) => n.includes(svc));
    if (hit) console.log(`  ${OK} ${svc.padEnd(10)} ${hit}`);
    else console.log(`  ${DIM} ${svc.padEnd(10)} no corriendo (pnpm docker:up:infra)`);
  }
}

async function main() {
  console.log('FLIT doctor — puertos DEV (esquema 4xxx)\n');
  console.log('Toolchain:');
  checkTool('node', 'node --version');
  checkTool('pnpm', 'pnpm --version');
  checkTool('dotnet', 'dotnet --version');
  checkTool('docker', 'docker --version');
  checkTool('uv', 'uv --version');
  console.log('');
  console.log('Puertos DEV:');
  await checkPorts();
  console.log('');
  console.log('Docker infra:');
  checkDockerInfra();
  console.log('');
  console.log('Flujo: pnpm docker:up:infra && pnpm dev');
  console.log('  UI http://localhost:4001  API http://localhost:4002');
}

main().catch((e) => {
  console.error('doctor falló:', e);
  process.exitCode = 1;
});
