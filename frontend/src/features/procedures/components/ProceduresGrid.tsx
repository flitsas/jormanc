import { Link } from "react-router-dom";
import type { ProcedureListItem } from "../api/procedures.schemas.js";

const STATUS_OPTIONS = [
  { value: "", label: "Todos los estados" },
  { value: "draft", label: "Borrador" },
  { value: "submitted", label: "Enviado" },
  { value: "processing_documents", label: "Procesando documentos" },
  { value: "pending_signatures", label: "Pendiente firmas" },
  { value: "approved", label: "Aprobado" },
  { value: "rejected", label: "Rechazado" },
  { value: "cancelled", label: "Anulado" },
] as const;

function formatDate(iso: string): string {
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
      {[1, 2, 3, 4].map((i) => (
        <td key={i} className="px-4 py-3">
          <div className="h-4 animate-pulse rounded bg-flit-border/40 dark:bg-flit-border-dark/40" />
        </td>
      ))}
    </tr>
  );
}

export interface ProceduresGridFilters {
  status: string;
  fechaFrom: string;
}

interface ProceduresGridProps {
  items: ProcedureListItem[];
  total: number;
  page: number;
  pageSize: number;
  filters: ProceduresGridFilters;
  isLoading: boolean;
  error: Error | null;
  onFiltersChange: (filters: ProceduresGridFilters) => void;
  onPageChange: (page: number) => void;
  onRetry: () => void;
}

export function ProceduresGrid({
  items,
  total,
  page,
  pageSize,
  filters,
  isLoading,
  error,
  onFiltersChange,
  onPageChange,
  onRetry,
}: ProceduresGridProps) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  function handleStatusChange(e: React.ChangeEvent<HTMLSelectElement>) {
    onFiltersChange({ ...filters, status: e.target.value });
  }

  function handleFechaFromChange(e: React.ChangeEvent<HTMLInputElement>) {
    onFiltersChange({ ...filters, fechaFrom: e.target.value });
  }

  return (
    <div className="flit-list-panel">
      <div className="flit-list-panel__toolbar flex flex-wrap items-end gap-3">
        <div className="flex flex-col gap-1">
          <label htmlFor="procedures-filter-status" className="text-xs font-medium text-flit-muted">
            Estado
          </label>
          <select
            id="procedures-filter-status"
            value={filters.status}
            onChange={handleStatusChange}
            className="rounded-lg border border-flit-border bg-white px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-900"
            aria-label="Filtrar por estado"
          >
            {STATUS_OPTIONS.map((opt) => (
              <option key={opt.value || "all"} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="procedures-filter-fecha-from" className="text-xs font-medium text-flit-muted">
            Desde
          </label>
          <input
            id="procedures-filter-fecha-from"
            type="date"
            value={filters.fechaFrom}
            onChange={handleFechaFromChange}
            className="rounded-lg border border-flit-border bg-white px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-900"
            aria-label="Filtrar desde fecha"
          />
        </div>
      </div>

      {isLoading ? (
        <div className="flit-list-panel__table" aria-label="Cargando trámites" aria-busy="true">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-flit-border text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:border-flit-border-dark">
                <th className="px-4 py-3">ID compuesto</th>
                <th className="px-4 py-3">Estado</th>
                <th className="px-4 py-3">Creado</th>
                <th className="px-4 py-3">Acción</th>
              </tr>
            </thead>
            <tbody>
              {Array.from({ length: 5 }).map((_, i) => (
                <SkeletonRow key={i} />
              ))}
            </tbody>
          </table>
        </div>
      ) : error ? (
        <div className="flit-list-panel__error" role="alert">
          <div className="flit-list-panel__error-icon" aria-hidden="true">
            <i className="pi pi-exclamation-circle" />
          </div>
          <div className="flit-list-panel__error-copy">
            <p className="flit-list-panel__error-title">No se pudo cargar el listado</p>
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
      ) : items.length === 0 ? (
        <div className="flit-list-panel__empty py-12 text-center" role="status">
          <i className="pi pi-folder-open text-3xl text-flit-muted mb-2" aria-hidden="true" />
          <p className="text-flit-heading dark:text-flit-heading-dark font-medium">
            No hay trámites en este rango
          </p>
          <p className="text-sm text-flit-muted mt-1">Ajusta los filtros o crea un nuevo trámite.</p>
        </div>
      ) : (
        <div className="flit-list-panel__table">
          <table className="w-full text-sm">
            <caption className="sr-only">
              Listado de trámites del tenant con paginación server-side
            </caption>
            <thead>
              <tr className="border-b border-flit-border text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:border-flit-border-dark">
                <th scope="col" className="px-4 py-3">
                  ID compuesto
                </th>
                <th scope="col" className="px-4 py-3">
                  Estado
                </th>
                <th scope="col" className="px-4 py-3">
                  Creado
                </th>
                <th scope="col" className="px-4 py-3">
                  Acción
                </th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr
                  key={item.id}
                  className="border-b border-flit-border/50 hover:bg-flit-primary/5 dark:border-flit-border-dark/50"
                >
                  <td className="px-4 py-3 font-mono text-xs font-medium text-flit-heading dark:text-flit-heading-dark">
                    {item.compositeId}
                  </td>
                  <td className="px-4 py-3 capitalize">{item.status.replaceAll("_", " ")}</td>
                  <td className="px-4 py-3 text-flit-muted">{formatDate(item.createdAt)}</td>
                  <td className="px-4 py-3">
                    <Link
                      to={`/procedures/${item.id}`}
                      className="text-flit-primary hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-flit-primary"
                    >
                      Ver detalle
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {!isLoading && !error && total > pageSize ? (
        <nav
          className="flit-list-panel__pagination flex items-center justify-between border-t border-flit-border px-4 py-3 dark:border-flit-border-dark"
          aria-label="Paginación de trámites"
        >
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
            className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-40"
            aria-label="Página anterior"
          >
            Anterior
          </button>
          <span className="text-sm text-flit-muted" role="status">
            Página {page} de {totalPages} · {total} trámites
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => onPageChange(page + 1)}
            className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-40"
            aria-label="Página siguiente"
          >
            Siguiente
          </button>
        </nav>
      ) : null}
    </div>
  );
}
