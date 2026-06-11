import { ForgotPasswordForm } from "../components/ForgotPasswordForm.js";

export function ForgotPasswordPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-flit-canvas px-4 dark:bg-flit-canvas-dark">
      <div className="w-full max-w-md">
        <div className="mb-8 flex flex-col items-center gap-3">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-flit-accent to-flit-primary shadow-flit-md">
            <svg
              width="28"
              height="28"
              viewBox="0 0 24 24"
              fill="none"
              stroke="white"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M5 17H3a2 2 0 01-2-2V5a2 2 0 012-2h11l5 5v2" />
              <path d="M14 17h6l-3-3 3-3h-6" />
            </svg>
          </div>
          <div className="text-center">
            <h1 className="text-2xl font-bold text-flit-heading dark:text-flit-heading-dark">
              FLIT
            </h1>
            <p className="text-sm text-flit-muted dark:text-flit-muted-dark">
              Recuperar contraseña
            </p>
          </div>
        </div>

        <div className="flit-card">
          <h2 className="mb-1 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark">
            ¿Olvidaste tu contraseña?
          </h2>
          <p className="mb-6 text-sm text-flit-muted dark:text-flit-muted-dark">
            Ingresa tu correo y te enviaremos un enlace para restablecer tu contraseña
          </p>
          <ForgotPasswordForm />
        </div>
      </div>
    </div>
  );
}
