import type { ProcedureStep } from "../api/procedures-config.schemas.js";

interface StepCardProps {
  step: ProcedureStep;
  index: number;
  isDragging: boolean;
  onDragStart: (stepId: string) => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: (targetStepId: string) => void;
  onDragEnd: () => void;
  isSelected: boolean;
  onSelect: () => void;
}

export function StepCard({
  step,
  index,
  isDragging,
  onDragStart,
  onDragOver,
  onDrop,
  onDragEnd,
  isSelected,
  onSelect,
}: StepCardProps) {
  return (
    <div
      role="listitem"
      draggable
      aria-grabbed={isDragging}
      aria-label={`Paso ${index + 1}: ${step.name}`}
      onDragStart={() => onDragStart(step.id)}
      onDragOver={onDragOver}
      onDrop={(e) => {
        e.preventDefault();
        onDrop(step.id);
      }}
      onDragEnd={onDragEnd}
      onClick={onSelect}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onSelect();
        }
      }}
      tabIndex={0}
      className={[
        "cursor-grab rounded-lg border p-3 transition-shadow active:cursor-grabbing",
        isSelected
          ? "border-flit-primary bg-flit-primary/5 shadow-flit"
          : "border-flit-border dark:border-flit-border-dark bg-white dark:bg-flit-surface-dark",
        isDragging ? "opacity-50" : "",
      ].join(" ")}
    >
      <div className="flex items-center gap-2">
        <i className="pi pi-bars text-flit-muted" aria-hidden="true" />
        <span className="text-xs font-semibold text-flit-muted">#{step.orderIndex}</span>
        <span className="font-medium text-flit-heading dark:text-flit-heading-dark">
          {step.name}
        </span>
        <span className="ml-auto text-xs text-flit-muted">{step.stepType}</span>
      </div>
    </div>
  );
}
