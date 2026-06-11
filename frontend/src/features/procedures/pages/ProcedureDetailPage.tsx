import { Link, useParams } from "react-router-dom";
import { FlitListPanelError } from "../../../shared/components/ui/FlitListPanelError.js";
import {
  useProcedureAttachments,
  useProcedureDetail,
  useSecondarySellers,
  useUploadAttachment,
} from "../api/procedures.api.js";
import { DocumentStatusPanel } from "../../documents/components/DocumentStatusPanel.js";
import { AttachmentsPanel } from "../components/AttachmentsPanel.js";
import { DynamicStepper } from "../components/ProcedureStepper/DynamicStepper.js";
import { SecondarySellerAccordion } from "../components/SecondarySellerAccordion.js";

export function ProcedureDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, error, refetch } = useProcedureDetail(id);
  const attachmentsQuery = useProcedureAttachments(id);
  const secondarySellersQuery = useSecondarySellers(id);
  const uploadAttachment = useUploadAttachment(id ?? "");

  if (isLoading) {
    return (
      <div className="p-4 sm:p-6" aria-label="Cargando trámite" aria-busy="true">
        <div className="h-8 w-48 animate-pulse rounded bg-flit-border/40 dark:bg-flit-border-dark/40 mb-4" />
        <div className="h-64 animate-pulse rounded-lg bg-flit-border/30 dark:bg-flit-border-dark/30" />
      </div>
    );
  }

  if (error || !id) {
    return (
      <div className="p-4 sm:p-6">
        <FlitListPanelError
          title="No se pudo cargar el trámite"
          message={error?.message ?? "Identificador de trámite inválido."}
          onRetry={() => void refetch()}
        />
      </div>
    );
  }

  return (
    <div className="p-4 sm:p-6 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
        <div>
          <h1 className="flit-section-title">
            <i className="pi pi-file-edit text-flit-primary" aria-hidden="true" />
            Trámite {data?.compositeId ?? id}
          </h1>
          {data ? (
            <p className="mt-0.5 text-sm text-flit-muted dark:text-flit-muted-dark">
              Snapshot: <code className="text-xs">{data.procedureTypeSnapshotId}</code> · Estado:{" "}
              <span className="font-medium text-flit-heading dark:text-flit-heading-dark">
                {data.status}
              </span>
            </p>
          ) : null}
        </div>
        <Link to="/procedures" className="text-sm text-flit-primary hover:underline">
          ← Volver al listado
        </Link>
      </div>

      <DynamicStepper procedureId={id} />

      <SecondarySellerAccordion
        sellers={secondarySellersQuery.data ?? []}
        isLoading={secondarySellersQuery.isLoading}
        error={secondarySellersQuery.error}
        onRetry={() => void secondarySellersQuery.refetch()}
      />

      <DocumentStatusPanel procedureId={id} />

      <AttachmentsPanel
        attachments={attachmentsQuery.data ?? []}
        isLoading={attachmentsQuery.isLoading}
        error={attachmentsQuery.error}
        isUploading={uploadAttachment.isPending}
        uploadError={uploadAttachment.error}
        onRetry={() => void attachmentsQuery.refetch()}
        onUpload={(file, labelSlug) => void uploadAttachment.mutate({ file, labelSlug })}
      />
    </div>
  );
}
