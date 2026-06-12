import type { User } from "../api/users.schemas.js";

const STATUS_LABELS: Record<User["status"], string> = {
  active: "Activo",
  pending: "Pendiente",
  suspended: "Suspendido",
  deleted: "Eliminado",
};

const STATUS_CLASSES: Record<User["status"], string> = {
  active:
    "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-700/50",
  pending:
    "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-700/50",
  suspended:
    "bg-slate-100 text-slate-600 border-slate-200 dark:bg-slate-800 dark:text-slate-400 dark:border-slate-700",
  deleted:
    "bg-red-50 text-red-700 border-red-200 dark:bg-red-950/40 dark:text-red-300 dark:border-red-700/50",
};

function SkeletonRow() {
  return (
    <tr aria-hidden="true">
      {[1, 2, 3, 4, 5].map((i) => (
        <td key={i} className="px-4 py-3">
          <div className="h-4 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        </td>
      ))}
    </tr>
  );
}

interface UsersTableProps {
  users: User[];
  isLoading: boolean;
  error: Error | null;
  onRetry: () => void;
  onManageRoles: (user: User) => void;
}

export function UsersTable({ users, isLoading, error, onRetry, onManageRoles }: UsersTableProps) {
  if (isLoading) {
    return (
      <div className="flit-list-panel__table" aria-label="Cargando usuarios" aria-busy="true">
        <table className="w-full text-sm" role="table">
          <thead>
            <TableHead />
          </thead>
          <tbody>
            {Array.from({ length: 5 }).map((_, i) => (
              <SkeletonRow key={i} />
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flit-list-panel__error" role="alert">
        <div className="flit-list-panel__error-icon" aria-hidden="true">
          <i className="pi pi-exclamation-circle" />
        </div>
        <div className="flit-list-panel__error-copy">
          <p className="flit-list-panel__error-title">No se pudo cargar el listado</p>
          <p className="flit-list-panel__error-desc">{error.message}</p>
        </div>
        <div className="flit-list-panel__error-action">
          <button
            type="button"
            onClick={onRetry}
            className="inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-flit-heading shadow-flit transition-colors hover:bg-flit-canvas focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary dark:border-flit-border-dark dark:bg-flit-surface-dark dark:text-flit-heading-dark dark:hover:bg-slate-700"
          >
            <i className="pi pi-refresh text-xs" aria-hidden="true" />
            Reintentar
          </button>
        </div>
      </div>
    );
  }

  if (users.length === 0) {
    return (
      <div className="flit-list-panel__empty" role="status">
        <div className="flit-list-panel__empty-icon" aria-hidden="true">
          <i className="pi pi-users" />
        </div>
        <div className="flit-list-panel__empty-copy">
          <p className="flit-list-panel__empty-title">No hay usuarios en este tenant</p>
          <p className="flit-list-panel__empty-desc">
            Invita a miembros del equipo para que accedan a la plataforma
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="flit-list-panel__table overflow-x-auto">
      <table className="w-full text-sm" role="table" aria-label="Lista de usuarios">
        <thead>
          <TableHead />
        </thead>
        <tbody className="divide-y divide-flit-border dark:divide-flit-border-dark">
          {users.map((user) => (
            <tr
              key={user.id}
              className="transition-colors hover:bg-flit-canvas/50 dark:hover:bg-slate-700/30"
            >
              <td className="px-4 py-3">
                <div>
                  <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
                    {user.fullName}
                  </p>
                  <p className="text-xs text-flit-muted dark:text-flit-muted-dark">{user.email}</p>
                </div>
              </td>
              <td className="px-4 py-3">
                <span
                  className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${STATUS_CLASSES[user.status]}`}
                >
                  {STATUS_LABELS[user.status]}
                </span>
              </td>
              <td className="px-4 py-3 text-flit-muted dark:text-flit-muted-dark">
                {user.roles.length > 0 ? (
                  user.roles.map((r) => r.name).join(", ")
                ) : (
                  <span className="italic">Sin roles</span>
                )}
              </td>
              <td className="px-4 py-3 text-flit-muted dark:text-flit-muted-dark tabular-nums">
                {user.lastLoginAt ? (
                  new Date(user.lastLoginAt).toLocaleDateString("es-CO", {
                    day: "2-digit",
                    month: "2-digit",
                    year: "numeric",
                  })
                ) : (
                  <span className="italic">Nunca</span>
                )}
              </td>
              <td className="px-4 py-3 text-right">
                <button
                  type="button"
                  onClick={() => onManageRoles(user)}
                  aria-label={`Gestionar roles de ${user.fullName}`}
                  className="flit-row-action"
                >
                  <i className="pi pi-shield text-xs" aria-hidden="true" />
                  Roles
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function TableHead() {
  return (
    <tr className="border-b border-flit-border bg-flit-canvas/60 dark:border-flit-border-dark dark:bg-slate-800/60">
      <th
        scope="col"
        className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:text-flit-muted-dark"
      >
        Usuario
      </th>
      <th
        scope="col"
        className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:text-flit-muted-dark"
      >
        Estado
      </th>
      <th
        scope="col"
        className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:text-flit-muted-dark"
      >
        Roles
      </th>
      <th
        scope="col"
        className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-flit-muted dark:text-flit-muted-dark"
      >
        Último acceso
      </th>
      <th scope="col" className="px-4 py-3">
        <span className="sr-only">Acciones</span>
      </th>
    </tr>
  );
}
