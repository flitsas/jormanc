import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { TOKEN_KEY, USER_KEY } from "../api/client.js";

/**
 * Connects to the SignalR session hub and handles `SessionRevoked` events.
 * When the server revokes the current session (e.g. after a roles update),
 * the query cache is cleared and the user is redirected to /login.
 */
export function useSignalR() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(
        `${import.meta.env.VITE_API_BASE_URL?.replace("/api/v1", "") ?? ""}/hubs/session`,
        { accessTokenFactory: () => localStorage.getItem(TOKEN_KEY) ?? "" },
      )
      .withAutomaticReconnect()
      .configureLogging(
        import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning,
      )
      .build();

    connection.on("SessionRevoked", () => {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      queryClient.clear();
      navigate("/login?reason=session_revoked", { replace: true });
    });

    connection.start().catch(() => {
      // Silently ignore connection failures (backend may not be running in dev)
    });

    return () => {
      connection.stop().catch(() => {});
    };
  }, [queryClient, navigate]);
}
