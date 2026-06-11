import { useState } from "react";
import { useCreateCompany } from "../api/companies.api.js";
import { CreateCompanyFormSchema, type CreateCompanyFormValues } from "../api/companies.schemas.js";

interface CreateCompanyModalProps {
  onClose: () => void;
  onCreated: () => void;
}

export function CreateCompanyModal({ onClose, onCreated }: CreateCompanyModalProps) {
  const create = useCreateCompany();
  const [values, setValues] = useState<CreateCompanyFormValues>({
    nit: "",
    name: "",
    tenantSlug: "",
  });
  const [errors, setErrors] = useState<Partial<Record<keyof CreateCompanyFormValues, string>>>({});
  const [apiError, setApiError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);
    const parsed = CreateCompanyFormSchema.safeParse(values);
    if (!parsed.success) {
      const fieldErrors: Partial<Record<keyof CreateCompanyFormValues, string>> = {};
      for (const issue of parsed.error.issues) {
        const key = issue.path[0] as keyof CreateCompanyFormValues;
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
      setApiError(err instanceof Error ? err.message : "Error al crear compañía");
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
    >
      <form
        onSubmit={(e) => void handleSubmit(e)}
        className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl dark:bg-flit-surface-dark"
      >
        <h2 className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark mb-4">
          Nueva compañía
        </h2>

        {apiError && (
          <p className="mb-3 text-sm text-red-600" role="alert">
            {apiError}
          </p>
        )}

        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium mb-1" htmlFor="company-nit">
              NIT
            </label>
            <input
              id="company-nit"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
              value={values.nit}
              onChange={(e) => setValues((v) => ({ ...v, nit: e.target.value }))}
            />
            {errors.nit && <p className="text-xs text-red-600 mt-1">{errors.nit}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1" htmlFor="company-name">
              Nombre
            </label>
            <input
              id="company-name"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
              value={values.name}
              onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))}
            />
            {errors.name && <p className="text-xs text-red-600 mt-1">{errors.name}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1" htmlFor="company-slug">
              Slug tenant
            </label>
            <input
              id="company-slug"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
              value={values.tenantSlug}
              onChange={(e) => setValues((v) => ({ ...v, tenantSlug: e.target.value }))}
            />
            {errors.tenantSlug && <p className="text-xs text-red-600 mt-1">{errors.tenantSlug}</p>}
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
