import type { TopUser } from "../api/dashboard.schemas.js";
import { EmptyUserCard } from "./EmptyUserCard.js";

interface TopUsersCardProps {
  users: TopUser[];
  selectableUsers: TopUser[];
  selectedUserIds: string[];
  isLoading: boolean;
  error: Error | null;
  onSelectedUserIdsChange: (userIds: string[]) => void;
  onRetry: () => void;
}

function TopUsersSkeleton() {
  return (
    <div className="flex flex-col gap-3" aria-label="Cargando top radicadores" aria-busy="true">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex flex-col gap-2">
          <div className="h-4 w-40 animate-pulse rounded bg-flit-border/40 dark:bg-flit-border-dark/40" />
          <div className="h-2 w-full animate-pulse rounded-full bg-flit-border/40 dark:bg-flit-border-dark/40" />
        </div>
      ))}
    </div>
  );
}

function UserProgressRow({ user, isLeader }: { user: TopUser; isLeader: boolean }) {
  if (user.count === 0) {
    return <EmptyUserCard userName={user.fullName} />;
  }

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-center justify-between gap-2 text-sm">
        <span className="font-medium text-flit-heading dark:text-flit-heading-dark">
          {user.fullName}
        </span>
        <span className="tabular-nums text-flit-muted">
          {user.count} ({user.pctOfTotal}%)
        </span>
      </div>
      <div
        className="h-2 w-full overflow-hidden rounded-full bg-flit-border/30 dark:bg-flit-border-dark/30"
        role="progressbar"
        aria-valuenow={user.pctOfTotal}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`${user.fullName}: ${user.pctOfTotal}% del total`}
      >
        <div
          className="h-full rounded-full bg-flit-primary transition-all"
          style={{ width: `${Math.min(100, Math.max(0, user.pctOfTotal))}%` }}
          data-testid={`progress-${user.userId}`}
          data-is-leader={isLeader ? "true" : "false"}
        />
      </div>
    </div>
  );
}

export function TopUsersCard({
  users,
  selectableUsers,
  selectedUserIds,
  isLoading,
  error,
  onSelectedUserIdsChange,
  onRetry,
}: TopUsersCardProps) {
  function toggleUser(userId: string, checked: boolean) {
    if (checked) {
      onSelectedUserIdsChange([...selectedUserIds, userId]);
      return;
    }
    onSelectedUserIdsChange(selectedUserIds.filter((id) => id !== userId));
  }

  const leaderPct = users.find((u) => u.count > 0)?.pctOfTotal ?? 0;

  return (
    <section className="flit-card" aria-label="Top 5 radicadores">
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-base font-semibold text-flit-heading dark:text-flit-heading-dark">
            Top radicadores
          </h2>
          <p className="mt-0.5 text-sm text-flit-muted">
            Usuarios con más trámites radicados en el período
          </p>
        </div>

        {selectedUserIds.length > 0 ? (
          <span
            className="inline-flex items-center rounded-full border border-flit-primary/30 bg-flit-primary/10 px-3 py-1 text-xs font-medium text-flit-primary"
            role="status"
          >
            {selectedUserIds.length}{" "}
            {selectedUserIds.length === 1 ? "radicador seleccionado" : "radicadores seleccionados"}
          </span>
        ) : null}
      </div>

      {selectableUsers.length > 0 ? (
        <fieldset className="mb-4 rounded-lg border border-flit-border p-3 dark:border-flit-border-dark">
          <legend className="px-1 text-xs font-semibold uppercase tracking-wide text-flit-muted">
            Filtrar por radicador
          </legend>
          <div
            className="mt-2 flex flex-wrap gap-x-4 gap-y-2"
            role="group"
            aria-label="Selector multiselección de radicadores"
          >
            {selectableUsers.map((user) => {
              const inputId = `radicador-filter-${user.userId}`;
              return (
                <label
                  key={user.userId}
                  htmlFor={inputId}
                  className="inline-flex cursor-pointer items-center gap-2 text-sm text-flit-heading dark:text-flit-heading-dark"
                >
                  <input
                    id={inputId}
                    type="checkbox"
                    checked={selectedUserIds.includes(user.userId)}
                    onChange={(e) => toggleUser(user.userId, e.target.checked)}
                    className="h-4 w-4 rounded border-flit-border text-flit-primary focus:ring-flit-primary"
                  />
                  {user.fullName}
                </label>
              );
            })}
          </div>
        </fieldset>
      ) : null}

      {isLoading ? <TopUsersSkeleton /> : null}

      {!isLoading && error ? (
        <div className="flit-list-panel__error" role="alert">
          <div className="flit-list-panel__error-icon" aria-hidden="true">
            <i className="pi pi-exclamation-circle" />
          </div>
          <div className="flit-list-panel__error-copy">
            <p className="flit-list-panel__error-title">No se pudo cargar el ranking</p>
            <p className="flit-list-panel__error-desc">{error.message}</p>
          </div>
          <button
            type="button"
            onClick={onRetry}
            className="inline-flex items-center gap-1.5 rounded-lg border border-flit-border bg-white px-3 py-2 text-sm font-medium"
          >
            <i className="pi pi-refresh" aria-hidden="true" />
            Reintentar
          </button>
        </div>
      ) : null}

      {!isLoading && !error && users.length === 0 ? (
        <div className="flit-list-panel__empty py-8" role="status">
          <i className="pi pi-users text-3xl text-flit-muted mb-2" aria-hidden="true" />
          <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
            Sin radicadores en el período
          </p>
        </div>
      ) : null}

      {!isLoading && !error && users.length > 0 ? (
        <ol className="flex flex-col gap-4" aria-label="Ranking de radicadores">
          {users.slice(0, 5).map((user) => (
            <li key={user.userId}>
              <UserProgressRow
                user={user}
                isLeader={user.pctOfTotal === leaderPct && user.count > 0}
              />
            </li>
          ))}
        </ol>
      ) : null}
    </section>
  );
}
