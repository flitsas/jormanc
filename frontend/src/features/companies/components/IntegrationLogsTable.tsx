import { useState } from "react";
import { useIntegrationLogs } from "../api/companies.api.js";
import type { IntegrationLog } from "../api/companies.schemas.js";

interface IntegrationLogsTableProps {
  tenantId: string;
}

function formatJson(raw: string | null | undefined): string {
  if (!raw) return "—";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

function LogRow({ log }: { log: IntegrationLog }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <>
      <tr
        className="cursor-pointer border-b border-slate-100 hover:bg-slate-50 dark:border-flit-border-dark/50 dark:hover:bg-slate-800/40"
        onClick={() => setExpanded((e) => !e)}
        onKeyDown={(e) => e.key === "Enter" && setExpanded((x) => !x)}
        tabIndex={0}
        role="button"
        aria-expanded={expanded}
      >
        <td className="px-3 py-2 text-xs">{log.provider}</td>
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
        <td className="px-3 py-2 text-xs">{log.durationMs} ms</td>
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
          <td colSpan={5} className="bg-slate-50 px-4 py-3 dark:bg-slate-900/50">
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

export function IntegrationLogsTable({ tenantId }: IntegrationLogsTableProps) {
  const { data, isLoading, error, refetch } = useIntegrationLogs(tenantId);

  if (isLoading) {
    return (
      <p className="text-sm text-flit-muted" aria-busy="true">
        Cargando logs de integración…
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

  if (logs.length === 0) {
    return (
      <p className="text-sm text-flit-muted" role="status">
        No hay logs de integración para este tenant.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto" role="tabpanel">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b text-left text-xs font-semibold uppercase text-flit-muted">
            <th className="px-3 py-2">Proveedor</th>
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
  );
}
