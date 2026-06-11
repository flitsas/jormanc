import { useEffect, useState } from "react";
import { useProcedureType } from "../../procedures-config/api/procedures-config.api.js";
import { useOtDocumentOrder, useUpdateDocumentOrder } from "../api/ot-admin.api.js";
import type { OrderedDocument } from "../api/ot-admin.schemas.js";
import { DocumentOrderDragItem } from "./DocumentOrderDragItem.js";

interface DocumentOrderDragDropProps {
  otId: string;
}

export function DocumentOrderDragDrop({ otId }: DocumentOrderDragDropProps) {
  const { data: entries, isLoading, error, refetch } = useOtDocumentOrder(otId);
  const updateOrder = useUpdateDocumentOrder(otId);
  const [selectedProcedureTypeId, setSelectedProcedureTypeId] = useState<string | null>(null);
  const [localDocuments, setLocalDocuments] = useState<OrderedDocument[]>([]);
  const [draggingId, setDraggingId] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const selectedEntry = entries?.find((e) => e.procedureTypeId === selectedProcedureTypeId);
  const { data: procedureType } = useProcedureType(selectedProcedureTypeId ?? undefined);

  useEffect(() => {
    if (!entries?.length) return;
    if (!selectedProcedureTypeId || !entries.some((e) => e.procedureTypeId === selectedProcedureTypeId)) {
      setSelectedProcedureTypeId(entries[0]!.procedureTypeId);
    }
  }, [entries, selectedProcedureTypeId]);

  useEffect(() => {
    if (!draggingId && !updateOrder.isPending && selectedEntry) {
      setLocalDocuments(selectedEntry.orderedDocuments);
    }
  }, [selectedEntry, draggingId, updateOrder.isPending]);

  async function persistOrder(nextDocuments: OrderedDocument[]) {
    if (!selectedProcedureTypeId) return;
    setToast(null);
    const orderedDocumentTypeIds = nextDocuments.map((d) => d.documentType.id);
    try {
      const result = await updateOrder.mutateAsync({
        procedureTypeId: selectedProcedureTypeId,
        orderedDocumentTypeIds,
      });
      setToast(result.message || "Prelación actualizada");
      setTimeout(() => setToast(null), 3000);
    } catch (err) {
      setToast(err instanceof Error ? err.message : "Error al actualizar prelación");
      if (selectedEntry) setLocalDocuments(selectedEntry.orderedDocuments);
    }
  }

  function handleDrop(targetDocumentTypeId: string) {
    if (!draggingId || draggingId === targetDocumentTypeId) {
      setDraggingId(null);
      return;
    }

    const sorted = [...localDocuments].sort((a, b) => a.orderIndex - b.orderIndex);
    const fromIndex = sorted.findIndex((d) => d.documentType.id === draggingId);
    const toIndex = sorted.findIndex((d) => d.documentType.id === targetDocumentTypeId);
    if (fromIndex < 0 || toIndex < 0) return;

    const reordered = [...sorted];
    const [moved] = reordered.splice(fromIndex, 1);
    reordered.splice(toIndex, 0, moved);

    const updated = reordered.map((doc, i) => ({ ...doc, orderIndex: i + 1 }));
    setLocalDocuments(updated);
    setDraggingId(null);
    void persistOrder(updated);
  }

  if (isLoading) {
    return (
      <p
        className="text-sm text-flit-muted"
        aria-busy="true"
        aria-label="Cargando prelación documental"
      >
        Cargando prelación documental…
      </p>
    );
  }

  if (error) {
    return (
      <div role="alert" className="text-sm text-red-600">
        {error.message}
        <button type="button" onClick={() => void refetch()} className="ml-2 underline">
          Reintentar
        </button>
      </div>
    );
  }

  if (!entries?.length) {
    return (
      <p className="text-sm text-flit-muted" role="status">
        Sin documentos configurados para prelación en este OT.
      </p>
    );
  }

  const sorted = [...localDocuments].sort((a, b) => a.orderIndex - b.orderIndex);
  const procedureLabel =
    procedureType?.name ?? `Tipo de trámite ${selectedProcedureTypeId?.slice(0, 8) ?? ""}`;

  return (
    <div className="space-y-4" role="region" aria-label="Editor de prelación documental">
      <div>
        <label htmlFor="ot-procedure-type-select" className="mb-1 block text-sm font-medium">
          Tipo de trámite
        </label>
        <select
          id="ot-procedure-type-select"
          value={selectedProcedureTypeId ?? ""}
          onChange={(e) => setSelectedProcedureTypeId(e.target.value)}
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
        >
          {entries.map((entry) => (
            <option key={entry.procedureTypeId} value={entry.procedureTypeId}>
              {entry.procedureTypeId}
            </option>
          ))}
        </select>
        <p className="mt-1 text-xs text-flit-muted">{procedureLabel}</p>
      </div>

      {sorted.length === 0 ? (
        <p className="text-sm text-flit-muted" role="status">
          Sin documentos configurados para este tipo de trámite.
        </p>
      ) : (
        <div
          role="list"
          aria-label={`Orden de documentos para ${procedureLabel}`}
          className="grid gap-2"
          aria-busy={updateOrder.isPending}
        >
          {sorted.map((doc, index) => (
            <DocumentOrderDragItem
              key={doc.documentType.id}
              item={doc}
              index={index}
              isDragging={draggingId === doc.documentType.id}
              onDragStart={setDraggingId}
              onDragOver={(e) => e.preventDefault()}
              onDrop={handleDrop}
              onDragEnd={() => setDraggingId(null)}
            />
          ))}
        </div>
      )}

      {toast ? (
        <div
          className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-200"
          role="status"
          aria-live="polite"
        >
          {toast}
        </div>
      ) : null}
    </div>
  );
}
