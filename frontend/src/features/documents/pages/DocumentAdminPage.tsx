import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  sortTemplatesByVersionDesc,
  useDocumentTemplates,
  usePreviewTemplatePdf,
  useUploadDocumentTemplate,
} from "../api/documents.api.js";
import { TemplateUploadModal } from "../components/TemplateUploadModal.js";
import { TemplateVersionsList } from "../components/TemplateVersionsList.js";
import type { DocumentTemplate } from "../api/documents.schemas.js";

const RECENT_TYPES_KEY = "flit_recent_document_type_ids";

function loadRecentDocumentTypeIds(): string[] {
  try {
    const raw = sessionStorage.getItem(RECENT_TYPES_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as unknown;
    return Array.isArray(parsed) ? parsed.filter((id): id is string => typeof id === "string") : [];
  } catch {
    return [];
  }
}

function rememberDocumentTypeId(id: string) {
  const recent = loadRecentDocumentTypeIds().filter((item) => item !== id);
  sessionStorage.setItem(RECENT_TYPES_KEY, JSON.stringify([id, ...recent].slice(0, 8)));
}

function DocumentTypeSelector() {
  const navigate = useNavigate();
  const [documentTypeId, setDocumentTypeId] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const recentIds = loadRecentDocumentTypeIds();

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = documentTypeId.trim();
    const uuidRegex =
      /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    if (!uuidRegex.test(trimmed)) {
      setValidationError("Ingrese un UUID válido de tipo de documento.");
      return;
    }
    setValidationError(null);
    rememberDocumentTypeId(trimmed);
    navigate(`/admin/document-types/${trimmed}/templates`);
  }

  return (
    <div className="p-4 sm:p-6 max-w-xl space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-flit-heading dark:text-flit-heading-dark">
          Administración de plantillas
        </h1>
        <p className="text-sm text-flit-muted mt-1">
          Seleccione el tipo de documento para gestionar sus versiones de plantilla HTML.
        </p>
      </header>

      <form onSubmit={handleSubmit} className="space-y-3">
        <div>
          <label className="block text-sm font-medium mb-1" htmlFor="document-type-id">
            ID del tipo de documento
          </label>
          <input
            id="document-type-id"
            type="text"
            value={documentTypeId}
            onChange={(e) => setDocumentTypeId(e.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
            placeholder="00000000-0000-0000-0000-000000000000"
            aria-invalid={!!validationError}
            aria-describedby={validationError ? "document-type-id-error" : undefined}
          />
          {validationError ? (
            <p id="document-type-id-error" className="mt-1 text-xs text-red-600" role="alert">
              {validationError}
            </p>
          ) : null}
        </div>
        <button type="submit" className="flit-btn flit-btn-primary">
          Gestionar plantillas
        </button>
      </form>

      {recentIds.length > 0 ? (
        <section aria-labelledby="recent-types-heading">
          <h2 id="recent-types-heading" className="text-sm font-semibold text-flit-heading dark:text-flit-heading-dark mb-2">
            Tipos recientes
          </h2>
          <ul className="space-y-1">
            {recentIds.map((id) => (
              <li key={id}>
                <button
                  type="button"
                  className="text-sm text-flit-primary hover:underline font-mono"
                  onClick={() => {
                    rememberDocumentTypeId(id);
                    navigate(`/admin/document-types/${id}/templates`);
                  }}
                >
                  {id}
                </button>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  );
}

function DocumentTemplatesPanel({ documentTypeId }: { documentTypeId: string }) {
  const navigate = useNavigate();
  const [showUpload, setShowUpload] = useState(false);
  const [previewingVersionId, setPreviewingVersionId] = useState<string | null>(null);

  const { data, isLoading, error, refetch } = useDocumentTemplates(documentTypeId);
  const upload = useUploadDocumentTemplate();
  const previewPdf = usePreviewTemplatePdf(documentTypeId);

  const templates = sortTemplatesByVersionDesc(data ?? []);

  async function handlePreviewPdf(template: DocumentTemplate) {
    setPreviewingVersionId(template.templateId);
    try {
      const blob = await previewPdf.mutateAsync(template.templateId);
      const url = URL.createObjectURL(blob);
      window.open(url, "_blank", "noopener,noreferrer");
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (err) {
      window.alert(err instanceof Error ? err.message : "No se pudo generar la vista previa PDF");
    } finally {
      setPreviewingVersionId(null);
    }
  }

  return (
    <div className="p-4 sm:p-6 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <button
            type="button"
            className="text-sm text-flit-muted hover:text-flit-primary mb-2 inline-flex items-center gap-1"
            onClick={() => navigate("/admin/documents")}
          >
            <i className="pi pi-arrow-left" aria-hidden="true" />
            Volver a tipos de documento
          </button>
          <h1 className="text-xl font-semibold text-flit-heading dark:text-flit-heading-dark">
            Plantillas del tipo de documento
          </h1>
          <p className="text-sm text-flit-muted font-mono mt-1">{documentTypeId}</p>
        </div>
        <button
          type="button"
          className="flit-btn flit-btn-primary"
          onClick={() => setShowUpload(true)}
        >
          <i className="pi pi-upload" aria-hidden="true" />
          Subir nueva plantilla
        </button>
      </div>

      <TemplateVersionsList
        templates={templates}
        isLoading={isLoading}
        error={error}
        onRetry={() => void refetch()}
        onPreviewPdf={(template) => void handlePreviewPdf(template)}
        previewingVersionId={previewingVersionId}
      />

      {showUpload ? (
        <TemplateUploadModal
          documentTypeId={documentTypeId}
          onClose={() => setShowUpload(false)}
          isUploading={upload.isPending}
          onUpload={async (input) => {
            rememberDocumentTypeId(documentTypeId);
            return upload.mutateAsync({ documentTypeId, ...input });
          }}
        />
      ) : null}
    </div>
  );
}

export function DocumentAdminPage() {
  const { documentTypeId } = useParams<{ documentTypeId?: string }>();

  if (!documentTypeId) {
    return <DocumentTypeSelector />;
  }

  return <DocumentTemplatesPanel documentTypeId={documentTypeId} />;
}
