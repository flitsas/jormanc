import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { ProceduresGrid } from "./ProceduresGrid.js";
import type { ProcedureListItem } from "../api/procedures.schemas.js";

const MOCK_ITEMS: ProcedureListItem[] = [
  {
    id: "00000000-0000-0000-0000-000000000001",
    compositeId: "TRASP-02_EVE-8841",
    status: "submitted",
    procedureTypeId: "00000000-0000-0000-0000-000000000010",
    companyId: "00000000-0000-0000-0000-000000000020",
    createdAt: "2026-01-15T10:00:00Z",
  },
];

const defaultProps = {
  items: [] as ProcedureListItem[],
  total: 0,
  page: 1,
  pageSize: 20,
  filters: { status: "", fechaFrom: "" },
  isLoading: false,
  error: null,
  onFiltersChange: vi.fn(),
  onPageChange: vi.fn(),
  onRetry: vi.fn(),
};

function renderGrid(overrides: Partial<typeof defaultProps> = {}) {
  return render(
    <MemoryRouter>
      <ProceduresGrid {...defaultProps} {...overrides} />
    </MemoryRouter>,
  );
}

describe("ProceduresGrid — AC1", () => {
  it("muestra composite_id y filtros status/fecha_from", () => {
    renderGrid({ items: MOCK_ITEMS, total: 1 });
    expect(screen.getByText("TRASP-02_EVE-8841")).toBeInTheDocument();
    expect(screen.getByLabelText(/filtrar por estado/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/filtrar desde fecha/i)).toBeInTheDocument();
  });

  it("paginación server-side muestra total real", () => {
    renderGrid({ items: MOCK_ITEMS, total: 45, page: 2, pageSize: 20 });
    expect(screen.getByText(/45 trámites/i)).toBeInTheDocument();
    expect(screen.getByText(/página 2 de 3/i)).toBeInTheDocument();
  });

  it("propaga cambio de filtro status", async () => {
    const onFiltersChange = vi.fn();
    const user = userEvent.setup();
    renderGrid({ onFiltersChange });
    await user.selectOptions(screen.getByLabelText(/filtrar por estado/i), "submitted");
    expect(onFiltersChange).toHaveBeenCalledWith({ status: "submitted", fechaFrom: "" });
  });
});
