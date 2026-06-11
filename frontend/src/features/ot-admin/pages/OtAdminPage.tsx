import { useState } from "react";
import { useDeleteOtOrganism, useOtOrganismsList } from "../api/ot-admin.api.js";
import type { OtOrganism } from "../api/ot-admin.schemas.js";
import { CreateOtModal } from "../components/CreateOtModal.js";
import { OtDetailPanel } from "../components/OtDetailPanel.js";
import { OtList } from "../components/OtList.js";

export function OtAdminPage() {
  const { data, isLoading, error, refetch } = useOtOrganismsList();
  const deleteOt = useDeleteOtOrganism();

  const [showCreate, setShowCreate] = useState(false);
  const [selectedOt, setSelectedOt] = useState<OtOrganism | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<OtOrganism | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const organisms = data ?? [];

  async function handleDelete() {
    if (!deleteTarget) return;
    setDeleteError(null);
    try {
      await deleteOt.mutateAsync(deleteTarget.id);
      if (selectedOt?.id === deleteTarget.id) setSelectedOt(null);
      setDeleteTarget(null);
      void refetch();
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : "No se pudo eliminar el OT");
    }
  }

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="flit-section-title">
            <i className="pi pi-map-marker text-flit-primary" aria-hidden="true" />
            Organismos de Tránsito
          </h1>
          {!isLoading && !error && (
            <p className="mt-0.5 text-sm text-flit-muted dark:text-flit-muted-dark">
              <strong className="font-semibold text-flit-heading dark:text-flit-heading-dark">
                {organisms.length}
              </strong>{" "}
              {organisms.length === 1 ? "OT configurado" : "OTs configurados"}
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={() => setShowCreate(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white shadow-flit"
        >
          <i className="pi pi-plus text-sm" aria-hidden="true" />
          Nuevo OT
        </button>
      </div>

      <div className="flit-list-panel">
        <OtList
          organisms={organisms}
          isLoading={isLoading}
          error={error}
          onRetry={() => void refetch()}
          onSelect={setSelectedOt}
          onDelete={setDeleteTarget}
        />
      </div>

      {showCreate && (
        <CreateOtModal onClose={() => setShowCreate(false)} onCreated={() => void refetch()} />
      )}

      {selectedOt && (
        <OtDetailPanel
          organism={selectedOt}
          onClose={() => setSelectedOt(null)}
          onUpdated={(updated) => setSelectedOt(updated)}
        />
      )}

      {deleteTarget && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          role="alertdialog"
          aria-labelledby="delete-ot-title"
        >
          <div className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl dark:bg-flit-surface-dark">
            <h2 id="delete-ot-title" className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark">
              Eliminar OT
            </h2>
            <p className="mt-2 text-sm text-flit-muted">
              ¿Confirma eliminar <strong>{deleteTarget.name}</strong>? Esta acción no se puede deshacer.
            </p>
            {deleteError && (
              <p className="mt-2 text-sm text-red-600" role="alert">
                {deleteError}
              </p>
            )}
            <div className="mt-6 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => {
                  setDeleteTarget(null);
                  setDeleteError(null);
                }}
                className="rounded-lg border px-4 py-2 text-sm"
              >
                Cancelar
              </button>
              <button
                type="button"
                onClick={() => void handleDelete()}
                disabled={deleteOt.isPending}
                className="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
              >
                Eliminar
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
