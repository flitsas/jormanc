import { useState } from "react";
import { useCompaniesList } from "../api/companies.api.js";
import { CompaniesTable } from "../components/CompaniesTable.js";
import { CompanyFormTabs } from "../components/CompanyFormTabs.js";
import { CreateCompanyModal } from "../components/CreateCompanyModal.js";
import type { CompanyListItem } from "../api/companies.schemas.js";

export function CompaniesPage() {
  const [page, setPage] = useState(1);
  const [nitFilter, setNitFilter] = useState("");
  const [nameFilter, setNameFilter] = useState("");
  const [debouncedNit, setDebouncedNit] = useState("");
  const [debouncedName, setDebouncedName] = useState("");
  const [nitTimer, setNitTimer] = useState<ReturnType<typeof setTimeout> | null>(null);
  const [nameTimer, setNameTimer] = useState<ReturnType<typeof setTimeout> | null>(null);

  const [editingCompany, setEditingCompany] = useState<CompanyListItem | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const pageSize = 20;
  const { data, isLoading, error, refetch } = useCompaniesList({
    page,
    pageSize,
    nit: debouncedNit || undefined,
    name: debouncedName || undefined,
  });

  function handleNitChange(e: React.ChangeEvent<HTMLInputElement>) {
    const value = e.target.value;
    setNitFilter(value);
    setPage(1);
    if (nitTimer) clearTimeout(nitTimer);
    setNitTimer(setTimeout(() => setDebouncedNit(value), 350));
  }

  function handleNameChange(e: React.ChangeEvent<HTMLInputElement>) {
    const value = e.target.value;
    setNameFilter(value);
    setPage(1);
    if (nameTimer) clearTimeout(nameTimer);
    setNameTimer(setTimeout(() => setDebouncedName(value), 350));
  }

  const companies = data?.data ?? [];
  const total = data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div className="p-4 sm:p-6 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="flit-section-title">
            <i className="pi pi-building text-flit-primary" aria-hidden="true" />
            Compañías
          </h1>
          {!isLoading && !error && (
            <p className="mt-0.5 text-sm text-flit-muted dark:text-flit-muted-dark">
              <strong className="font-semibold text-flit-heading dark:text-flit-heading-dark">
                {total}
              </strong>{" "}
              {total === 1 ? "compañía" : "compañías"}
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={() => setShowCreate(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white shadow-flit"
        >
          <i className="pi pi-plus text-sm" aria-hidden="true" />
          Nueva compañía
        </button>
      </div>

      <div className="flit-list-panel">
        <div className="flit-list-panel__toolbar flex flex-wrap gap-3">
          <div className="flit-search-field max-w-xs">
            <div className="flit-search-field__icon-wrap" aria-hidden="true">
              <i className="pi pi-search" />
            </div>
            <input
              type="search"
              className="flit-search-field__input"
              placeholder="Filtrar por NIT…"
              value={nitFilter}
              onChange={handleNitChange}
              aria-label="Filtrar por NIT"
            />
          </div>
          <div className="flit-search-field max-w-xs">
            <div className="flit-search-field__icon-wrap" aria-hidden="true">
              <i className="pi pi-search" />
            </div>
            <input
              type="search"
              className="flit-search-field__input"
              placeholder="Filtrar por nombre…"
              value={nameFilter}
              onChange={handleNameChange}
              aria-label="Filtrar por nombre"
            />
          </div>
        </div>

        <CompaniesTable
          companies={companies}
          isLoading={isLoading}
          error={error}
          onRetry={() => void refetch()}
          onEdit={(c) => setEditingCompany(c)}
        />

        {total > pageSize && (
          <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3 dark:border-flit-border-dark">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-40"
            >
              Anterior
            </button>
            <span className="text-sm text-flit-muted">
              Página {page} de {totalPages}
            </span>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-40"
            >
              Siguiente
            </button>
          </div>
        )}
      </div>

      {editingCompany && (
        <CompanyFormTabs company={editingCompany} onClose={() => setEditingCompany(null)} />
      )}

      {showCreate && (
        <CreateCompanyModal
          onClose={() => setShowCreate(false)}
          onCreated={() => void refetch()}
        />
      )}
    </div>
  );
}
