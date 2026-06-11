import { useId, useRef, useState } from "react";
import type { AttachmentListItem } from "../api/procedures.schemas.js";

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(iso: string): string {
  try {
    return new Intl.DateTimeFormat("es-CO", { dateStyle: "medium", timeStyle: "short" }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
}

function groupByLabel(attachments: AttachmentListItem[]): Map<string, AttachmentListItem[]> {
  const map = new Map<string, AttachmentListItem[]>();
  for (const att of attachments) {
    const list = map.get(att.labelSlug) ?? [];
    list.push(att);
    map.set(att.labelSlug, list);
  }
  return new Map([...map.entries()].sort(([a], [b]) => a.localeCompare(b)));
}

interface AttachmentsPanelProps {
  attachments: AttachmentListItem[];
  isLoading: boolean;
  error: Error | null;
  isUploading: boolean;
  uploadError: Error | null;
  onRetry: () => void;
  onUpload: (file: File, labelSlug: string) => void;
}

export function AttachmentsPanel({
  attachments,
  isLoading,
  error,
  isUploading,
  uploadError,
  onRetry,
  onUpload,
}: AttachmentsPanelProps) {
  const labelInputId = useId();
  const fileInputId = useId();
  const fileRef = useRef<HTMLInputElement>(null);
  const [labelSlug, setLabelSlug] = useState("licencia_transito");

  function handleUploadClick() {
    const file = fileRef.current?.files?.[0];
    if (!file || !labelSlug.trim()) return;
    onUpload(file, labelSlug.trim());
    if (fileRef.current) fileRef.current.value = "";
  }

  if (isLoading) {
    return (
      <section aria-label="Cargando adjuntos" aria-busy="true" className="space-y-3 rounded-lg border border-flit-border p-4 dark:border-flit-border-dark">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded bg-flit-border/30" />
        ))}
      </section>
    );
  }

  if (error) {
    return (
      <section role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-950/30">
        <p className="text-sm font-medium text-red-800 dark:text-red-200">No se pudieron cargar los adjuntos</p>
        <p className="text-sm text-red-700 dark:text-red-300">{error.message}</p>
        <button type="button" onClick={onRetry} className="mt-2 rounded-lg border px-3 py-1.5 text-sm">
          Reintentar
        </button>
      </section>
    );
  }

  const grouped = groupByLabel(attachments);

  return (
    <section aria-labelledby="attachments-panel-title" className="space-y-4 rounded-lg border border-flit-border p-4 dark:border-flit-border-dark">
      <h2 id="attachments-panel-title" className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark">
        <i className="pi pi-paperclip mr-2 text-flit-primary" aria-hidden="true" />
        Adjuntos del trámite
      </h2>

      <div className="flex flex-wrap items-end gap-3 rounded-md border border-dashed border-flit-border p-3 dark:border-flit-border-dark">
        <div className="flex flex-col gap-1">
          <label htmlFor={labelInputId} className="text-xs font-medium text-flit-muted">
            Etiqueta (label_slug)
          </label>
          <input
            id={labelInputId}
            type="text"
            value={labelSlug}
            onChange={(e) => setLabelSlug(e.target.value)}
            className="rounded-lg border border-flit-border px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-900"
            placeholder="licencia_transito"
            aria-required="true"
          />
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor={fileInputId} className="text-xs font-medium text-flit-muted">
            Archivo
          </label>
          <input
            id={fileInputId}
            ref={fileRef}
            type="file"
            className="text-sm"
            aria-required="true"
          />
        </div>
        <button
          type="button"
          onClick={handleUploadClick}
          disabled={isUploading || !labelSlug.trim()}
          className="flit-btn flit-btn-primary inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold disabled:opacity-50"
          aria-busy={isUploading}
        >
          <i className="pi pi-upload" aria-hidden="true" />
          {isUploading ? "Subiendo…" : "Subir adjunto"}
        </button>
      </div>

      {uploadError ? (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {uploadError.message}
        </p>
      ) : null}

      {attachments.length === 0 ? (
        <p className="text-sm text-flit-muted" role="status">
          No hay adjuntos cargados. Sube el primer archivo con una etiqueta.
        </p>
      ) : (
        <div className="space-y-4">
          {[...grouped.entries()].map(([label, items]) => (
            <section key={label} aria-labelledby={`label-${label}`} className="space-y-2">
              <h3
                id={`label-${label}`}
                className="text-sm font-semibold uppercase tracking-wide text-flit-muted"
              >
                {label}
              </h3>
              <ul className="divide-y divide-flit-border rounded-md border border-flit-border dark:divide-flit-border-dark dark:border-flit-border-dark">
                {items.map((att) => (
                  <li key={att.id} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2 text-sm">
                    <span className="font-medium text-flit-heading dark:text-flit-heading-dark">
                      {att.fileName}
                    </span>
                    <span className="text-flit-muted">
                      {formatFileSize(att.sizeBytes)} · {formatDate(att.uploadedAt)}
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
      )}
    </section>
  );
}
