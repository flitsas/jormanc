import { useState } from "react";
import { ProceduresGrid, type ProceduresGridFilters } from "../components/ProceduresGrid.js";
import { useProceduresList } from "../api/procedures.api.js";

export function ProceduresPage() {
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<ProceduresGridFilters>({
    status: "",
    fechaFrom: "",
  });
  const pageSize = 20;

  const { data, isLoading, error, refetch } = useProceduresList({
    page,
    pageSize,
    status: filters.status || undefined,
    fechaFrom: filters.fechaFrom || undefined,
  });

  const items = data?.data ?? [];
  const total = data?.total ?? 0;

  function handleFiltersChange(next: ProceduresGridFilters) {
    setFilters(next);
    setPage(1);
  }

  return (
    <div className="p-4 sm:p-6 space-y-4">
      <div>
        <h1 className="flit-section-title">
          <i className="pi pi-folder-open text-flit-primary" aria-hidden="true" />
          Trámites
        </h1>
        {!isLoading && !error ? (
          <p className="mt-0.5 text-sm text-flit-muted dark:text-flit-muted-dark">
            <strong className="font-semibold text-flit-heading dark:text-flit-heading-dark">
              {total}
            </strong>{" "}
            {total === 1 ? "trámite" : "trámites"} en este tenant
          </p>
        ) : null}
      </div>

      <ProceduresGrid
        items={items}
        total={total}
        page={page}
        pageSize={pageSize}
        filters={filters}
        isLoading={isLoading}
        error={error}
        onFiltersChange={handleFiltersChange}
        onPageChange={setPage}
        onRetry={() => void refetch()}
      />
    </div>
  );
}
