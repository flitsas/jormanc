import { useState } from "react";
import { useUpdateOtMode } from "../api/ot-admin.api.js";
import type { OtMode, OtOrganism } from "../api/ot-admin.schemas.js";

interface OtModeSwitchProps {
  organism: OtOrganism;
  onModeChanged?: (organism: OtOrganism) => void;
}

export function OtModeSwitch({ organism, onModeChanged }: OtModeSwitchProps) {
  const updateMode = useUpdateOtMode(organism.id);
  const [confirmQx, setConfirmQx] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isQx = organism.mode === "qx";

  async function applyMode(mode: OtMode) {
    setError(null);
    try {
      const updated = await updateMode.mutateAsync(mode);
      onModeChanged?.(updated);
      setConfirmQx(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo cambiar el modo");
    }
  }

  function handleToggle() {
    if (isQx) {
      void applyMode("dashboard");
      return;
    }
    setConfirmQx(true);
  }

  return (
    <section aria-labelledby="ot-mode-switch-title" className="rounded-lg border border-slate-200 p-4 dark:border-flit-border-dark">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 id="ot-mode-switch-title" className="text-sm font-semibold text-flit-heading dark:text-flit-heading-dark">
            Modo de operación
          </h3>
          <p className="mt-1 text-sm text-flit-muted">
            {isQx
              ? "Modo QX: la cola de trámites es solo lectura; los estados se actualizan vía Quipux."
              : "Modo Dashboard: gestión nativa FLIT con aprobación y rechazo en la cola."}
          </p>
        </div>

        <div className="flex items-center gap-3">
          <span
            className={`inline-flex rounded-full border px-2.5 py-0.5 text-xs font-semibold ${
              isQx
                ? "border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-800 dark:bg-amber-950/40 dark:text-amber-300"
                : "border-blue-200 bg-blue-50 text-blue-700 dark:border-blue-800 dark:bg-blue-950/40 dark:text-blue-300"
            }`}
          >
            {isQx ? "Modo QX" : "Modo Dashboard"}
          </span>

          <button
            type="button"
            role="switch"
            aria-checked={isQx}
            aria-label={isQx ? "Cambiar a Modo Dashboard" : "Cambiar a Modo QX"}
            disabled={updateMode.isPending}
            onClick={handleToggle}
            className={`relative inline-flex h-7 w-12 shrink-0 rounded-full border-2 transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-flit-primary ${
              isQx
                ? "border-amber-500 bg-amber-500"
                : "border-slate-300 bg-slate-200 dark:border-slate-600 dark:bg-slate-700"
            }`}
          >
            <span
              className={`pointer-events-none inline-block h-6 w-6 transform rounded-full bg-white shadow transition-transform ${
                isQx ? "translate-x-5" : "translate-x-0"
              }`}
            />
          </button>
        </div>
      </div>

      {error && (
        <p className="mt-3 text-sm text-red-600" role="alert">
          {error}
        </p>
      )}

      {confirmQx && (
        <div
          className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950/30"
          role="alertdialog"
          aria-labelledby="confirm-qx-title"
        >
          <p id="confirm-qx-title" className="text-sm font-medium text-amber-900 dark:text-amber-200">
            ¿Activar Modo QX?
          </p>
          <p className="mt-1 text-sm text-amber-800 dark:text-amber-300">
            Los botones de aprobar y rechazar se ocultarán en la cola de trámites. Configure Quipux
            antes de continuar.
          </p>
          <div className="mt-3 flex gap-2">
            <button
              type="button"
              onClick={() => void applyMode("qx")}
              disabled={updateMode.isPending}
              className="rounded-lg bg-amber-600 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
            >
              Confirmar Modo QX
            </button>
            <button
              type="button"
              onClick={() => setConfirmQx(false)}
              className="rounded-lg border px-3 py-1.5 text-sm"
            >
              Cancelar
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
