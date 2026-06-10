// shared/api/client.ts
// Post ADR-0004/0007 (2026-05-27): el backend se accede via Flit.Gateway (YARP)
// DEV: Vite :4001, proxy /api → gateway :4002 (ver vite.config.ts).
// En prod: Caddy hace TLS y rutea api.flit.co → gateway.
import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "/api/v1",
  headers: { "Content-Type": "application/json" },
  timeout: 10_000,
});

apiClient.interceptors.response.use(
  (res) => res,
  (error) => {
    const message = error.response?.data?.message ?? error.message ?? "Error de red";
    return Promise.reject(new Error(message));
  },
);
