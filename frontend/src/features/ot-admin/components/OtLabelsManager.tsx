import { useState } from "react";
import {
  useCreateOtLabel,
  useDeleteOtLabel,
  useOtLabelImpact,
  useOtLabels,
  useUpdateOtLabel,
} from "../api/ot-admin.api.js";
import type { OtDocumentLabel } from "../api/ot-admin.schemas.js";
import { DeleteLabelModal } from "./DeleteLabelModal.js";

interface OtLabelsManagerProps {
  otId: string;
}

export function OtLabelsManager({ otId }: OtLabelsManagerProps) {
  const { data: labels, isLoading, error, refetch } = useOtLabels(otId);
  const createLabel = useCreateOtLabel(otId);
  const updateLabel = useUpdateOtLabel(otId);
  const deleteLabel = useDeleteOtLabel(otId);

  const [slug, setSlug] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editDisplayName, setEditDisplayName] = useState("");
  const [labelToDelete, setLabelToDelete] = useState<OtDocumentLabel | null>(null);

  const { data: impact, isLoading: isLoadingImpact } = useOtLabelImpact(
    otId,
    labelToDelete?.id ?? null,
  );

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    try {
      await createLabel.mutateAsync({ slug: slug.trim(), displayName: displayName.trim() });
      setSlug("");
      setDisplayName("");
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Error al crear etiqueta");
    }
  }

  async function handleSaveEdit(labelId: string) {
    try {
      await updateLabel.mutateAsync({
        labelId,
        data: { displayName: editDisplayName.trim() },
      });
      setEditingId(null);
    } catch {
      // keep editing open on error
    }
  }

  async function handleConfirmDelete() {
    if (!labelToDelete) return;
    try {
      await deleteLabel.mutateAsync({ labelId: labelToDelete.id, confirm: true });
      setLabelToDelete(null);
    } catch {
      // modal stays open on error
    }
  }

  if (isLoading) {
    return (
      <p
        className="text-sm text-flit-muted"
        aria-busy="true"
        aria-label="Cargando etiquetas personalizadas"
      >
        Cargando etiquetas…
      </p>
    );
  }

  if (error) {
    return (
      <div role="alert" className="text-sm text-red-600">
        {error.message}
        <button type="button" onClick={() => void refetch()} className="ml-2 underline">
          Reintentar
        </button>
      </div>
    );
  }

  const items = labels ?? [];

  return (
    <div className="space-y-4" role="region" aria-label="Gestor de etiquetas personalizadas">
      <form onSubmit={(e) => void handleCreate(e)} className="grid gap-3 sm:grid-cols-3">
        <div>
          <label htmlFor="ot-label-slug" className="mb-1 block text-sm font-medium">
            Slug
          </label>
          <input
            id="ot-label-slug"
            value={slug}
            onChange={(e) => setSlug(e.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
            placeholder="paz_y_salvo"
            required
          />
        </div>
        <div>
          <label htmlFor="ot-label-display-name" className="mb-1 block text-sm font-medium">
            Nombre visible
          </label>
          <input
            id="ot-label-display-name"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
            placeholder="Paz y Salvo Municipal"
            required
          />
        </div>
        <div className="flex items-end">
          <button
            type="submit"
            disabled={createLabel.isPending}
            className="w-full rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
          >
            Crear etiqueta
          </button>
        </div>
      </form>

      {formError ? (
        <p className="text-sm text-red-600" role="alert">
          {formError}
        </p>
      ) : null}

      {items.length === 0 ? (
        <p className="text-sm text-flit-muted" role="status">
          Sin etiquetas personalizadas
        </p>
      ) : (
        <ul className="divide-y divide-slate-100 dark:divide-flit-border-dark/50">
          {items.map((label) => (
            <li key={label.id} className="flex flex-wrap items-center justify-between gap-2 py-3">
              {editingId === label.id ? (
                <div className="flex flex-1 flex-wrap items-center gap-2">
                  <input
                    value={editDisplayName}
                    onChange={(e) => setEditDisplayName(e.target.value)}
                    aria-label={`Editar nombre de ${label.slug}`}
                    className="flex-1 rounded-lg border border-slate-300 px-3 py-1.5 text-sm dark:border-flit-border-dark dark:bg-slate-800"
                  />
                  <button
                    type="button"
                    onClick={() => void handleSaveEdit(label.id)}
                    disabled={updateLabel.isPending}
                    className="rounded-lg bg-flit-primary px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
                  >
                    Guardar
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditingId(null)}
                    className="text-sm text-flit-muted hover:underline"
                  >
                    Cancelar
                  </button>
                </div>
              ) : (
                <>
                  <div>
                    <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
                      {label.displayName}
                    </p>
                    <p className="font-mono text-xs text-flit-muted">{label.slug}</p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => {
                        setEditingId(label.id);
                        setEditDisplayName(label.displayName);
                      }}
                      className="text-sm text-flit-primary hover:underline"
                      aria-label={`Editar etiqueta ${label.displayName}`}
                    >
                      Editar
                    </button>
                    <button
                      type="button"
                      onClick={() => setLabelToDelete(label)}
                      className="text-sm text-red-600 hover:underline"
                      aria-label={`Eliminar etiqueta ${label.displayName}`}
                    >
                      Eliminar
                    </button>
                  </div>
                </>
              )}
            </li>
          ))}
        </ul>
      )}

      {labelToDelete ? (
        <DeleteLabelModal
          label={labelToDelete}
          impactCount={impact?.impactCount ?? 0}
          isLoadingImpact={isLoadingImpact}
          isDeleting={deleteLabel.isPending}
          onConfirm={() => void handleConfirmDelete()}
          onClose={() => setLabelToDelete(null)}
        />
      ) : null}
    </div>
  );
}
