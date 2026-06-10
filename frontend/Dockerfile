# Build context: monorepo root (el lockfile pnpm vive en la raiz).
FROM node:22-alpine AS builder
RUN corepack enable
WORKDIR /app
# Manifiestos primero para cachear la instalacion de dependencias.
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY frontend/package.json ./frontend/package.json
RUN pnpm install --frozen-lockfile
COPY frontend/ ./frontend/
WORKDIR /app/frontend
ARG VITE_API_BASE_URL=/api/v1
ARG VITE_DEV_USER_ID=01900000-100b-7001-8001-000000000001
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_DEV_USER_ID=$VITE_DEV_USER_ID
RUN pnpm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=builder /app/frontend/dist /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
  CMD wget -qO- http://localhost/ || exit 1
