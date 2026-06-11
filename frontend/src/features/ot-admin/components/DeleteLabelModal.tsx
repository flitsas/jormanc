import { useState } from "react";
import { FlitModal } from "../../../shared/components/ui/FlitModal.js";
import type { OtDocumentLabel } from "../api/ot-admin.schemas.js";

interface DeleteLabelModalProps {
  label: OtDocumentLabel;
  impactCount: number;
  isLoadingImpact: boolean;
  isDeleting: boolean;
  onConfirm: () => void;
  onClose: () => void;
}

export function DeleteLabelModal({
  label,
  impactCount,
  isLoadingImpact,
  isDeleting,
  onConfirm,
  onClose,
}: DeleteLabelModalProps) {
  const [confirmed, setConfirmed] = useState(false);

  const impactMessage =
    impactCount > 0
      ? `Esta etiqueta está en uso en ${impactCount} adjunto${impactCount === 1 ? "" : "s"}. ¿Confirmas la eliminación?`
      : "¿Confirmas la eliminación de esta etiqueta?";

  return (
    <FlitModal
      title="Eliminar etiqueta"
      subtitle={label.displayName}
      onClose={onClose}
      closeLabel="Cancelar eliminación"
      maxWidthClass="max-w-md"
    >
      <div className="space-y-4">
        {isLoadingImpact ? (
          <p className="text-sm text-flit-muted" aria-busy="true">
            Consultando impacto en adjuntos…
          </p>
        ) : (
          <p className="text-sm text-flit-heading dark:text-flit-heading-dark" role="status">
            {impactMessage}
          </p>
        )}

        {!isLoadingImpact && impactCount > 0 ? (
          <label className="flex items-start gap-2 text-sm">
            <input
              type="checkbox"
              checked={confirmed}
              onChange={(e) => setConfirmed(e.target.checked)}
              className="mt-0.5"
            />
            <span>Entiendo que se eliminará la etiqueta &quot;{label.slug}&quot; y afectará adjuntos existentes.</span>
          </label>
        ) : null}

        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium dark:border-flit-border-dark"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={
              isDeleting ||
              isLoadingImpact ||
              (impactCount > 0 && !confirmed)
            }
            className="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
          >
            {isDeleting ? "Eliminando…" : "Eliminar"}
          </button>
        </div>
      </div>
    </FlitModal>
  );
}
