import { useParams, Link } from "react-router-dom";
import { ResetPasswordForm } from "../components/ResetPasswordForm.js";

export function ResetPasswordPage() {
  const { token = "" } = useParams<{ token: string }>();

  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-flit-canvas px-4 dark:bg-flit-canvas-dark">
        <div className="w-full max-w-sm text-center">
          <div className="flit-card flex flex-col items-center gap-5 py-10">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-red-100 text-red-600 dark:bg-red-950/50 dark:text-red-300">
              <i className="pi pi-times-circle text-3xl" aria-hidden="true" />
            </div>
            <div>
              <h1 className="text-xl font-semibold text-flit-heading dark:text-flit-heading-dark">
                Enlace inválido
              </h1>
              <p className="mt-2 text-sm text-flit-muted dark:text-flit-muted-dark">
                El enlace de recuperación no es válido.
              </p>
            </div>
            <Link
              to="/login"
              className="text-sm font-medium text-flit-primary underline-offset-2 hover:underline"
            >
              Volver al inicio de sesión
            </Link>
          </div>
        </div>
      </div>
    );
  }

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
              Recuperación de contraseña
            </p>
          </div>
        </div>

        <div className="flit-card">
          <h2 className="mb-1 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark">
            Nueva contraseña
          </h2>
          <p className="mb-6 text-sm text-flit-muted dark:text-flit-muted-dark">
            Elige una contraseña segura para tu cuenta
          </p>
          <ResetPasswordForm token={token} />
        </div>
      </div>
    </div>
  );
}
