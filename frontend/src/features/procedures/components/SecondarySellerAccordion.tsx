import { useId, useState } from "react";
import type { SecondarySeller } from "../api/procedures.schemas.js";

interface SecondarySellerAccordionProps {
  sellers: SecondarySeller[];
  isLoading: boolean;
  error: Error | null;
  onRetry: () => void;
}

export function SecondarySellerAccordion({
  sellers,
  isLoading,
  error,
  onRetry,
}: SecondarySellerAccordionProps) {
  const headingId = useId();
  const panelId = useId();
  const [expanded, setExpanded] = useState(false);

  if (isLoading) {
    return (
      <section aria-labelledby={headingId} className="rounded-lg border border-flit-border p-4 dark:border-flit-border-dark">
        <div className="h-6 w-48 animate-pulse rounded bg-flit-border/40" aria-busy="true" aria-label="Cargando vendedores secundarios" />
      </section>
    );
  }

  if (error) {
    return (
      <section aria-labelledby={headingId} className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-950/30" role="alert">
        <h2 id={headingId} className="text-sm font-semibold text-red-800 dark:text-red-200">
          Vendedores secundarios RUNT
        </h2>
        <p className="mt-1 text-sm text-red-700 dark:text-red-300">{error.message}</p>
        <button
          type="button"
          onClick={onRetry}
          className="mt-2 rounded-lg border px-3 py-1.5 text-sm"
        >
          Reintentar
        </button>
      </section>
    );
  }

  return (
    <section className="rounded-lg border border-flit-border dark:border-flit-border-dark">
      <h2 id={headingId} className="sr-only">
        Vendedores secundarios RUNT
      </h2>
      <button
        type="button"
        id={headingId}
        aria-expanded={expanded}
        aria-controls={panelId}
        onClick={() => setExpanded((v) => !v)}
        className="flex w-full items-center justify-between px-4 py-3 text-left text-sm font-semibold text-flit-heading hover:bg-flit-primary/5 dark:text-flit-heading-dark"
      >
        <span>
          <i className="pi pi-users mr-2 text-flit-primary" aria-hidden="true" />
          Vendedores secundarios RUNT
          <span className="ml-2 text-xs font-normal text-flit-muted">({sellers.length})</span>
        </span>
        <i className={`pi ${expanded ? "pi-chevron-up" : "pi-chevron-down"}`} aria-hidden="true" />
      </button>

      {expanded ? (
        <div id={panelId} role="region" aria-labelledby={headingId} className="border-t border-flit-border px-4 py-3 dark:border-flit-border-dark">
          {sellers.length === 0 ? (
            <p className="text-sm text-flit-muted" role="status">
              No hay vendedores secundarios registrados en RUNT para este trámite.
            </p>
          ) : (
            <ul className="space-y-2">
              {sellers.map((seller) => (
                <li
                  key={`${seller.documentType}-${seller.documentNumber}`}
                  className="rounded-md border border-flit-border/60 px-3 py-2 text-sm dark:border-flit-border-dark/60"
                >
                  <p className="font-medium text-flit-heading dark:text-flit-heading-dark">{seller.fullName}</p>
                  <p className="text-flit-muted">
                    {seller.documentType} {seller.documentNumber}
                    {seller.ownershipPct != null ? ` · ${seller.ownershipPct}%` : null}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </section>
  );
}
