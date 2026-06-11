import type { DocumentTemplate } from "../api/documents.schemas.js";

const STATUS_BADGE: Record<DocumentTemplate["status"], { label: string; className: string }> = {
  active: {
    label: "Activa",
    className:
      "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 font-semibold",
  },
  deprecated: {
    label: "Deprecada",
    className: "bg-slate-100 text-slate-600 border-slate-200 dark:bg-slate-800 dark:text-slate-400",
  },
};

function formatDate(iso: string): string {
  return new Intl.DateTimeFormat("es-CO", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(iso));
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

interface TemplateVersionsListProps {
  templates: DocumentTemplate[];
  isLoading: boolean;
  error: Error | null;
  onRetry: () => void;
  onPreviewPdf?: (template: DocumentTemplate) => void;
  previewingVersionId?: string | null;
}

export function TemplateVersionsList({
  templates,
  isLoading,
  error,
  onRetry,
  onPreviewPdf,
  previewingVersionId,
}: TemplateVersionsListProps) {
  if (isLoading) {
    return (
      <div
        className="flit-list-panel__table"
        aria-label="Cargando versiones de plantilla"
        aria-busy="true"
      >
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 dark:border-flit-border-dark text-left text-xs font-semibold uppercase tracking-wide text-flit-muted">
              <th className="px-4 py-3">Versión</th>
              <th className="px-4 py-3">Estado</th>
              <th className="px-4 py-3">Fecha</th>
              <th className="px-4 py-3">Notas</th>
              <th className="px-4 py-3">Marcadores</th>
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 3 }).map((_, i) => (
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
          <p className="flit-list-panel__error-title">No se pudo cargar el historial</p>
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

  if (templates.length === 0) {
    return (
      <div className="flit-list-panel__empty py-12 text-center" role="status">
        <i className="pi pi-file text-3xl text-flit-muted mb-2" aria-hidden="true" />
        <p className="text-flit-heading dark:text-flit-heading-dark font-medium">
          Sin plantillas registradas
        </p>
        <p className="text-sm text-flit-muted mt-1">
          Suba la primera plantilla HTML para este tipo de documento.
        </p>
      </div>
    );
  }

  return (
    <div className="flit-list-panel__table">
      <table className="w-full text-sm" aria-label="Historial de versiones de plantilla">
        <thead>
          <tr className="border-b border-slate-200 dark:border-flit-border-dark text-left text-xs font-semibold uppercase tracking-wide text-flit-muted">
            <th className="px-4 py-3">Versión</th>
            <th className="px-4 py-3">Estado</th>
            <th className="px-4 py-3">Fecha</th>
            <th className="px-4 py-3">Notas</th>
            <th className="px-4 py-3">Marcadores</th>
            {onPreviewPdf ? <th className="px-4 py-3">Acciones</th> : null}
          </tr>
        </thead>
        <tbody>
          {templates.map((template) => {
            const badge = STATUS_BADGE[template.status];
            return (
              <tr
                key={template.templateId}
                className="border-b border-slate-100 dark:border-flit-border-dark/60"
              >
                <td className="px-4 py-3 font-medium">v{template.version}</td>
                <td className="px-4 py-3">
                  <span
                    className={`inline-flex rounded-full border px-2.5 py-0.5 text-xs ${badge.className}`}
                  >
                    {badge.label}
                  </span>
                </td>
                <td className="px-4 py-3 text-flit-muted">{formatDate(template.createdAt)}</td>
                <td className="px-4 py-3 text-flit-muted max-w-xs truncate">
                  {template.notes?.trim() || "—"}
                </td>
                <td className="px-4 py-3 text-flit-muted">{template.markersDetected.length}</td>
                {onPreviewPdf ? (
                  <td className="px-4 py-3">
                    <button
                      type="button"
                      className="flit-btn flit-btn-outline text-xs"
                      aria-label={`Vista previa PDF versión ${template.version}`}
                      aria-busy={previewingVersionId === template.templateId}
                      disabled={previewingVersionId === template.templateId}
                      onClick={() => onPreviewPdf(template)}
                    >
                      <i className="pi pi-eye" aria-hidden="true" />
                      PDF
                    </button>
                  </td>
                ) : null}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
