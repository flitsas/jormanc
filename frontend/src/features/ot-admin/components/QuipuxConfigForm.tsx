import { useEffect, useState } from "react";
import { useUpdateQuipuxConfig } from "../api/ot-admin.api.js";
import {
  QuipuxConfigFormSchema,
  parseQuipuxConfig,
  type OtOrganism,
  type QuipuxConfigFormValues,
} from "../api/ot-admin.schemas.js";

interface QuipuxConfigFormProps {
  organism: OtOrganism;
  onSaved?: (organism: OtOrganism) => void;
}

export function QuipuxConfigForm({ organism, onSaved }: QuipuxConfigFormProps) {
  const updateConfig = useUpdateQuipuxConfig(organism.id);
  const parsed = parseQuipuxConfig(organism.quipuxConfig);

  const [values, setValues] = useState<QuipuxConfigFormValues>({
    endpoint: parsed.endpoint,
    webhookToken: "",
  });
  const [errors, setErrors] = useState<Partial<Record<keyof QuipuxConfigFormValues, string>>>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    const next = parseQuipuxConfig(organism.quipuxConfig);
    setValues({ endpoint: next.endpoint, webhookToken: "" });
  }, [organism.id, organism.quipuxConfig]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);
    setSuccess(null);

    const payload: QuipuxConfigFormValues = {
      endpoint: values.endpoint.trim(),
      webhookToken: values.webhookToken?.trim() || undefined,
    };

    const result = QuipuxConfigFormSchema.safeParse(payload);
    if (!result.success) {
      const fieldErrors: Partial<Record<keyof QuipuxConfigFormValues, string>> = {};
      for (const issue of result.error.issues) {
        const key = issue.path[0] as keyof QuipuxConfigFormValues;
        fieldErrors[key] = issue.message;
      }
      setErrors(fieldErrors);
      return;
    }

    setErrors({});
    try {
      const updated = await updateConfig.mutateAsync(result.data);
      setValues((v) => ({ ...v, webhookToken: "" }));
      setSuccess("Configuración Quipux guardada");
      onSaved?.(updated);
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Error al guardar configuración Quipux");
    }
  }

  return (
    <form
      onSubmit={(e) => void handleSubmit(e)}
      className="space-y-4"
      aria-label="Configuración Quipux"
    >
      <div>
        <h3 className="text-sm font-semibold text-flit-heading dark:text-flit-heading-dark">
          Integración Quipux
        </h3>
        <p className="mt-1 text-sm text-flit-muted">
          Configure el endpoint y el token del webhook. El token se almacena hasheado en el
          servidor.
        </p>
      </div>

      {apiError && (
        <p className="text-sm text-red-600" role="alert">
          {apiError}
        </p>
      )}
      {success && (
        <p className="text-sm text-emerald-700 dark:text-emerald-400" role="status">
          {success}
        </p>
      )}

      <div>
        <label htmlFor="quipux-endpoint" className="mb-1 block text-sm font-medium">
          Endpoint Quipux
        </label>
        <input
          id="quipux-endpoint"
          type="url"
          autoComplete="off"
          placeholder="https://quipux.example.com/api"
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
          value={values.endpoint}
          onChange={(e) => setValues((v) => ({ ...v, endpoint: e.target.value }))}
        />
        {errors.endpoint && <p className="mt-1 text-xs text-red-600">{errors.endpoint}</p>}
      </div>

      <div>
        <label htmlFor="quipux-webhook-token" className="mb-1 block text-sm font-medium">
          Token webhook
        </label>
        <input
          id="quipux-webhook-token"
          type="password"
          autoComplete="new-password"
          placeholder={parsed.hasWebhookToken ? "•••••••• (configurado)" : "Mínimo 8 caracteres"}
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
          value={values.webhookToken ?? ""}
          onChange={(e) => setValues((v) => ({ ...v, webhookToken: e.target.value }))}
        />
        {parsed.hasWebhookToken && (
          <p className="mt-1 text-xs text-flit-muted">
            Ya existe un token configurado. Déjelo vacío para conservarlo.
          </p>
        )}
        {errors.webhookToken && <p className="mt-1 text-xs text-red-600">{errors.webhookToken}</p>}
      </div>

      <button
        type="submit"
        disabled={updateConfig.isPending}
        className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white shadow-flit disabled:opacity-50"
      >
        Guardar configuración
      </button>
    </form>
  );
}
