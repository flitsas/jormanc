import { useEffect, useState } from "react";
import type { ProcedureStep } from "../api/procedures-config.schemas.js";
import { StepCard } from "./StepCard.js";

interface PipelineBuilderProps {
  steps: ProcedureStep[];
  onReorder: (stepId: string, newOrderIndex: number) => void;
  selectedStepId: string | null;
  onSelectStep: (stepId: string) => void;
  isReordering?: boolean;
}

export function PipelineBuilder({
  steps,
  onReorder,
  selectedStepId,
  onSelectStep,
  isReordering = false,
}: PipelineBuilderProps) {
  const [localSteps, setLocalSteps] = useState(steps);
  const [draggingId, setDraggingId] = useState<string | null>(null);

  const sorted = [...localSteps].sort((a, b) => a.orderIndex - b.orderIndex);

  useEffect(() => {
    if (!draggingId && !isReordering) {
      setLocalSteps(steps);
    }
  }, [steps, draggingId, isReordering]);

  function handleDrop(targetStepId: string) {
    if (!draggingId || draggingId === targetStepId) {
      setDraggingId(null);
      return;
    }

    const fromIndex = sorted.findIndex((s) => s.id === draggingId);
    const toIndex = sorted.findIndex((s) => s.id === targetStepId);
    if (fromIndex < 0 || toIndex < 0) return;

    const reordered = [...sorted];
    const [moved] = reordered.splice(fromIndex, 1);
    reordered.splice(toIndex, 0, moved);

    const updated = reordered.map((s, i) => ({ ...s, orderIndex: i + 1 }));
    setLocalSteps(updated);
    setDraggingId(null);

    const movedStep = updated.find((s) => s.id === draggingId);
    if (movedStep) onReorder(draggingId, movedStep.orderIndex);
  }

  if (sorted.length === 0) {
    return (
      <p className="text-sm text-flit-muted dark:text-flit-muted-dark" role="status">
        No hay pasos en el pipeline. Agregue pasos desde el backend.
      </p>
    );
  }

  return (
    <div
      role="list"
      aria-label="Pipeline de pasos del trámite"
      className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4"
    >
      {sorted.map((step, index) => (
        <StepCard
          key={step.id}
          step={step}
          index={index}
          isDragging={draggingId === step.id}
          onDragStart={setDraggingId}
          onDragOver={(e) => e.preventDefault()}
          onDrop={handleDrop}
          onDragEnd={() => setDraggingId(null)}
          isSelected={selectedStepId === step.id}
          onSelect={() => onSelectStep(step.id)}
        />
      ))}
    </div>
  );
}
