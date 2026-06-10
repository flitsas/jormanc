#!/usr/bin/env bash
# Reinicia Postgres local (Docker) y aplica el schema SQL del shell.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

echo "Bajando volumen postgres..."
docker compose -f infra/docker-compose.yml down -v postgres

echo "Levantando infra..."
pnpm docker:up:infra

sleep 8

echo "Aplicando schema flit-shell-schema.sql..."
docker compose -f infra/docker-compose.yml exec -T postgres \
  psql -U flit -d flit_dev -v ON_ERROR_STOP=1 \
  < scripts/sql/flit-shell-schema.sql

echo "Listo. Ejecuta: pnpm dev"
