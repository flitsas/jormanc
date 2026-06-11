import type { CompanyListItem } from "../api/companies.schemas.js";

const STATUS_CLASSES: Record<string, string> = {
  active:
    "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300",
  suspended: "bg-slate-100 text-slate-600 border-slate-200 dark:bg-slate-800 dark:text-slate-400",
};

interface CompaniesTableProps {
  companies: CompanyListItem[];
  isLoading: boolean;
  error: Error | null;
  onRetry: () => void;
  onEdit: (company: CompanyListItem) => void;
}

function SkeletonRow() {
  return (
    <tr aria-hidden="true">
      {[1, 2, 3, 4, 5].map((i) => (
        <td key={i} className="px-4 py-3">
          <div className="h-4 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        </td>
      ))}
    </tr>
  );
}

export function CompaniesTable({
  companies,
  isLoading,
  error,
  onRetry,
  onEdit,
}: CompaniesTableProps) {
  if (isLoading) {
    return (
      <div className="flit-list-panel__table" aria-label="Cargando compañías" aria-busy="true">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 dark:border-flit-border-dark text-left text-xs font-semibold uppercase tracking-wide text-flit-muted">
              <th className="px-4 py-3">NIT</th>
              <th className="px-4 py-3">Nombre</th>
              <th className="px-4 py-3">Tenant</th>
              <th className="px-4 py-3">Estado</th>
              <th className="px-4 py-3">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 5 }).map((_, i) => (
              <SkeletonRow key={i} />
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  if (error) {
    return (
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
          className="inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium"
        >
          <i className="pi pi-refresh" aria-hidden="true" />
          Reintentar
        </button>
      </div>
    );
  }

  if (companies.length === 0) {
    return (
      <div className="flit-list-panel__empty py-12 text-center" role="status">
        <i className="pi pi-building text-3xl text-flit-muted mb-2" aria-hidden="true" />
        <p className="text-flit-heading dark:text-flit-heading-dark font-medium">
          No hay compañías registradas
        </p>
        <p className="text-sm text-flit-muted mt-1">
          Crea la primera compañía con el botón superior.
        </p>
      </div>
    );
  }

  return (
    <div className="flit-list-panel__table">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 dark:border-flit-border-dark text-left text-xs font-semibold uppercase tracking-wide text-flit-muted">
            <th className="px-4 py-3">NIT</th>
            <th className="px-4 py-3">Nombre</th>
            <th className="px-4 py-3">Tenant</th>
            <th className="px-4 py-3">Estado</th>
            <th className="px-4 py-3">Acciones</th>
          </tr>
        </thead>
        <tbody>
          {companies.map((c) => (
            <tr
              key={c.id}
              className="border-b border-slate-100 dark:border-flit-border-dark/50 hover:bg-slate-50 dark:hover:bg-slate-800/40"
            >
              <td className="px-4 py-3 font-mono text-xs">{c.nit}</td>
              <td className="px-4 py-3 font-medium text-flit-heading dark:text-flit-heading-dark">
                {c.name}
              </td>
              <td className="px-4 py-3 text-flit-muted">{c.tenantSlug}</td>
              <td className="px-4 py-3">
                <span
                  className={`inline-flex rounded-full border px-2 py-0.5 text-xs font-medium ${STATUS_CLASSES[c.status] ?? STATUS_CLASSES.suspended}`}
                >
                  {c.status}
                </span>
              </td>
              <td className="px-4 py-3">
                <button
                  type="button"
                  onClick={() => onEdit(c)}
                  className="text-sm font-medium text-flit-primary hover:underline"
                >
                  Configurar
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
