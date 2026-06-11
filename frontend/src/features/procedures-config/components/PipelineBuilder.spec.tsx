import { render, screen, fireEvent } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { PipelineBuilder } from "./PipelineBuilder.js";
import type { ProcedureStep } from "../api/procedures-config.schemas.js";

const STEPS: ProcedureStep[] = [
  {
    id: "00000000-0000-0000-0000-000000000001",
    procedureTypeId: "00000000-0000-0000-0000-000000000099",
    orderIndex: 1,
    name: "Paso 1",
    stepType: "form",
    isRequired: true,
    sections: [],
  },
  {
    id: "00000000-0000-0000-0000-000000000002",
    procedureTypeId: "00000000-0000-0000-0000-000000000099",
    orderIndex: 2,
    name: "Paso 2",
    stepType: "form",
    isRequired: true,
    sections: [],
  },
  {
    id: "00000000-0000-0000-0000-000000000003",
    procedureTypeId: "00000000-0000-0000-0000-000000000099",
    orderIndex: 3,
    name: "Paso 3",
    stepType: "form",
    isRequired: true,
    sections: [],
  },
  {
    id: "00000000-0000-0000-0000-000000000004",
    procedureTypeId: "00000000-0000-0000-0000-000000000099",
    orderIndex: 4,
    name: "Paso 4",
    stepType: "form",
    isRequired: true,
    sections: [],
  },
];

describe("PipelineBuilder", () => {
  it("AC1 renderiza 4 pasos como tarjetas arrastrables", () => {
    render(
      <PipelineBuilder
        steps={STEPS}
        selectedStepId={STEPS[0].id}
        onSelectStep={vi.fn()}
        onReorder={vi.fn()}
      />,
    );
    expect(screen.getByLabelText(/pipeline de pasos/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/paso 1: paso 1/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/paso 4: paso 4/i)).toBeInTheDocument();
  });

  it("AC1 al reordenar invoca onReorder con nuevo order_index", () => {
    const onReorder = vi.fn();
    render(
      <PipelineBuilder
        steps={STEPS}
        selectedStepId={STEPS[0].id}
        onSelectStep={vi.fn()}
        onReorder={onReorder}
      />,
    );

    const paso3 = screen.getByLabelText(/paso 3: paso 3/i);
    const paso1 = screen.getByLabelText(/paso 1: paso 1/i);

    fireEvent.dragStart(paso3);
    fireEvent.dragOver(paso1);
    fireEvent.drop(paso1);

    expect(onReorder).toHaveBeenCalledWith(STEPS[2].id, 1);
  });
});
