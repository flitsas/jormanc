import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { VehicleCaptureStep } from "./VehicleCaptureStep.js";

describe("VehicleCaptureStep — AC2", () => {
  const baseProps = {
    procedureId: "00000000-0000-0000-0000-000000000001",
    isSubmitting: false,
    error: null,
    result: null,
    onCapture: vi.fn(),
    onContinue: vi.fn(),
  };

  it('muestra label "Placa" cuando vehicle_query_key=placa', () => {
    render(<VehicleCaptureStep {...baseProps} vehicleQueryKey="placa" />);
    expect(screen.getByLabelText(/^placa/i)).toBeInTheDocument();
  });

  it('muestra label "VIN / Número de chasis" cuando vehicle_query_key=vin', () => {
    render(<VehicleCaptureStep {...baseProps} vehicleQueryKey="vin" />);
    expect(screen.getByLabelText(/vin \/ número de chasis/i)).toBeInTheDocument();
  });

  it("integra VehicleWarningBanner debajo del campo de placa", () => {
    render(
      <VehicleCaptureStep
        {...baseProps}
        vehicleQueryKey="placa"
        result={{
          procedureId: baseProps.procedureId,
          status: "draft",
          vehicle: { plate: "XYZ999" },
          warnings: ["Multa SIMIT: $500.000"],
        }}
      />,
    );
    expect(screen.getByText(/hallazgos informativos/i)).toBeInTheDocument();
  });
});
