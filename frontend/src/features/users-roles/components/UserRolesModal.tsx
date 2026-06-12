import { useState, useId } from "react";
import { FlitModal } from "../../../shared/components/ui/FlitModal.js";
import { useUpdateUserRoles, useRoles } from "../api/users.api.js";
import type { User } from "../api/users.schemas.js";

interface Props {
  user: User;
  onClose: () => void;
}

export function UserRolesModal({ user, onClose }: Props) {
  const uid = useId();
  const { data: allRoles = [], isLoading: rolesLoading } = useRoles();
  const { mutateAsync: updateRoles, isPending } = useUpdateUserRoles(user.id);

  const [selectedRoleIds, setSelectedRoleIds] = useState<Set<string>>(
    new Set(user.roles.map((r) => r.id)),
  );
  const [apiError, setApiError] = useState<string | null>(null);

  function toggleRole(roleId: string) {
    setSelectedRoleIds((prev) => {
      const next = new Set(prev);
      if (next.has(roleId)) {
        next.delete(roleId);
      } else {
        next.add(roleId);
      }
      return next;
    });
    setApiError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setApiError(null);

    try {
      await updateRoles(Array.from(selectedRoleIds));
      onClose();
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Error al actualizar los roles");
    }
  }

  return (
    <FlitModal
      title="Gestionar roles"
      subtitle={`Usuario: ${user.fullName} (${user.email})`}
      onClose={onClose}
      maxWidthClass="max-w-md"
    >
      <form
        onSubmit={handleSubmit}
        aria-label="Formulario de roles de usuario"
        className="flex flex-col gap-5"
      >
        {apiError && (
          <div role="alert" aria-live="assertive" className="flit-alert flit-alert--block">
            <i className="pi pi-times-circle mr-2" aria-hidden="true" />
            {apiError}
          </div>
        )}

        {rolesLoading ? (
          <div
            className="flex items-center justify-center gap-2 py-8 text-sm text-flit-muted dark:text-flit-muted-dark"
            aria-label="Cargando roles disponibles"
            aria-busy="true"
          >
            <i className="pi pi-spin pi-spinner" aria-hidden="true" />
            Cargando roles…
          </div>
        ) : (
          <fieldset className="flex flex-col gap-2">
            <legend className="flit-label mb-2">Roles disponibles</legend>
            {allRoles.length === 0 ? (
              <p className="text-sm text-flit-muted dark:text-flit-muted-dark italic">
                No hay roles configurados
              </p>
            ) : (
              allRoles.map((role) => {
                const checkId = `${uid}-role-${role.id}`;
                const isChecked = selectedRoleIds.has(role.id);
                return (
                  <label
                    key={role.id}
                    htmlFor={checkId}
                    className={`flex cursor-pointer items-start gap-3 rounded-lg border p-3 transition-colors ${
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
                      className="mt-0.5 h-4 w-4 shrink-0 accent-flit-primary"
                    />
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-flit-heading dark:text-flit-heading-dark">
                        {role.name}
                        {role.isSystem && (
                          <span className="ml-1.5 text-xs font-normal text-flit-muted dark:text-flit-muted-dark">
                            (sistema)
                          </span>
                        )}
                      </p>
                      {role.description && (
                        <p className="text-xs text-flit-muted dark:text-flit-muted-dark">
                          {role.description}
                        </p>
                      )}
                    </div>
                  </label>
                );
              })
            )}
          </fieldset>
        )}

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
                Guardando…
              </>
            ) : (
              "Guardar roles"
            )}
          </button>
        </div>
      </form>
    </FlitModal>
  );
}
