import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { OtLabelsManager } from "./OtLabelsManager.js";
import type { OtDocumentLabel } from "../api/ot-admin.schemas.js";

const OT_ID = "00000000-0000-0000-0000-000000000001";

const LABELS: OtDocumentLabel[] = [
  {
    id: "00000000-0000-0000-0000-000000000021",
    otId: OT_ID,
    slug: "paz_y_salvo",
    displayName: "Paz y Salvo Municipal",
    isActive: true,
    createdAt: "2026-01-01T00:00:00Z",
  },
];

const refetch = vi.fn();
const createMutate = vi.fn();
const deleteMutate = vi.fn();

vi.mock("../api/ot-admin.api.js", () => ({
  useOtLabels: vi.fn(),
  useCreateOtLabel: () => ({ mutateAsync: createMutate, isPending: false }),
  useUpdateOtLabel: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useDeleteOtLabel: () => ({ mutateAsync: deleteMutate, isPending: false }),
  useOtLabelImpact: vi.fn(),
}));

import { useOtLabels, useOtLabelImpact } from "../api/ot-admin.api.js";

function renderWithProviders(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

describe("OtLabelsManager", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useOtLabelImpact).mockReturnValue({
      data: { impactCount: 23 },
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as ReturnType<typeof useOtLabelImpact>);
  });

  it("AC3 muestra skeleton mientras carga", () => {
    vi.mocked(useOtLabels).mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch,
    } as ReturnType<typeof useOtLabels>);

    renderWithProviders(<OtLabelsManager otId={OT_ID} />);
    expect(screen.getByLabelText(/cargando etiquetas personalizadas/i)).toBeInTheDocument();
  });

  it("AC3 muestra estado vacío", () => {
    vi.mocked(useOtLabels).mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch,
    } as ReturnType<typeof useOtLabels>);

    renderWithProviders(<OtLabelsManager otId={OT_ID} />);
    expect(screen.getByText(/sin etiquetas personalizadas/i)).toBeInTheDocument();
  });

  it("AC3 muestra error con reintentar", async () => {
    vi.mocked(useOtLabels).mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error("Error de red"),
      refetch,
    } as ReturnType<typeof useOtLabels>);

    const user = userEvent.setup();
    renderWithProviders(<OtLabelsManager otId={OT_ID} />);
    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(refetch).toHaveBeenCalled();
  });

  it("AC3 lista etiquetas con acciones editar y eliminar", () => {
    vi.mocked(useOtLabels).mockReturnValue({
      data: LABELS,
      isLoading: false,
      error: null,
      refetch,
    } as ReturnType<typeof useOtLabels>);

    renderWithProviders(<OtLabelsManager otId={OT_ID} />);
    expect(screen.getByText("Paz y Salvo Municipal")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /editar etiqueta paz y salvo municipal/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /eliminar etiqueta paz y salvo municipal/i }),
    ).toBeInTheDocument();
  });

  it("AC2 DeleteLabelModal muestra impacto y exige checkbox", async () => {
    vi.mocked(useOtLabels).mockReturnValue({
      data: LABELS,
      isLoading: false,
      error: null,
      refetch,
    } as ReturnType<typeof useOtLabels>);

    const user = userEvent.setup();
    renderWithProviders(<OtLabelsManager otId={OT_ID} />);

    await user.click(
      screen.getByRole("button", { name: /eliminar etiqueta paz y salvo municipal/i }),
    );

    expect(screen.getByText(/esta etiqueta está en uso en 23 adjuntos/i)).toBeInTheDocument();

    const deleteBtn = screen.getByRole("button", { name: /^eliminar$/i });
    expect(deleteBtn).toBeDisabled();

    await user.click(screen.getByRole("checkbox"));
    expect(deleteBtn).not.toBeDisabled();
  });
});
