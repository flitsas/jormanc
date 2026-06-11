import type { DashboardFamily, DashboardProcedureItem } from "../api/dashboard.schemas.js";
import { familyLabel } from "../lib/familyLabels.js";

function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  try {
    return new Intl.DateTimeFormat("es-CO", {
      dateStyle: "short",
      timeStyle: "short",
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

function SkeletonRow() {
  return (
    <tr aria-hidden="true">
      {Array.from({ length: 7 }).map((_, i) => (
        <td key={i} className="px-3 py-2">
          <div className="h-4 animate-pulse rounded bg-flit-border/40 dark:bg-flit-border-dark/40" />
        </td>
      ))}
    </tr>
  );
}

interface ProcedureDetailTableProps {
  family: DashboardFamily | null;
  items: DashboardProcedureItem[];
  total: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  error: Error | null;
  onPageChange: (page: number) => void;
  onRetry: () => void;
}

export function ProcedureDetailTable({
  family,
  items,
  total,
  page,
  pageSize,
  isLoading,
  error,
  onPageChange,
  onRetry,
}: ProcedureDetailTableProps) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  if (!family) {
    return (
      <aside
        className="flit-card flex min-h-[20rem] flex-col items-center justify-center text-center"
        aria-label="Detalle de trámites por familia"
      >
        <i className="pi pi-table text-3xl text-flit-muted mb-2" aria-hidden="true" />
        <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
          Selecciona un segmento del gráfico
        </p>
        <p className="mt-1 text-sm text-flit-muted">
          Haz clic en Matrículas, Traspasos u Otros para ver el detalle.
        </p>
      </aside>
    );
  }

  return (
    <aside className="flit-list-panel" aria-label={`Detalle de trámites — ${familyLabel(family)}`}>
      <div className="border-b border-flit-border px-4 py-3 dark:border-flit-border-dark">
        <h2 className="text-base font-semibold text-flit-heading dark:text-flit-heading-dark">
          Detalle — {familyLabel(family)}
        </h2>
        {!isLoading && !error ? (
          <p className="mt-0.5 text-sm text-flit-muted">
            {total} {total === 1 ? "trámite" : "trámites"} en el período
          </p>
        ) : null}
      </div>

      {isLoading ? (
        <div className="flit-list-panel__table" aria-label="Cargando detalle de trámites" aria-busy="true">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-flit-border text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:border-flit-border-dark">
                <th className="px-3 py-2">ID</th>
                <th className="px-3 py-2">Fecha rad.</th>
                <th className="px-3 py-2">Estado</th>
                <th className="px-3 py-2">Placa</th>
                <th className="px-3 py-2">Propietario</th>
                <th className="px-3 py-2">F. aprobación</th>
                <th className="px-3 py-2">Actualización</th>
              </tr>
            </thead>
            <tbody>
              {Array.from({ length: 5 }).map((_, i) => (
                <SkeletonRow key={i} />
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {!isLoading && error ? (
        <div className="flit-list-panel__error" role="alert">
          <div className="flit-list-panel__error-icon" aria-hidden="true">
            <i className="pi pi-exclamation-circle" />
          </div>
          <div className="flit-list-panel__error-copy">
            <p className="flit-list-panel__error-title">No se pudo cargar el detalle</p>
            <p className="flit-list-panel__error-desc">{error.message}</p>
          </div>
          <button
            type="button"
            onClick={onRetry}
            className="inline-flex items-center gap-1.5 rounded-lg border border-flit-border bg-white px-3 py-2 text-sm font-medium"
          >
            <i className="pi pi-refresh" aria-hidden="true" />
            Reintentar
          </button>
        </div>
      ) : null}

      {!isLoading && !error && items.length === 0 ? (
        <div className="flit-list-panel__empty py-12" role="status">
          <i className="pi pi-inbox text-3xl text-flit-muted mb-2" aria-hidden="true" />
          <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
            Sin trámites en esta categoría
          </p>
        </div>
      ) : null}

      {!isLoading && !error && items.length > 0 ? (
        <>
          <div className="flit-list-panel__table overflow-x-auto">
            <table className="w-full min-w-[40rem] text-sm">
              <thead>
                <tr className="border-b border-flit-border text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:border-flit-border-dark">
                  <th className="px-3 py-2">ID</th>
                  <th className="px-3 py-2">Fecha rad.</th>
                  <th className="px-3 py-2">Estado</th>
                  <th className="px-3 py-2">Placa</th>
                  <th className="px-3 py-2">Propietario</th>
                  <th className="px-3 py-2">F. aprobación</th>
                  <th className="px-3 py-2">Actualización</th>
                </tr>
              </thead>
              <tbody>
                {items.map((row) => (
                  <tr
                    key={row.id}
                    className="border-b border-flit-border/60 last:border-0 dark:border-flit-border-dark/60"
                  >
                    <td className="px-3 py-2 font-medium text-flit-primary">{row.id}</td>
                    <td className="px-3 py-2 text-flit-muted">{formatDateTime(row.submittedAt)}</td>
                    <td className="px-3 py-2">
                      <span className="rounded-full border border-flit-border px-2 py-0.5 text-xs font-medium capitalize dark:border-flit-border-dark">
                        {row.status}
                      </span>
                    </td>
                    <td className="px-3 py-2">{row.plate ?? "—"}</td>
                    <td className="px-3 py-2">{row.ownerName ?? "—"}</td>
                    <td className="px-3 py-2 text-flit-muted">{formatDateTime(row.approvedAt)}</td>
                    <td className="px-3 py-2 text-flit-muted">{formatDateTime(row.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {totalPages > 1 ? (
            <div className="flex items-center justify-between border-t border-flit-border px-4 py-3 text-sm dark:border-flit-border-dark">
              <span className="text-flit-muted">
                Página {page} de {totalPages}
              </span>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => onPageChange(page - 1)}
                  className="rounded-lg border border-flit-border px-3 py-1.5 disabled:opacity-40 dark:border-flit-border-dark"
                  aria-label="Página anterior"
                >
                  <i className="pi pi-chevron-left" aria-hidden="true" />
                </button>
                <button
                  type="button"
                  disabled={page >= totalPages}
                  onClick={() => onPageChange(page + 1)}
                  className="rounded-lg border border-flit-border px-3 py-1.5 disabled:opacity-40 dark:border-flit-border-dark"
                  aria-label="Página siguiente"
                >
                  <i className="pi pi-chevron-right" aria-hidden="true" />
                </button>
              </div>
            </div>
          ) : null}
        </>
      ) : null}
    </aside>
  );
}
