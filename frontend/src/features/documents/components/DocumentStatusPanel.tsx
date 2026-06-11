import {
  useConsolidateDocuments,
  useProcedureDocuments,
} from "../api/documents.api.js";
import { getLatestConsolidatedPackage } from "../lib/consolidated.js";
import { ConsolidatedPackageCard } from "./ConsolidatedPackageCard.js";
import { DocumentItem } from "./DocumentItem.js";

interface DocumentStatusPanelProps {
  procedureId: string;
}

export function DocumentStatusPanel({ procedureId }: DocumentStatusPanelProps) {
  const { data, isLoading, error, refetch } = useProcedureDocuments(procedureId);
  const consolidate = useConsolidateDocuments(procedureId);

  if (isLoading) {
    return (
      <section
        aria-label="Cargando estado documental"
        aria-busy="true"
        className="space-y-3 rounded-lg border border-flit-border p-4 dark:border-flit-border-dark"
      >
        <div className="h-6 w-56 animate-pulse rounded bg-flit-border/40 dark:bg-flit-border-dark/40" />
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="h-14 animate-pulse rounded bg-flit-border/30 dark:bg-flit-border-dark/30" />
        ))}
      </section>
    );
  }

  if (error) {
    return (
      <section
        role="alert"
        className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-950/30"
      >
        <p className="text-sm font-medium text-red-800 dark:text-red-200">
          No se pudo cargar el estado documental
        </p>
        <p className="text-sm text-red-700 dark:text-red-300">{error.message}</p>
        <button
          type="button"
          onClick={() => void refetch()}
          className="mt-2 rounded-lg border px-3 py-1.5 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-flit-primary"
        >
          Reintentar
        </button>
      </section>
    );
  }

  const documents = data?.documents ?? [];
  const latestPackage = getLatestConsolidatedPackage(data?.consolidatedPackages ?? []);

  return (
    <section
      aria-labelledby="document-status-panel-title"
      className="space-y-4 rounded-lg border border-flit-border p-4 dark:border-flit-border-dark"
    >
      <h2
        id="document-status-panel-title"
        className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
      >
        <i className="pi pi-folder-open mr-2 text-flit-primary" aria-hidden="true" />
        Estado documental
      </h2>

      {documents.length === 0 ? (
        <p className="text-sm text-flit-muted dark:text-flit-muted-dark" role="status">
          Sin documentos configurados para este trámite.
        </p>
      ) : (
        <ul
          className="divide-y divide-flit-border rounded-md border border-flit-border dark:divide-flit-border-dark dark:border-flit-border-dark"
          aria-label="Lista de documentos del trámite"
        >
          {documents.map((doc) => (
            <DocumentItem key={doc.id} document={doc} />
          ))}
        </ul>
      )}

      <ConsolidatedPackageCard
        procedureId={procedureId}
        latestPackage={latestPackage}
        isConsolidating={consolidate.isPending}
        consolidateError={consolidate.error}
        onConsolidate={() => void consolidate.mutate()}
      />
    </section>
  );
}
