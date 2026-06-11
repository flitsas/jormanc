import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { DocumentStatusPanel } from "./DocumentStatusPanel.js";

const mockUseProcedureDocuments = vi.fn();
const mockUseConsolidateDocuments = vi.fn();

vi.mock("../api/documents.api.js", () => ({
  useProcedureDocuments: (...args: unknown[]) => mockUseProcedureDocuments(...args),
  useConsolidateDocuments: (...args: unknown[]) => mockUseConsolidateDocuments(...args),
}));

function renderPanel() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <DocumentStatusPanel procedureId="00000000-0000-0000-0000-000000000099" />
    </QueryClientProvider>,
  );
}

describe("DocumentStatusPanel — AC1 panel y 4 estados UI", () => {
  it("muestra skeleton mientras carga", () => {
    mockUseProcedureDocuments.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch: vi.fn(),
    });
    mockUseConsolidateDocuments.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      error: null,
    });

    renderPanel();
    expect(screen.getByLabelText(/cargando estado documental/i)).toBeInTheDocument();
  });

  it("muestra error con reintentar", () => {
    mockUseProcedureDocuments.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error("Servicio no disponible"),
      refetch: vi.fn(),
    });
    mockUseConsolidateDocuments.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      error: null,
    });

    renderPanel();
    expect(screen.getByRole("alert")).toHaveTextContent(/no se pudo cargar/i);
    expect(screen.getByRole("button", { name: /reintentar/i })).toBeInTheDocument();
  });

  it("lista documentos y paquete consolidado cuando hay datos", () => {
    mockUseProcedureDocuments.mockReturnValue({
      data: {
        procedureId: "00000000-0000-0000-0000-000000000099",
        documents: [
          {
            id: "00000000-0000-0000-0000-000000000001",
            documentType: {
              id: "00000000-0000-0000-0000-0000000000d1",
              name: "Formato RUNT",
              loadType: "generacion",
            },
            origin: "generated",
            status: "ready",
            templateVersion: 1,
            fileRef: "x.pdf",
            generatedAt: "2026-06-01T10:00:00Z",
            uploadedBy: null,
            isRequired: true,
            orderIndex: 0,
          },
        ],
        consolidatedPackages: [
          {
            version: 1,
            mergedFileRef: "merged.pdf",
            createdAt: "2026-06-02T12:00:00Z",
            downloadFilename: "TRAMITE_test.pdf",
            docCount: 1,
          },
        ],
      },
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    mockUseConsolidateDocuments.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      error: null,
    });

    renderPanel();
    expect(screen.getByRole("heading", { name: /estado documental/i })).toBeInTheDocument();
    expect(screen.getByText("Formato RUNT")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /descargar pdf/i })).toBeInTheDocument();
  });

  it("muestra vacío cuando no hay documentos configurados", () => {
    mockUseProcedureDocuments.mockReturnValue({
      data: {
        procedureId: "00000000-0000-0000-0000-000000000099",
        documents: [],
        consolidatedPackages: [],
      },
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    mockUseConsolidateDocuments.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      error: null,
    });

    renderPanel();
    expect(screen.getByText(/sin documentos configurados/i)).toBeInTheDocument();
  });
});
