import { useEffect, useId, useRef, useState } from "react";
import type { ConsolidatedPackage } from "../api/documents.schemas.js";
import { downloadConsolidatedPdf, fetchConsolidatedPdfBlob } from "../api/documents.api.js";

function formatDate(iso: string): string {
  try {
    return new Intl.DateTimeFormat("es-CO", { dateStyle: "medium", timeStyle: "short" }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
}

interface ConsolidatedPackageCardProps {
  procedureId: string;
  latestPackage: ConsolidatedPackage | undefined;
  isConsolidating: boolean;
  consolidateError: Error | null;
  onConsolidate: () => void;
}

export function ConsolidatedPackageCard({
  procedureId,
  latestPackage,
  isConsolidating,
  consolidateError,
  onConsolidate,
}: ConsolidatedPackageCardProps) {
  const viewerTitleId = useId();
  const [isDownloading, setIsDownloading] = useState(false);
  const [downloadError, setDownloadError] = useState<string | null>(null);
  const [viewerOpen, setViewerOpen] = useState(false);
  const [viewerUrl, setViewerUrl] = useState<string | null>(null);
  const [viewerLoading, setViewerLoading] = useState(false);
  const [viewerError, setViewerError] = useState<string | null>(null);
  const viewerUrlRef = useRef<string | null>(null);

  useEffect(() => {
    return () => {
      if (viewerUrlRef.current) {
        URL.revokeObjectURL(viewerUrlRef.current);
      }
    };
  }, []);

  async function handleDownload() {
    setIsDownloading(true);
    setDownloadError(null);
    try {
      const { blob, filename } = await downloadConsolidatedPdf(procedureId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = filename;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setDownloadError(err instanceof Error ? err.message : "No se pudo descargar el PDF.");
    } finally {
      setIsDownloading(false);
    }
  }

  async function handleOpenViewer() {
    setViewerOpen(true);
    setViewerLoading(true);
    setViewerError(null);
    if (viewerUrlRef.current) {
      URL.revokeObjectURL(viewerUrlRef.current);
      viewerUrlRef.current = null;
      setViewerUrl(null);
    }
    try {
      const blob = await fetchConsolidatedPdfBlob(procedureId);
      const url = URL.createObjectURL(blob);
      viewerUrlRef.current = url;
      setViewerUrl(url);
    } catch (err) {
      setViewerError(err instanceof Error ? err.message : "No se pudo cargar el visor.");
    } finally {
      setViewerLoading(false);
    }
  }

  function handleCloseViewer() {
    setViewerOpen(false);
    if (viewerUrlRef.current) {
      URL.revokeObjectURL(viewerUrlRef.current);
      viewerUrlRef.current = null;
    }
    setViewerUrl(null);
    setViewerError(null);
  }

  if (!latestPackage) {
    return (
      <section
        aria-labelledby="consolidated-empty-title"
        className="rounded-lg border border-dashed border-flit-border p-4 dark:border-flit-border-dark"
      >
        <h3
          id="consolidated-empty-title"
          className="text-sm font-semibold text-flit-heading dark:text-flit-heading-dark"
        >
          Paquete consolidado
        </h3>
        <p className="mt-1 text-sm text-flit-muted dark:text-flit-muted-dark" role="status">
          Consolidación pendiente. Los documentos obligatorios deben estar listos.
        </p>
        <button
          type="button"
          onClick={onConsolidate}
          disabled={isConsolidating}
          className="flit-btn flit-btn-outline mt-3 inline-flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm disabled:opacity-50"
          aria-busy={isConsolidating}
        >
          <i className="pi pi-refresh" aria-hidden="true" />
          {isConsolidating ? "Consolidando…" : "Forzar consolidación"}
        </button>
        {consolidateError ? (
          <p role="alert" className="mt-2 text-sm text-red-600 dark:text-red-400">
            {consolidateError.message}
          </p>
        ) : null}
      </section>
    );
  }

  return (
    <>
      <section
        aria-labelledby="consolidated-package-title"
        className="rounded-lg border border-flit-border bg-flit-surface/50 p-4 dark:border-flit-border-dark dark:bg-slate-900/30"
      >
        <h3
          id="consolidated-package-title"
          className="text-sm font-semibold text-flit-heading dark:text-flit-heading-dark"
        >
          <i className="pi pi-file-pdf mr-2 text-flit-primary" aria-hidden="true" />
          Paquete consolidado
        </h3>
        <dl className="mt-2 grid gap-1 text-sm text-flit-muted dark:text-flit-muted-dark sm:grid-cols-2">
          <div>
            <dt className="inline font-medium text-flit-heading dark:text-flit-heading-dark">Versión: </dt>
            <dd className="inline">{latestPackage.version}</dd>
          </div>
          <div>
            <dt className="inline font-medium text-flit-heading dark:text-flit-heading-dark">Documentos: </dt>
            <dd className="inline">{latestPackage.docCount}</dd>
          </div>
          <div className="sm:col-span-2">
            <dt className="inline font-medium text-flit-heading dark:text-flit-heading-dark">Generado: </dt>
            <dd className="inline">{formatDate(latestPackage.createdAt)}</dd>
          </div>
          <div className="sm:col-span-2">
            <dt className="sr-only">Nombre de archivo</dt>
            <dd className="truncate font-mono text-xs">{latestPackage.downloadFilename}</dd>
          </div>
        </dl>
        <div className="mt-3 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => void handleDownload()}
            disabled={isDownloading}
            className="flit-btn flit-btn-primary inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold disabled:opacity-50"
            aria-busy={isDownloading}
          >
            <i className="pi pi-download" aria-hidden="true" />
            {isDownloading ? "Descargando…" : "Descargar PDF"}
          </button>
          <button
            type="button"
            onClick={() => void handleOpenViewer()}
            className="flit-btn flit-btn-outline inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold"
          >
            <i className="pi pi-eye" aria-hidden="true" />
            Ver PDF
          </button>
          <button
            type="button"
            onClick={onConsolidate}
            disabled={isConsolidating}
            className="flit-btn flit-btn-outline inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm disabled:opacity-50"
            aria-busy={isConsolidating}
          >
            <i className="pi pi-refresh" aria-hidden="true" />
            {isConsolidating ? "Reconsolidando…" : "Reconsolidar"}
          </button>
        </div>
        {downloadError ? (
          <p role="alert" className="mt-2 text-sm text-red-600 dark:text-red-400">
            {downloadError}
          </p>
        ) : null}
        {consolidateError ? (
          <p role="alert" className="mt-2 text-sm text-red-600 dark:text-red-400">
            {consolidateError.message}
          </p>
        ) : null}
      </section>

      {viewerOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby={viewerTitleId}
          onClick={handleCloseViewer}
          onKeyDown={(e) => {
            if (e.key === "Escape") handleCloseViewer();
          }}
        >
          <div
            className="flex max-h-[90vh] w-full max-w-4xl flex-col overflow-hidden rounded-lg bg-white shadow-xl dark:bg-slate-900"
            onClick={(e) => e.stopPropagation()}
          >
            <header className="flex items-center justify-between border-b border-flit-border px-4 py-3 dark:border-flit-border-dark">
              <h2 id={viewerTitleId} className="text-base font-semibold text-flit-heading dark:text-flit-heading-dark">
                Visor — {latestPackage.downloadFilename}
              </h2>
              <button
                type="button"
                onClick={handleCloseViewer}
                className="rounded-lg p-2 text-flit-muted hover:bg-flit-border/30 focus-visible:outline focus-visible:outline-2 focus-visible:outline-flit-primary"
                aria-label="Cerrar visor de PDF"
              >
                <i className="pi pi-times" aria-hidden="true" />
              </button>
            </header>
            <div className="min-h-[60vh] flex-1 bg-slate-100 dark:bg-slate-800">
              {viewerLoading ? (
                <p className="p-6 text-sm text-flit-muted" aria-busy="true">
                  Cargando PDF…
                </p>
              ) : null}
              {viewerError ? (
                <p role="alert" className="p-6 text-sm text-red-600 dark:text-red-400">
                  {viewerError}
                </p>
              ) : null}
              {viewerUrl && !viewerError ? (
                <iframe
                  src={viewerUrl}
                  title={`PDF consolidado ${latestPackage.downloadFilename}`}
                  className="h-[70vh] w-full border-0"
                />
              ) : null}
            </div>
          </div>
        </div>
      ) : null}
    </>
  );
}
