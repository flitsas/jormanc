import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { VehicleCaptureStep } from "./ProcedureStepper/VehicleCaptureStep.js";

describe("VehicleWarningBanner — AC2", () => {
  const warnings = ["Multa SIMIT: $500.000", "Restricción: PRENDA"];

  it("muestra hallazgos en banner amarillo sin bloquear Siguiente", async () => {
    const onContinue = vi.fn();
    const user = userEvent.setup();

    render(
      <VehicleCaptureStep
        procedureId="00000000-0000-0000-0000-000000000001"
        vehicleQueryKey="placa"
        isSubmitting={false}
        error={null}
        result={{
          procedureId: "00000000-0000-0000-0000-000000000001",
          status: "draft",
          vehicle: { plate: "ABC123" },
          warnings,
        }}
        onCapture={vi.fn()}
        onContinue={onContinue}
      />,
    );

    expect(screen.getByText(/hallazgos informativos/i)).toBeInTheDocument();
    expect(screen.getByText("Multa SIMIT: $500.000")).toBeInTheDocument();
    expect(screen.getByText("Restricción: PRENDA")).toBeInTheDocument();

    const nextButton = screen.getByRole("button", { name: /siguiente/i });
    expect(nextButton).toBeEnabled();
    await user.click(nextButton);
    expect(onContinue).toHaveBeenCalled();
  });
});
