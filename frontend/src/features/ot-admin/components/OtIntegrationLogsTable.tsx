import { useState } from "react";
import { useOtIntegrationLogs } from "../api/ot-admin.api.js";
import type { OtIntegrationLog } from "../api/ot-admin.schemas.js";

interface OtIntegrationLogsTableProps {
  otId: string;
}

function formatJson(raw: string | null | undefined): string {
  if (!raw) return "—";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

function LogRow({ log }: { log: OtIntegrationLog }) {
  const [expanded, setExpanded] = useState(false);
  const label = `${log.eventType} — ${log.procedureRef ?? "sin referencia"}`;

  return (
    <>
      <tr
        className="cursor-pointer border-b border-slate-100 hover:bg-slate-50 dark:border-flit-border-dark/50 dark:hover:bg-slate-800/40"
        onClick={() => setExpanded((e) => !e)}
        onKeyDown={(e) => e.key === "Enter" && setExpanded((x) => !x)}
        tabIndex={0}
        role="button"
        aria-expanded={expanded}
        aria-label={label}
      >
        <td className="px-3 py-2 text-xs">{log.eventType}</td>
        <td className="px-3 py-2 text-xs font-mono">{log.procedureRef ?? "—"}</td>
        <td className="px-3 py-2 text-xs">
          <span
            className={`rounded px-1.5 py-0.5 font-medium ${
              (log.httpStatus ?? 0) >= 200 && (log.httpStatus ?? 0) < 300
                ? "bg-emerald-100 text-emerald-800"
                : "bg-red-100 text-red-800"
            }`}
          >
            {log.httpStatus ?? "—"}
          </span>
        </td>
        <td className="px-3 py-2 text-xs">{log.durationMs ?? "—"} ms</td>
        <td className="px-3 py-2 text-xs text-flit-muted">
          {new Date(log.loggedAt).toLocaleString()}
        </td>
        <td className="px-3 py-2 text-xs">
          <i
            className={`pi ${expanded ? "pi-chevron-up" : "pi-chevron-down"}`}
            aria-hidden="true"
          />
        </td>
      </tr>
      {expanded && (
        <tr>
          <td colSpan={6} className="bg-slate-50 px-4 py-3 dark:bg-slate-900/50">
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <p className="mb-1 text-xs font-semibold uppercase text-flit-muted">Request</p>
                <pre className="max-h-48 overflow-auto rounded bg-slate-900 p-2 text-xs text-emerald-300">
                  {formatJson(log.requestPayload)}
                </pre>
              </div>
              <div>
                <p className="mb-1 text-xs font-semibold uppercase text-flit-muted">Response</p>
                <pre className="max-h-48 overflow-auto rounded bg-slate-900 p-2 text-xs text-emerald-300">
                  {formatJson(log.responsePayload)}
                </pre>
              </div>
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

export function OtIntegrationLogsTable({ otId }: OtIntegrationLogsTableProps) {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading, error, refetch } = useOtIntegrationLogs(otId, page, pageSize);

  if (isLoading) {
    return (
      <p
        className="text-sm text-flit-muted"
        aria-busy="true"
        aria-label="Cargando logs de integración"
      >
        Cargando logs de integración Quipux…
      </p>
    );
  }

  if (error) {
    return (
      <div role="alert" className="text-sm text-red-600">
        {error.message}
        <button type="button" onClick={() => void refetch()} className="ml-2 underline">
          Reintentar
        </button>
      </div>
    );
  }

  const logs = data?.data ?? [];
  const total = data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  if (logs.length === 0) {
    return (
      <p className="text-sm text-flit-muted" role="status">
        Sin logs de integración Quipux para este OT.
      </p>
    );
  }

  return (
    <div role="region" aria-label="Logs de integración Quipux">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left text-xs font-semibold uppercase text-flit-muted">
              <th className="px-3 py-2">Evento</th>
              <th className="px-3 py-2">Referencia</th>
              <th className="px-3 py-2">Status</th>
              <th className="px-3 py-2">Duración</th>
              <th className="px-3 py-2">Timestamp</th>
              <th className="px-3 py-2" aria-label="Expandir" />
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <LogRow key={log.id} log={log} />
            ))}
          </tbody>
        </table>
      </div>

      {total > pageSize && (
        <div className="mt-3 flex items-center justify-between border-t border-slate-200 pt-3 dark:border-flit-border-dark">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-40"
          >
            Anterior
          </button>
          <span className="text-sm text-flit-muted">
            Página {page} de {totalPages}
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-40"
          >
            Siguiente
          </button>
        </div>
      )}
    </div>
  );
}
