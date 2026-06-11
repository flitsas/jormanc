import { useState } from "react";
import { useExportDashboardPdf } from "../api/dashboard.api.js";
import type { DashboardExportParams } from "../api/dashboard.api.js";

interface ExportPdfButtonProps {
  params: DashboardExportParams | null;
  disabled?: boolean;
}

export function ExportPdfButton({ params, disabled = false }: ExportPdfButtonProps) {
  const exportMutation = useExportDashboardPdf();
  const [toast, setToast] = useState<string | null>(null);

  const isDisabled = disabled || !params || exportMutation.isPending;

  async function handleExport() {
    if (!params) return;
    setToast(null);
    try {
      await exportMutation.mutateAsync(params);
    } catch {
      setToast("Error al exportar. Intenta nuevamente.");
    }
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => void handleExport()}
        disabled={isDisabled}
        className="inline-flex items-center gap-2 rounded-lg border border-flit-border bg-white px-3 py-2 text-sm font-medium text-flit-heading transition-colors hover:bg-flit-canvas disabled:cursor-not-allowed disabled:opacity-50 dark:border-flit-border-dark dark:bg-slate-900 dark:text-flit-heading-dark"
        aria-label="Exportar PDF"
        aria-busy={exportMutation.isPending}
      >
        {exportMutation.isPending ? (
          <i className="pi pi-spin pi-spinner" aria-hidden="true" />
        ) : (
          <i className="pi pi-file-pdf" aria-hidden="true" />
        )}
        Exportar PDF
      </button>

      {toast ? (
        <div
          className="absolute right-0 top-full z-10 mt-2 min-w-[16rem] rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 shadow-flit dark:border-red-900 dark:bg-red-950/50 dark:text-red-200"
          role="alert"
        >
          {toast}
        </div>
      ) : null}
    </div>
  );
}
