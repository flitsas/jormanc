#!/usr/bin/env bash
# Reinicia Postgres local (Docker). La base arranca VACIA: el esquema lo crean
# las migraciones EF Core (`pnpm migrate`) a medida que se implementan features.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

echo "Bajando volumen postgres..."
docker compose -f infra/docker-compose.yml down -v postgres

echo "Levantando infra..."
pnpm docker:up:infra

sleep 8

echo "Postgres listo (base vacia). Aplica migraciones con: pnpm migrate"
echo "Luego ejecuta: pnpm dev"
