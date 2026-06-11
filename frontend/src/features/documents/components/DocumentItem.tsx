import type { DocumentOrigin, DocumentStatus, ProcedureDocumentItem } from "../api/documents.schemas.js";

const STATUS_CONFIG: Record<
  DocumentStatus,
  { label: string; icon: string; className: string }
> = {
  pending: {
    label: "Pendiente",
    icon: "pi-clock",
    className: "bg-amber-100 text-amber-800 dark:bg-amber-950/40 dark:text-amber-200",
  },
  ready: {
    label: "Listo",
    icon: "pi-check-circle",
    className: "bg-emerald-100 text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-200",
  },
  failed: {
    label: "Fallido",
    icon: "pi-times-circle",
    className: "bg-red-100 text-red-800 dark:bg-red-950/40 dark:text-red-200",
  },
  expired: {
    label: "Vencido",
    icon: "pi-calendar-times",
    className: "bg-orange-100 text-orange-800 dark:bg-orange-950/40 dark:text-orange-200",
  },
};

const ORIGIN_LABEL: Record<DocumentOrigin, string> = {
  generated: "Generado",
  uploaded: "Cargado",
};

function formatDate(iso: string | null | undefined): string | null {
  if (!iso) return null;
  try {
    return new Intl.DateTimeFormat("es-CO", { dateStyle: "medium", timeStyle: "short" }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
}

interface DocumentItemProps {
  document: ProcedureDocumentItem;
}

export function DocumentItem({ document }: DocumentItemProps) {
  const status = STATUS_CONFIG[document.status];
  const generatedLabel = formatDate(document.generatedAt);
  const loadIcon = document.documentType.loadType === "generacion" ? "pi-file-pdf" : "pi-upload";

  return (
    <li
      className="flex flex-wrap items-center justify-between gap-3 px-3 py-3 text-sm"
      aria-label={`${document.documentType.name}, ${status.label}`}
    >
      <div className="flex min-w-0 flex-1 items-start gap-3">
        <span
          className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-flit-primary/10 text-flit-primary"
          aria-hidden="true"
        >
          <i className={`pi ${loadIcon}`} />
        </span>
        <div className="min-w-0">
          <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
            {document.documentType.name}
            {document.isRequired ? (
              <span className="ml-1 text-xs font-normal text-red-600 dark:text-red-400" aria-label="obligatorio">
                *
              </span>
            ) : null}
          </p>
          <p className="text-xs text-flit-muted dark:text-flit-muted-dark">
            {ORIGIN_LABEL[document.origin]}
            {document.templateVersion != null ? ` · Plantilla v${document.templateVersion}` : null}
            {generatedLabel ? ` · ${generatedLabel}` : null}
          </p>
        </div>
      </div>
      <span
        className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${status.className}`}
        role="status"
      >
        <i className={`pi ${status.icon}`} aria-hidden="true" />
        {status.label}
      </span>
    </li>
  );
}

export { STATUS_CONFIG, ORIGIN_LABEL };
