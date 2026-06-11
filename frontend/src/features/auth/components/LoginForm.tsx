import { useState, useId } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useLogin } from "../api/auth.api.js";
import { LoginFormSchema, type LoginFormValues } from "../api/auth.schemas.js";

interface FieldError {
  email?: string;
  password?: string;
  tenant_slug?: string;
}

export function LoginForm() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const uid = useId();
  const { mutateAsync: login, isPending } = useLogin();

  const [values, setValues] = useState<LoginFormValues>({
    email: "",
    password: "",
    tenant_slug: "",
  });
  const [fieldErrors, setFieldErrors] = useState<FieldError>({});
  const [apiError, setApiError] = useState<string | null>(null);

  const reason = searchParams.get("reason");
  const sessionRevokedMsg =
    reason === "session_revoked"
      ? "Tu sesión fue cerrada por un cambio de permisos. Ingresa nuevamente."
      : reason === "forbidden"
        ? "Acceso denegado. Ingresa nuevamente."
        : null;

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target;
    setValues((prev) => ({ ...prev, [name]: value }));
    setFieldErrors((prev) => ({ ...prev, [name]: undefined }));
    setApiError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);

    const parsed = LoginFormSchema.safeParse(values);
    if (!parsed.success) {
      const errs: FieldError = {};
      for (const issue of parsed.error.issues) {
        const key = issue.path[0] as keyof FieldError;
        errs[key] = issue.message;
      }
      setFieldErrors(errs);
      return;
    }

    try {
      await login(parsed.data);
      navigate("/dashboard", { replace: true });
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Credenciales inválidas");
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      aria-label="Formulario de inicio de sesión"
      className="flex flex-col gap-5"
    >
      {sessionRevokedMsg && (
        <div role="alert" className="flit-alert flit-alert--warn">
          <i className="pi pi-exclamation-triangle mr-2" aria-hidden="true" />
          {sessionRevokedMsg}
        </div>
      )}

      {apiError && (
        <div role="alert" aria-live="assertive" className="flit-alert flit-alert--block">
          <i className="pi pi-times-circle mr-2" aria-hidden="true" />
          {apiError}
        </div>
      )}

      <div className="flit-field">
        <label htmlFor={`${uid}-email`} className="flit-label">
          Correo electrónico <span className="text-flit-primary" aria-hidden="true">*</span>
        </label>
        <input
          id={`${uid}-email`}
          name="email"
          type="email"
          autoComplete="email"
          required
          aria-required="true"
          aria-invalid={!!fieldErrors.email}
          aria-describedby={fieldErrors.email ? `${uid}-email-err` : undefined}
          value={values.email}
          onChange={handleChange}
          disabled={isPending}
          placeholder="usuario@empresa.com"
          className="h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-flit-heading placeholder-flit-muted/60 shadow-flit transition-colors focus:border-flit-primary focus:outline-none focus:ring-2 focus:ring-flit-primary/25 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-slate-800 dark:border-flit-border-dark dark:text-flit-heading-dark"
        />
        {fieldErrors.email && (
          <p id={`${uid}-email-err`} role="alert" className="text-xs text-red-600 dark:text-red-400">
            {fieldErrors.email}
          </p>
        )}
      </div>

      <div className="flit-field">
        <label htmlFor={`${uid}-password`} className="flit-label">
          Contraseña <span className="text-flit-primary" aria-hidden="true">*</span>
        </label>
        <input
          id={`${uid}-password`}
          name="password"
          type="password"
          autoComplete="current-password"
          required
          aria-required="true"
          aria-invalid={!!fieldErrors.password}
          aria-describedby={fieldErrors.password ? `${uid}-password-err` : undefined}
          value={values.password}
          onChange={handleChange}
          disabled={isPending}
          placeholder="••••••••"
          className="h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-flit-heading placeholder-flit-muted/60 shadow-flit transition-colors focus:border-flit-primary focus:outline-none focus:ring-2 focus:ring-flit-primary/25 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-slate-800 dark:border-flit-border-dark dark:text-flit-heading-dark"
        />
        {fieldErrors.password && (
          <p id={`${uid}-password-err`} role="alert" className="text-xs text-red-600 dark:text-red-400">
            {fieldErrors.password}
          </p>
        )}
      </div>

      <div className="flit-field">
        <label htmlFor={`${uid}-tenant`} className="flit-label">
          Organización <span className="text-flit-primary" aria-hidden="true">*</span>
        </label>
        <input
          id={`${uid}-tenant`}
          name="tenant_slug"
          type="text"
          autoComplete="organization"
          required
          aria-required="true"
          aria-invalid={!!fieldErrors.tenant_slug}
          aria-describedby={fieldErrors.tenant_slug ? `${uid}-tenant-err` : undefined}
          value={values.tenant_slug}
          onChange={handleChange}
          disabled={isPending}
          placeholder="mi-empresa"
          className="h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-flit-heading placeholder-flit-muted/60 shadow-flit transition-colors focus:border-flit-primary focus:outline-none focus:ring-2 focus:ring-flit-primary/25 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-slate-800 dark:border-flit-border-dark dark:text-flit-heading-dark"
        />
        {fieldErrors.tenant_slug && (
          <p id={`${uid}-tenant-err`} role="alert" className="text-xs text-red-600 dark:text-red-400">
            {fieldErrors.tenant_slug}
          </p>
        )}
      </div>

      <button
        type="submit"
        disabled={isPending}
        aria-busy={isPending}
        className="mt-1 flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-flit-primary px-4 text-sm font-semibold text-white shadow-flit transition-colors hover:bg-flit-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isPending ? (
          <>
            <i className="pi pi-spin pi-spinner text-base" aria-hidden="true" />
            Ingresando…
          </>
        ) : (
          "Ingresar"
        )}
      </button>
    </form>
  );
}
