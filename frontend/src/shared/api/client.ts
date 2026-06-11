// shared/api/client.ts
// Post ADR-0004/0007 (2026-05-27): el backend se accede via Flit.Gateway (YARP)
// DEV: Vite :4001, proxy /api → gateway :4002 (ver vite.config.ts).
// En prod: Caddy hace TLS y rutea api.flit.co → gateway.
import axios from "axios";

export const TOKEN_KEY = "flit_access_token";
export const USER_KEY = "flit_user";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "/api/v1",
  headers: { "Content-Type": "application/json" },
  timeout: 10_000,
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (res) => res,
    (error) => {
    if (error.response?.status === 403) {
      const reason: string = error.response?.data?.reason ?? "";
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      const url =
        reason === "session_revoked"
          ? "/login?reason=session_revoked"
          : "/login?reason=forbidden";
      window.location.href = url;
    }
    const message = error.response?.data?.message ?? error.message ?? "Error de red";
    const enriched = new Error(message) as Error & {
      status?: number;
      data?: unknown;
    };
    enriched.status = error.response?.status;
    enriched.data = error.response?.data;
    return Promise.reject(enriched);
  },
);
