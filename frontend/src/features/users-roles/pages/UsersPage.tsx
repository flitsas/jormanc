import { useState } from "react";
import { useUsers } from "../api/users.api.js";
import { UsersTable } from "../components/UsersTable.js";
import { UserRolesModal } from "../components/UserRolesModal.js";
import { InviteUserModal } from "../components/InviteUserModal.js";
import type { User } from "../api/users.schemas.js";

export function UsersPage() {
  const [page] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [searchTimer, setSearchTimer] = useState<ReturnType<typeof setTimeout> | null>(null);

  const [rolesModalUser, setRolesModalUser] = useState<User | null>(null);
  const [showInviteModal, setShowInviteModal] = useState(false);

  const { data, isLoading, error, refetch } = useUsers(page, 20, debouncedSearch || undefined);

  function handleSearchChange(e: React.ChangeEvent<HTMLInputElement>) {
    const value = e.target.value;
    setSearch(value);
    if (searchTimer) clearTimeout(searchTimer);
    const timer = setTimeout(() => setDebouncedSearch(value), 350);
    setSearchTimer(timer);
  }

  const users = data?.items ?? [];
  const total = data?.total ?? 0;

  return (
    <div className="p-4 sm:p-6 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="flit-section-title">
            <i className="pi pi-users text-flit-primary" aria-hidden="true" />
            Usuarios
          </h1>
          {!isLoading && !error && (
            <p className="mt-0.5 text-sm text-flit-muted dark:text-flit-muted-dark">
              <strong className="font-semibold text-flit-heading dark:text-flit-heading-dark">
                {total}
              </strong>{" "}
              {total === 1 ? "usuario" : "usuarios"} en este tenant
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={() => setShowInviteModal(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white shadow-flit transition-colors hover:bg-flit-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2 sm:w-auto"
        >
          <i className="pi pi-user-plus text-sm" aria-hidden="true" />
          Invitar usuario
        </button>
      </div>

      <div className="flit-list-panel">
        <div className="flit-list-panel__toolbar">
          <div className="flit-search-field max-w-sm">
            <div className="flit-search-field__icon-wrap" aria-hidden="true">
              <i className="pi pi-search" />
            </div>
            <input
              type="search"
              className="flit-search-field__input"
              placeholder="Buscar por nombre o email…"
              value={search}
              onChange={handleSearchChange}
              aria-label="Buscar usuarios"
            />
          </div>
        </div>

        <UsersTable
          users={users}
          isLoading={isLoading}
          error={error}
          onRetry={() => void refetch()}
          onManageRoles={(user) => setRolesModalUser(user)}
        />
      </div>

      {rolesModalUser && (
        <UserRolesModal user={rolesModalUser} onClose={() => setRolesModalUser(null)} />
      )}

      {showInviteModal && <InviteUserModal onClose={() => setShowInviteModal(false)} />}
    </div>
  );
}
