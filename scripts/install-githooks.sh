#!/usr/bin/env bash
# Activa los hooks de .githooks/ en este clon de git.
# Uso: bash scripts/install-githooks.sh
set -euo pipefail

cd "$(dirname "$0")/.."

git config core.hooksPath .githooks
chmod +x .githooks/* 2>/dev/null || true

echo "OK Hooks activados (core.hooksPath = .githooks)"
echo "   Hooks instalados:"
ls -1 .githooks/ | sed 's/^/     - /'
echo ""
echo "Para desactivar: git config --unset core.hooksPath"
