import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { DocumentOrderDragDrop } from "./DocumentOrderDragDrop.js";
import type { DocumentOrderEntry } from "../api/ot-admin.schemas.js";

const PROCEDURE_TYPE_ID = "00000000-0000-0000-0000-000000000099";
const OT_ID = "00000000-0000-0000-0000-000000000001";

const DOC = (id: string, name: string, orderIndex: number) => ({
  orderIndex,
  documentType: { id, name },
});

const ENTRIES: DocumentOrderEntry[] = [
  {
    procedureTypeId: PROCEDURE_TYPE_ID,
    orderedDocuments: [
      DOC("00000000-0000-0000-0000-000000000011", "Tarjeta de propiedad", 1),
      DOC("00000000-0000-0000-0000-000000000012", "SOAT", 2),
      DOC("00000000-0000-0000-0000-000000000013", "RTM", 3),
      DOC("00000000-0000-0000-0000-000000000014", "Paz y salvo", 4),
    ],
  },
];

const mutateAsync = vi.fn();
const refetch = vi.fn();

vi.mock("../api/ot-admin.api.js", () => ({
  useOtDocumentOrder: vi.fn(),
  useUpdateDocumentOrder: () => ({
    mutateAsync,
    isPending: false,
  }),
}));

vi.mock("../../procedures-config/api/procedures-config.api.js", () => ({
  useProcedureType: () => ({
    data: { name: "Traspaso Simple" },
    isLoading: false,
    error: null,
  }),
}));

import { useOtDocumentOrder } from "../api/ot-admin.api.js";

function renderWithProviders(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

describe("DocumentOrderDragDrop", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mutateAsync.mockResolvedValue({
      procedureTypeId: PROCEDURE_TYPE_ID,
      orderedDocuments: ENTRIES[0]!.orderedDocuments,
      updated: true,
      message: "Prelación actualizada",
    });
  });

  it("AC1 muestra skeleton mientras carga", () => {
    vi.mocked(useOtDocumentOrder).mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch,
    } as ReturnType<typeof useOtDocumentOrder>);

    renderWithProviders(<DocumentOrderDragDrop otId={OT_ID} />);
    expect(screen.getByLabelText(/cargando prelación documental/i)).toBeInTheDocument();
  });

  it("muestra estado vacío sin entradas", () => {
    vi.mocked(useOtDocumentOrder).mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch,
    } as ReturnType<typeof useOtDocumentOrder>);

    renderWithProviders(<DocumentOrderDragDrop otId={OT_ID} />);
    expect(screen.getByText(/sin documentos configurados para prelación/i)).toBeInTheDocument();
  });

  it("muestra error con reintentar", async () => {
    vi.mocked(useOtDocumentOrder).mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error("Error de red"),
      refetch,
    } as ReturnType<typeof useOtDocumentOrder>);

    const user = userEvent.setup();
    renderWithProviders(<DocumentOrderDragDrop otId={OT_ID} />);
    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(refetch).toHaveBeenCalled();
  });

  it("AC1 al reordenar envía PUT con nuevo orden y muestra toast", async () => {
    vi.mocked(useOtDocumentOrder).mockReturnValue({
      data: ENTRIES,
      isLoading: false,
      error: null,
      refetch,
    } as ReturnType<typeof useOtDocumentOrder>);

    renderWithProviders(<DocumentOrderDragDrop otId={OT_ID} />);

    const doc3 = screen.getByLabelText(/documento 3: rtm/i);
    const doc1 = screen.getByLabelText(/documento 1: tarjeta de propiedad/i);

    fireEvent.dragStart(doc3);
    fireEvent.dragOver(doc1);
    fireEvent.drop(doc1);

    expect(mutateAsync).toHaveBeenCalledWith({
      procedureTypeId: PROCEDURE_TYPE_ID,
      orderedDocumentTypeIds: [
        "00000000-0000-0000-0000-000000000013",
        "00000000-0000-0000-0000-000000000011",
        "00000000-0000-0000-0000-000000000012",
        "00000000-0000-0000-0000-000000000014",
      ],
    });

    expect(await screen.findByText(/prelación actualizada/i)).toBeInTheDocument();
  });
});
