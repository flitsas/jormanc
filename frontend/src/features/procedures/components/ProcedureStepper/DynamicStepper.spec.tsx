import type { ReactElement } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { DynamicStepper } from "./DynamicStepper.js";
import * as proceduresApi from "../../api/procedures.api.js";

const SNAPSHOT_ID = "00000000-0000-0000-0000-000000000050";
const PROCEDURE_ID = "00000000-0000-0000-0000-000000000001";

const MOCK_DETAIL = {
  id: PROCEDURE_ID,
  compositeId: "TRASP-01_ABC-001",
  status: "draft",
  procedureTypeSnapshotId: SNAPSHOT_ID,
  vehicleQueryKey: "placa",
  currentStepOrder: 1,
  stepData: {},
  snapshotConfig: {
    steps: [
      {
        id: "00000000-0000-0000-0000-000000000010",
        orderIndex: 1,
        name: "Vehículo",
        stepType: "vehicle",
        isRequired: true,
        sections: [],
      },
      {
        id: "00000000-0000-0000-0000-000000000011",
        orderIndex: 2,
        name: "Datos generales",
        stepType: "form",
        isRequired: true,
        sections: [
          {
            id: "00000000-0000-0000-0000-000000000020",
            stepId: "00000000-0000-0000-0000-000000000011",
            orderIndex: 1,
            slug: "general",
            name: "General",
            isCollapsible: false,
            fields: [
              {
                id: "00000000-0000-0000-0000-000000000030",
                sectionId: "00000000-0000-0000-0000-000000000020",
                orderIndex: 1,
                slug: "observacion",
                name: "Observación",
                fieldType: "text",
                isRequired: false,
                config: {},
              },
            ],
          },
        ],
      },
    ],
  },
};

function renderWithClient(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe("DynamicStepper — AC1", () => {
  beforeEach(() => {
    vi.spyOn(proceduresApi, "useProcedureDetail").mockReturnValue({
      data: MOCK_DETAIL,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as never);

    vi.spyOn(proceduresApi, "useCaptureVehicle").mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
      error: null,
      data: null,
    } as never);

    vi.spyOn(proceduresApi, "useAddActor").mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
      error: null,
    } as never);
  });

  it("renderiza pasos desde procedureTypeSnapshotId (snapshot config)", async () => {
    renderWithClient(<DynamicStepper procedureId={PROCEDURE_ID} />);

    expect(screen.getByRole("button", { name: /1\.\s*vehículo/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /2\.\s*datos generales/i })).toBeInTheDocument();

    const content = screen.getByTestId("dynamic-stepper-content");
    expect(content).toHaveAttribute("data-snapshot-id", SNAPSHOT_ID);
    expect(screen.getByRole("heading", { name: /captura de vehículo/i })).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /2\.\s*datos generales/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Datos generales" })).toBeInTheDocument();
    });
    expect(screen.getByLabelText(/observación/i)).toBeInTheDocument();
  });
});
