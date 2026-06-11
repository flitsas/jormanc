import { useState, useId } from "react";
import { useNavigate } from "react-router-dom";
import { useResetPassword } from "../api/auth.api.js";
import {
  ResetPasswordFormSchema,
  type ResetPasswordFormValues,
} from "../api/auth.schemas.js";

interface Props {
  token: string;
}

interface FieldErrors {
  password?: string;
  password_confirm?: string;
}

export function ResetPasswordForm({ token }: Props) {
  const navigate = useNavigate();
  const uid = useId();
  const { mutateAsync, isPending } = useResetPassword(token);

  const [values, setValues] = useState<ResetPasswordFormValues>({
    password: "",
    password_confirm: "",
  });
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [apiError, setApiError] = useState<string | null>(null);

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target;
    setValues((prev) => ({ ...prev, [name]: value }));
    setFieldErrors((prev) => ({ ...prev, [name]: undefined }));
    setApiError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);

    const parsed = ResetPasswordFormSchema.safeParse(values);
    if (!parsed.success) {
      const errs: FieldErrors = {};
      for (const issue of parsed.error.issues) {
        const key = issue.path[0] as keyof FieldErrors;
        errs[key] = issue.message;
      }
      setFieldErrors(errs);
      return;
    }

    try {
      await mutateAsync(parsed.data);
      navigate("/login?reset=success", { replace: true });
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "El enlace expiró o ya fue utilizado");
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      aria-label="Formulario de nueva contraseña"
      className="flex flex-col gap-5"
    >
      {apiError && (
        <div role="alert" aria-live="assertive" className="flit-alert flit-alert--block">
          <i className="pi pi-times-circle mr-2" aria-hidden="true" />
          {apiError}
        </div>
      )}

      <div className="flit-field">
        <label htmlFor={`${uid}-pwd`} className="flit-label">
          Nueva contraseña <span className="text-flit-primary" aria-hidden="true">*</span>
        </label>
        <input
          id={`${uid}-pwd`}
          name="password"
          type="password"
          autoComplete="new-password"
          required
          aria-required="true"
          aria-invalid={!!fieldErrors.password}
          aria-describedby={`${uid}-pwd-hint${fieldErrors.password ? ` ${uid}-pwd-err` : ""}`}
          value={values.password}
          onChange={handleChange}
          disabled={isPending}
          placeholder="••••••••"
          className="h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-flit-heading placeholder-flit-muted/60 shadow-flit transition-colors focus:border-flit-primary focus:outline-none focus:ring-2 focus:ring-flit-primary/25 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-slate-800 dark:border-flit-border-dark dark:text-flit-heading-dark"
        />
        <p id={`${uid}-pwd-hint`} className="flit-hint">
          Mínimo 8 caracteres, una mayúscula y un número
        </p>
        {fieldErrors.password && (
          <p id={`${uid}-pwd-err`} role="alert" className="text-xs text-red-600 dark:text-red-400">
            {fieldErrors.password}
          </p>
        )}
      </div>

      <div className="flit-field">
        <label htmlFor={`${uid}-confirm`} className="flit-label">
          Confirmar contraseña <span className="text-flit-primary" aria-hidden="true">*</span>
        </label>
        <input
          id={`${uid}-confirm`}
          name="password_confirm"
          type="password"
          autoComplete="new-password"
          required
          aria-required="true"
          aria-invalid={!!fieldErrors.password_confirm}
          aria-describedby={fieldErrors.password_confirm ? `${uid}-confirm-err` : undefined}
          value={values.password_confirm}
          onChange={handleChange}
          disabled={isPending}
          placeholder="••••••••"
          className="h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-flit-heading placeholder-flit-muted/60 shadow-flit transition-colors focus:border-flit-primary focus:outline-none focus:ring-2 focus:ring-flit-primary/25 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-slate-800 dark:border-flit-border-dark dark:text-flit-heading-dark"
        />
        {fieldErrors.password_confirm && (
          <p id={`${uid}-confirm-err`} role="alert" className="text-xs text-red-600 dark:text-red-400">
            {fieldErrors.password_confirm}
          </p>
        )}
      </div>

      <button
        type="submit"
        disabled={isPending}
        aria-busy={isPending}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-flit-primary px-4 text-sm font-semibold text-white shadow-flit transition-colors hover:bg-flit-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isPending ? (
          <>
            <i className="pi pi-spin pi-spinner text-base" aria-hidden="true" />
            Guardando…
          </>
        ) : (
          "Establecer contraseña"
        )}
      </button>
    </form>
  );
}
