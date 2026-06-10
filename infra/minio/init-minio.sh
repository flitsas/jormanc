#!/bin/sh
# init-minio.sh — bootstrap idempotente de MinIO para FLIT (ADR-0016 Aceptado 2026-05-27).
# Crea buckets, lifecycle, política y service account flit-core.
# Re-ejecuciones son seguras (todos los comandos son idempotentes con mc).

set -e

echo "[init-minio] Esperando MinIO en minio:9000..."
mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"

echo "[init-minio] Creando buckets..."
for bucket in tramites identidad recibos temporales; do
  mc mb --ignore-existing "local/$bucket"
  mc anonymous set none "local/$bucket"
  # SSE-S3: cifrado en reposo AES-256 (Habeas Data Ley 1581)
  mc encrypt set sse-s3 "local/$bucket" || echo "[init-minio] WARN: encrypt set falló en $bucket (puede requerir KMS)"
done

echo "[init-minio] Configurando lifecycle..."
# temporales: expire 7 días
mc ilm rule add --expire-days 7 local/temporales 2>/dev/null || true
# recibos: expire 5 años (1825 días) — compliance contable
mc ilm rule add --expire-days 1825 local/recibos 2>/dev/null || true

echo "[init-minio] Configurando política mínima flit-core-policy..."
cat > /tmp/core-api-policy.json <<'EOF'
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": [
      "s3:GetObject", "s3:PutObject", "s3:DeleteObject",
      "s3:GetObjectAttributes",
      "s3:ListBucket", "s3:GetBucketLocation"
    ],
    "Resource": [
      "arn:aws:s3:::tramites/*",  "arn:aws:s3:::tramites",
      "arn:aws:s3:::identidad/*", "arn:aws:s3:::identidad",
      "arn:aws:s3:::recibos/*",   "arn:aws:s3:::recibos",
      "arn:aws:s3:::temporales/*","arn:aws:s3:::temporales"
    ]
  }]
}
EOF
mc admin policy create local flit-core-policy /tmp/core-api-policy.json 2>/dev/null || \
  mc admin policy update local flit-core-policy /tmp/core-api-policy.json

echo "[init-minio] Creando service account flit-core (idempotente)..."
mc admin user svcacct add local "$MINIO_ROOT_USER" \
  --access-key "$FLIT_CORE_ACCESS_KEY" \
  --secret-key "$FLIT_CORE_SECRET_KEY" \
  --policy /tmp/core-api-policy.json 2>/dev/null || \
  echo "[init-minio] Service account ya existe (OK)."

echo "[init-minio] Bootstrap completado."
echo "  Buckets:        tramites, identidad, recibos, temporales"
echo "  Lifecycle:      temporales=7d, recibos=5y"
echo "  Service acct:   $FLIT_CORE_ACCESS_KEY (policy: flit-core-policy)"
echo "  Console (DEV):  http://localhost:4010 (login con MINIO_ROOT_USER)"
