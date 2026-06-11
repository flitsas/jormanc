import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CompaniesTable } from "./CompaniesTable.js";
import type { CompanyListItem } from "../api/companies.schemas.js";

const MOCK: CompanyListItem[] = [
  {
    id: "00000000-0000-0000-0000-000000000001",
    tenantId: "00000000-0000-0000-0000-000000000099",
    nit: "900123456",
    name: "Empresa ABC",
    status: "active",
    tenantSlug: "empresa-abc",
    createdAt: "2026-01-01T00:00:00Z",
  },
];

describe("CompaniesTable", () => {
  const defaultProps = {
    companies: [],
    isLoading: false,
    error: null,
    onRetry: vi.fn(),
    onEdit: vi.fn(),
  };

  it("shows loading skeleton (aria-busy)", () => {
    render(<CompaniesTable {...defaultProps} isLoading={true} />);
    expect(screen.getByLabelText(/cargando compañías/i)).toHaveAttribute("aria-busy", "true");
  });

  it("shows empty state", () => {
    render(<CompaniesTable {...defaultProps} />);
    expect(screen.getByText(/no hay compañías registradas/i)).toBeInTheDocument();
  });

  it("shows error state with retry", async () => {
    const onRetry = vi.fn();
    const user = userEvent.setup();
    render(
      <CompaniesTable {...defaultProps} error={new Error("Error de red")} onRetry={onRetry} />,
    );
    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(onRetry).toHaveBeenCalled();
  });

  it("renders company rows and edit action", async () => {
    const onEdit = vi.fn();
    const user = userEvent.setup();
    render(<CompaniesTable {...defaultProps} companies={MOCK} onEdit={onEdit} />);
    expect(screen.getByText("Empresa ABC")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /configurar/i }));
    expect(onEdit).toHaveBeenCalledWith(MOCK[0]);
  });
});
