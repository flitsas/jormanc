import { useState, useId } from "react";
import { FlitModal } from "../../../shared/components/ui/FlitModal.js";
import { useCreateInvitation, useRoles } from "../api/users.api.js";
import { InviteUserFormSchema, type InviteUserFormValues } from "../api/users.schemas.js";

interface Props {
  onClose: () => void;
}

interface FieldErrors {
  email?: string;
  roles?: string;
}

export function InviteUserModal({ onClose }: Props) {
  const uid = useId();
  const { data: roles = [], isLoading: rolesLoading } = useRoles();
  const { mutateAsync: createInvitation, isPending } = useCreateInvitation();

  const [values, setValues] = useState<InviteUserFormValues>({ email: "", roles: [] });
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  function handleEmailChange(e: React.ChangeEvent<HTMLInputElement>) {
    setValues((prev) => ({ ...prev, email: e.target.value }));
    setFieldErrors((prev) => ({ ...prev, email: undefined }));
    setApiError(null);
  }

  function toggleRole(roleId: string) {
    setValues((prev) => {
      const has = prev.roles.includes(roleId);
      return {
        ...prev,
        roles: has ? prev.roles.filter((id) => id !== roleId) : [...prev.roles, roleId],
      };
    });
    setFieldErrors((prev) => ({ ...prev, roles: undefined }));
    setApiError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);

    const parsed = InviteUserFormSchema.safeParse(values);
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
      await createInvitation(parsed.data);
      setSuccess(values.email);
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Error al enviar la invitación");
    }
  }

  if (success) {
    return (
      <FlitModal title="Invitación enviada" onClose={onClose} maxWidthClass="max-w-md">
        <div className="flex flex-col items-center gap-4 py-6 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-600 dark:bg-emerald-950/60 dark:text-emerald-300">
            <i className="pi pi-check text-2xl" aria-hidden="true" />
          </div>
          <div>
            <p className="font-semibold text-flit-heading dark:text-flit-heading-dark">
              Invitación enviada
            </p>
            <p className="mt-1 text-sm text-flit-muted dark:text-flit-muted-dark">
              Se envió un correo de activación a <strong>{success}</strong>.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg bg-flit-primary px-5 py-2 text-sm font-semibold text-white shadow-flit transition-colors hover:bg-flit-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2"
          >
            Cerrar
          </button>
        </div>
      </FlitModal>
    );
  }

  return (
    <FlitModal
      title="Invitar usuario"
      subtitle="Se enviará un correo de activación"
      onClose={onClose}
      maxWidthClass="max-w-md"
    >
      <form
        onSubmit={handleSubmit}
        aria-label="Formulario de invitación de usuario"
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
            Correo electrónico{" "}
            <span className="text-flit-primary" aria-hidden="true">
              *
            </span>
          </label>
          <input
            id={`${uid}-email`}
            name="email"
            type="email"
            required
            aria-required="true"
            aria-invalid={!!fieldErrors.email}
            aria-describedby={fieldErrors.email ? `${uid}-email-err` : undefined}
            value={values.email}
            onChange={handleEmailChange}
            disabled={isPending}
            placeholder="nuevo@empresa.com"
            className="h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-flit-heading placeholder-flit-muted/60 shadow-flit transition-colors focus:border-flit-primary focus:outline-none focus:ring-2 focus:ring-flit-primary/25 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-slate-800 dark:border-flit-border-dark dark:text-flit-heading-dark"
          />
          {fieldErrors.email && (
            <p
              id={`${uid}-email-err`}
              role="alert"
              className="text-xs text-red-600 dark:text-red-400"
            >
              {fieldErrors.email}
            </p>
          )}
        </div>

        <fieldset>
          <legend className="flit-label mb-2">
            Roles{" "}
            <span className="text-flit-primary" aria-hidden="true">
              *
            </span>
          </legend>
          {rolesLoading ? (
            <div
              className="flex items-center gap-2 text-sm text-flit-muted dark:text-flit-muted-dark py-2"
              aria-label="Cargando roles"
              aria-busy="true"
            >
              <i className="pi pi-spin pi-spinner" aria-hidden="true" />
              Cargando roles…
            </div>
          ) : roles.length === 0 ? (
            <p className="text-sm text-flit-muted dark:text-flit-muted-dark italic">
              No hay roles disponibles
            </p>
          ) : (
            <div className="flex flex-col gap-2">
              {roles.map((role) => {
                const checkId = `${uid}-invite-role-${role.id}`;
                const isChecked = values.roles.includes(role.id);
                return (
                  <label
                    key={role.id}
                    htmlFor={checkId}
                    className={`flex cursor-pointer items-center gap-3 rounded-lg border p-3 text-sm transition-colors ${
                      isChecked
                        ? "border-flit-primary/50 bg-flit-primary/5 dark:border-flit-primary/40 dark:bg-flit-primary/10"
                        : "border-flit-border bg-white hover:bg-flit-canvas dark:border-flit-border-dark dark:bg-flit-surface-dark dark:hover:bg-slate-700/40"
                    }`}
                  >
                    <input
                      id={checkId}
                      type="checkbox"
                      checked={isChecked}
                      onChange={() => toggleRole(role.id)}
                      disabled={isPending}
                      className="h-4 w-4 shrink-0 accent-flit-primary"
                    />
                    <span className="font-medium text-flit-heading dark:text-flit-heading-dark">
                      {role.name}
                    </span>
                  </label>
                );
              })}
            </div>
          )}
          {fieldErrors.roles && (
            <p role="alert" className="mt-1 text-xs text-red-600 dark:text-red-400">
              {fieldErrors.roles}
            </p>
          )}
        </fieldset>

        <div className="flex items-center justify-end gap-3 border-t border-flit-border pt-4 dark:border-flit-border-dark">
          <button
            type="button"
            onClick={onClose}
            disabled={isPending}
            className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-flit-heading shadow-flit transition-colors hover:bg-flit-canvas focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 disabled:opacity-60 dark:border-flit-border-dark dark:bg-flit-surface-dark dark:text-flit-heading-dark dark:hover:bg-slate-700"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isPending || rolesLoading}
            aria-busy={isPending}
            className="flex items-center gap-2 rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white shadow-flit transition-colors hover:bg-flit-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isPending ? (
              <>
                <i className="pi pi-spin pi-spinner text-xs" aria-hidden="true" />
                Enviando…
              </>
            ) : (
              <>
                <i className="pi pi-envelope text-xs" aria-hidden="true" />
                Enviar invitación
              </>
            )}
          </button>
        </div>
      </form>
    </FlitModal>
  );
}
