import { useState } from "react";
import { useCreateOtOrganism } from "../api/ot-admin.api.js";
import { CreateOtFormSchema, type CreateOtFormValues } from "../api/ot-admin.schemas.js";

interface CreateOtModalProps {
  onClose: () => void;
  onCreated: () => void;
}

export function CreateOtModal({ onClose, onCreated }: CreateOtModalProps) {
  const create = useCreateOtOrganism();
  const [values, setValues] = useState<CreateOtFormValues>({ slug: "", name: "" });
  const [errors, setErrors] = useState<Partial<Record<keyof CreateOtFormValues, string>>>({});
  const [apiError, setApiError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);
    const parsed = CreateOtFormSchema.safeParse(values);
    if (!parsed.success) {
      const fieldErrors: Partial<Record<keyof CreateOtFormValues, string>> = {};
      for (const issue of parsed.error.issues) {
        const key = issue.path[0] as keyof CreateOtFormValues;
        fieldErrors[key] = issue.message;
      }
      setErrors(fieldErrors);
      return;
    }
    setErrors({});
    try {
      await create.mutateAsync(parsed.data);
      onCreated();
      onClose();
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Error al crear OT");
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="create-ot-title"
    >
      <form
        onSubmit={(e) => void handleSubmit(e)}
        className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl dark:bg-flit-surface-dark"
      >
        <h2 id="create-ot-title" className="mb-4 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark">
          Nuevo organismo de tránsito
        </h2>

        {apiError && (
          <p className="mb-3 text-sm text-red-600" role="alert">
            {apiError}
          </p>
        )}

        <div className="space-y-3">
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ot-slug">
              Slug
            </label>
            <input
              id="ot-slug"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
              value={values.slug}
              onChange={(e) => setValues((v) => ({ ...v, slug: e.target.value.toLowerCase() }))}
              placeholder="ot-bogota"
            />
            {errors.slug && <p className="mt-1 text-xs text-red-600">{errors.slug}</p>}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ot-name">
              Nombre
            </label>
            <input
              id="ot-name"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
              value={values.name}
              onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))}
            />
            {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name}</p>}
          </div>
        </div>

        <div className="mt-6 flex justify-end gap-2">
          <button type="button" onClick={onClose} className="rounded-lg border px-4 py-2 text-sm">
            Cancelar
          </button>
          <button
            type="submit"
            disabled={create.isPending}
            className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
          >
            Crear
          </button>
        </div>
      </form>
    </div>
  );
}
