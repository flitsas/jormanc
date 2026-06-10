# Reinicia Postgres local (Docker). La base arranca VACIA: el esquema lo crean
# las migraciones EF Core (`pnpm migrate`) a medida que se implementan features.
$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $Root

Write-Host "Bajando volumen postgres..."
docker compose -f infra/docker-compose.yml down -v postgres

Write-Host "Levantando infra..."
pnpm docker:up:infra

Start-Sleep -Seconds 8

Write-Host "Postgres listo (base vacia). Aplica migraciones con: pnpm migrate"
Write-Host "Luego ejecuta: pnpm dev"
