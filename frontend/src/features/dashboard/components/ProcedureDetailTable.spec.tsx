import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ProcedureDetailTable } from "./ProcedureDetailTable.js";
import type { DashboardProcedureItem } from "../api/dashboard.schemas.js";

const MOCK_ITEMS: DashboardProcedureItem[] = [
  {
    id: "TRASP-02_EVE-8841",
    submittedAt: "2026-01-15T10:00:00Z",
    status: "submitted",
    plate: "ABC123",
    ownerName: "Juan Pérez",
    approvedAt: null,
    updatedAt: "2026-01-16T08:00:00Z",
  },
];

const defaultProps = {
  family: "traspasos" as const,
  items: MOCK_ITEMS,
  total: 1,
  page: 1,
  pageSize: 20,
  isLoading: false,
  error: null,
  onPageChange: vi.fn(),
  onRetry: vi.fn(),
};

describe("ProcedureDetailTable — AC2", () => {
  it("muestra columnas ID, fecha rad., estado, placa, propietario, aprobación y actualización", () => {
    render(<ProcedureDetailTable {...defaultProps} />);
    expect(screen.getByText("ID")).toBeInTheDocument();
    expect(screen.getByText("Fecha rad.")).toBeInTheDocument();
    expect(screen.getByText("Estado")).toBeInTheDocument();
    expect(screen.getByText("Placa")).toBeInTheDocument();
    expect(screen.getByText("Propietario")).toBeInTheDocument();
    expect(screen.getByText("F. aprobación")).toBeInTheDocument();
    expect(screen.getByText("Actualización")).toBeInTheDocument();
    expect(screen.getByText("TRASP-02_EVE-8841")).toBeInTheDocument();
    expect(screen.getByText("ABC123")).toBeInTheDocument();
    expect(screen.getByText("Juan Pérez")).toBeInTheDocument();
  });

  it("muestra panel lateral con título de familia Traspasos", () => {
    render(<ProcedureDetailTable {...defaultProps} />);
    expect(screen.getByLabelText(/detalle de trámites — traspasos/i)).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /detalle — traspasos/i })).toBeInTheDocument();
  });

  it("invita a seleccionar segmento cuando no hay familia", () => {
    render(<ProcedureDetailTable {...defaultProps} family={null} items={[]} total={0} />);
    expect(screen.getByText(/selecciona un segmento del gráfico/i)).toBeInTheDocument();
  });

  it("muestra skeleton mientras carga", () => {
    render(<ProcedureDetailTable {...defaultProps} isLoading={true} />);
    expect(screen.getByLabelText(/cargando detalle de trámites/i)).toHaveAttribute(
      "aria-busy",
      "true",
    );
  });

  it("muestra error con reintentar", async () => {
    const onRetry = vi.fn();
    const user = userEvent.setup();
    render(
      <ProcedureDetailTable
        {...defaultProps}
        error={new Error("Error de red")}
        items={[]}
        onRetry={onRetry}
      />,
    );
    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(onRetry).toHaveBeenCalled();
  });
});
