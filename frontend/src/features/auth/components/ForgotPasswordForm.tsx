import { useState, useId } from "react";
import { Link } from "react-router-dom";
import { useForgotPassword } from "../api/auth.api.js";
import { ForgotPasswordFormSchema } from "../api/auth.schemas.js";

export function ForgotPasswordForm() {
  const uid = useId();
  const { mutateAsync, isPending } = useForgotPassword();

  const [email, setEmail] = useState("");
  const [emailError, setEmailError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);
    setEmailError(null);

    const parsed = ForgotPasswordFormSchema.safeParse({ email });
    if (!parsed.success) {
      setEmailError(parsed.error.issues[0]?.message ?? "Email inválido");
      return;
    }

    try {
      await mutateAsync(parsed.data);
      setSubmitted(true);
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Error al enviar el correo");
    }
  }

  if (submitted) {
    return (
      <div className="flex flex-col items-center gap-4 py-4 text-center" role="status" aria-live="polite">
        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-600 dark:bg-emerald-950/60 dark:text-emerald-300">
          <i className="pi pi-check text-2xl" aria-hidden="true" />
        </div>
        <div>
          <p className="font-semibold text-flit-heading dark:text-flit-heading-dark">
            Correo enviado
          </p>
          <p className="mt-1 text-sm text-flit-muted dark:text-flit-muted-dark">
            Revisa tu bandeja de entrada en <strong>{email}</strong> y sigue las instrucciones.
          </p>
        </div>
        <Link
          to="/login"
          className="mt-2 text-sm font-medium text-flit-primary underline-offset-2 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2"
        >
          Volver al inicio de sesión
        </Link>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      aria-label="Formulario de recuperación de contraseña"
      className="flex flex-col gap-5"
    >
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
          aria-invalid={!!emailError}
          aria-describedby={emailError ? `${uid}-email-err` : undefined}
          value={email}
          onChange={(e) => {
            setEmail(e.target.value);
            setEmailError(null);
          }}
          disabled={isPending}
          placeholder="usuario@empresa.com"
          className="h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-flit-heading placeholder-flit-muted/60 shadow-flit transition-colors focus:border-flit-primary focus:outline-none focus:ring-2 focus:ring-flit-primary/25 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-slate-800 dark:border-flit-border-dark dark:text-flit-heading-dark"
        />
        {emailError && (
          <p id={`${uid}-email-err`} role="alert" className="text-xs text-red-600 dark:text-red-400">
            {emailError}
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
            Enviando…
          </>
        ) : (
          "Enviar instrucciones"
        )}
      </button>

      <p className="text-center text-sm text-flit-muted dark:text-flit-muted-dark">
        <Link
          to="/login"
          className="font-medium text-flit-primary underline-offset-2 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2"
        >
          Volver al inicio de sesión
        </Link>
      </p>
    </form>
  );
}
