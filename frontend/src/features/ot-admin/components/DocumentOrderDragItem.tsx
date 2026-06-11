import type { OrderedDocument } from "../api/ot-admin.schemas.js";

interface DocumentOrderDragItemProps {
  item: OrderedDocument;
  index: number;
  isDragging: boolean;
  onDragStart: (documentTypeId: string) => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: (targetDocumentTypeId: string) => void;
  onDragEnd: () => void;
}

export function DocumentOrderDragItem({
  item,
  index,
  isDragging,
  onDragStart,
  onDragOver,
  onDrop,
  onDragEnd,
}: DocumentOrderDragItemProps) {
  const docId = item.documentType.id;
  const label = `Documento ${index + 1}: ${item.documentType.name}`;

  return (
    <div
      role="listitem"
      draggable
      aria-grabbed={isDragging}
      aria-label={label}
      onDragStart={() => onDragStart(docId)}
      onDragOver={onDragOver}
      onDrop={(e) => {
        e.preventDefault();
        onDrop(docId);
      }}
      onDragEnd={onDragEnd}
      className={[
        "flex cursor-grab items-center gap-3 rounded-lg border px-3 py-2.5 transition-shadow active:cursor-grabbing",
        "border-flit-border bg-white dark:border-flit-border-dark dark:bg-flit-surface-dark",
        isDragging ? "opacity-50 shadow-flit" : "",
      ].join(" ")}
    >
      <i className="pi pi-bars text-flit-muted" aria-hidden="true" />
      <span className="text-xs font-semibold text-flit-muted">#{index + 1}</span>
      <span className="font-medium text-flit-heading dark:text-flit-heading-dark">
        {item.documentType.name}
      </span>
    </div>
  );
}
