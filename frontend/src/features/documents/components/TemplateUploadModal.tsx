import { useId, useState } from "react";
import { FlitModal } from "../../../shared/components/ui/FlitModal.js";
import { detectTemplateMarkers } from "../lib/templateMarkers.js";
import type { DocumentTemplate } from "../api/documents.schemas.js";

const NO_MARKERS_WARNING =
  "La plantilla no contiene marcadores. Los documentos generados no incluirán datos del trámite.";

interface TemplateUploadModalProps {
  documentTypeId: string;
  onClose: () => void;
  onUpload: (input: { file: File; notes?: string }) => Promise<DocumentTemplate>;
  isUploading?: boolean;
}

export function TemplateUploadModal({
  documentTypeId,
  onClose,
  onUpload,
  isUploading = false,
}: TemplateUploadModalProps) {
  const fileInputId = useId();
  const notesInputId = useId();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [notes, setNotes] = useState("");
  const [markersDetected, setMarkersDetected] = useState<string[] | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [isReadingFile, setIsReadingFile] = useState(false);

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    setFileError(null);
    setUploadError(null);
    setSelectedFile(null);
    setMarkersDetected(null);

    if (!file) return;

    if (!file.name.toLowerCase().endsWith(".html") && file.type !== "text/html") {
      setFileError("Seleccione un archivo HTML (.html).");
      return;
    }

    setIsReadingFile(true);
    try {
      const html = await file.text();
      const markers = detectTemplateMarkers(html);
      setSelectedFile(file);
      setMarkersDetected(markers);
    } catch {
      setFileError("No se pudo leer el archivo seleccionado.");
    } finally {
      setIsReadingFile(false);
    }
  }

  async function handleConfirm() {
    if (!selectedFile) return;
    setUploadError(null);
    try {
      await onUpload({ file: selectedFile, notes: notes.trim() || undefined });
      onClose();
    } catch (err) {
      setUploadError(err instanceof Error ? err.message : "Error al subir la plantilla");
    }
  }

  const showPreview = markersDetected !== null && selectedFile !== null;
  const confirmDisabled = !selectedFile || isUploading || isReadingFile;

  return (
    <FlitModal
      title="Subir nueva plantilla"
      subtitle={`Tipo de documento ${documentTypeId.slice(0, 8)}…`}
      onClose={onClose}
      closeLabel="Cancelar subida de plantilla"
      maxWidthClass="max-w-lg"
    >
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1" htmlFor={fileInputId}>
            Archivo HTML
          </label>
          <input
            id={fileInputId}
            type="file"
            accept=".html,text/html"
            className="block w-full text-sm text-flit-muted file:mr-3 file:rounded-lg file:border-0 file:bg-flit-primary file:px-3 file:py-2 file:text-sm file:font-medium file:text-white"
            onChange={(e) => void handleFileChange(e)}
            aria-describedby={fileError ? `${fileInputId}-error` : undefined}
          />
          {fileError ? (
            <p id={`${fileInputId}-error`} className="mt-1 text-xs text-red-600" role="alert">
              {fileError}
            </p>
          ) : null}
          {isReadingFile ? (
            <p className="mt-1 text-xs text-flit-muted" aria-live="polite">
              Analizando marcadores…
            </p>
          ) : null}
        </div>

        {showPreview ? (
          <section aria-labelledby="markers-preview-heading">
            <h3
              id="markers-preview-heading"
              className="text-sm font-semibold text-flit-heading dark:text-flit-heading-dark mb-2"
            >
              Marcadores detectados
            </h3>
            {markersDetected.length === 0 ? (
              <p
                className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800"
                role="alert"
              >
                {NO_MARKERS_WARNING}
              </p>
            ) : (
              <ul className="max-h-40 overflow-y-auto rounded-lg border border-slate-200 dark:border-flit-border-dark divide-y divide-slate-100 dark:divide-flit-border-dark/60">
                {markersDetected.map((marker) => (
                  <li
                    key={marker}
                    className="px-3 py-2 font-mono text-xs text-flit-heading dark:text-flit-heading-dark"
                  >
                    {`{{${marker}}}`}
                  </li>
                ))}
              </ul>
            )}
          </section>
        ) : null}

        <div>
          <label className="block text-sm font-medium mb-1" htmlFor={notesInputId}>
            Notas (opcional)
          </label>
          <textarea
            id={notesInputId}
            rows={2}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Ej. Ajuste de encabezado para traspaso"
          />
        </div>

        {uploadError ? (
          <p className="text-sm text-red-600" role="alert">
            {uploadError}
          </p>
        ) : null}

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            className="flit-btn flit-btn-outline"
            onClick={onClose}
            disabled={isUploading}
          >
            Cancelar
          </button>
          <button
            type="button"
            className="flit-btn flit-btn-primary"
            onClick={() => void handleConfirm()}
            disabled={confirmDisabled}
            aria-busy={isUploading}
          >
            Confirmar
          </button>
        </div>
      </div>
    </FlitModal>
  );
}

export { NO_MARKERS_WARNING };
