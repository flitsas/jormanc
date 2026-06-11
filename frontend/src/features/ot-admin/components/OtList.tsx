import type { OtOrganism } from "../api/ot-admin.schemas.js";

const MODE_CLASSES: Record<string, string> = {
  dashboard:
    "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800",
  qx: "bg-amber-50 text-amber-800 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800",
};

interface OtListProps {
  organisms: OtOrganism[];
  isLoading: boolean;
  error: Error | null;
  onRetry: () => void;
  onSelect: (ot: OtOrganism) => void;
  onDelete: (ot: OtOrganism) => void;
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

export function OtList({
  organisms,
  isLoading,
  error,
  onRetry,
  onSelect,
  onDelete,
}: OtListProps) {
  if (isLoading) {
    return (
      <div className="flit-list-panel__table" aria-label="Cargando organismos de tránsito" aria-busy="true">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:border-flit-border-dark">
              <th className="px-4 py-3">Slug</th>
              <th className="px-4 py-3">Nombre</th>
              <th className="px-4 py-3">Modo</th>
              <th className="px-4 py-3">Quipux</th>
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

  if (organisms.length === 0) {
    return (
      <div className="flit-list-panel__empty py-12 text-center" role="status">
        <i className="pi pi-map-marker mb-2 text-3xl text-flit-muted" aria-hidden="true" />
        <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
          No hay OTs configurados
        </p>
        <p className="mt-1 text-sm text-flit-muted">
          Crea el primer organismo de tránsito con el botón superior.
        </p>
      </div>
    );
  }

  return (
    <div className="flit-list-panel__table">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:border-flit-border-dark">
            <th className="px-4 py-3">Slug</th>
            <th className="px-4 py-3">Nombre</th>
            <th className="px-4 py-3">Modo</th>
            <th className="px-4 py-3">Quipux</th>
            <th className="px-4 py-3">Acciones</th>
          </tr>
        </thead>
        <tbody>
          {organisms.map((ot) => (
            <tr
              key={ot.id}
              className="border-b border-slate-100 hover:bg-slate-50 dark:border-flit-border-dark/50 dark:hover:bg-slate-800/40"
            >
              <td className="px-4 py-3 font-mono text-xs">{ot.slug}</td>
              <td className="px-4 py-3 font-medium text-flit-heading dark:text-flit-heading-dark">
                {ot.name}
              </td>
              <td className="px-4 py-3">
                <span
                  className={`inline-flex rounded-full border px-2 py-0.5 text-xs font-medium ${MODE_CLASSES[ot.mode] ?? MODE_CLASSES.dashboard}`}
                >
                  {ot.mode === "qx" ? "Modo QX" : "Dashboard"}
                </span>
              </td>
              <td className="px-4 py-3">
                {ot.quipuxEnabled ? (
                  <span className="text-xs font-medium text-emerald-700 dark:text-emerald-400">
                    Activo
                  </span>
                ) : (
                  <span className="text-xs text-flit-muted">—</span>
                )}
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => onSelect(ot)}
                    className="text-sm font-medium text-flit-primary hover:underline"
                  >
                    Configurar
                  </button>
                  <button
                    type="button"
                    onClick={() => onDelete(ot)}
                    className="text-sm font-medium text-red-600 hover:underline"
                    aria-label={`Eliminar ${ot.name}`}
                  >
                    Eliminar
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
