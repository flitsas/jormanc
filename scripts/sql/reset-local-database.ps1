# Reinicia Postgres local (Docker) y aplica el schema SQL del shell.
$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $Root

Write-Host "Bajando volumen postgres..."
docker compose -f infra/docker-compose.yml down -v postgres

Write-Host "Levantando infra..."
pnpm docker:up:infra

Start-Sleep -Seconds 8

Write-Host "Aplicando schema flit-shell-schema.sql..."
Get-Content -Raw (Join-Path $PSScriptRoot "flit-shell-schema.sql") |
    docker compose -f infra/docker-compose.yml exec -T postgres `
        psql -U flit -d flit_dev -v ON_ERROR_STOP=1

Write-Host "Listo. Ejecuta: pnpm dev"
