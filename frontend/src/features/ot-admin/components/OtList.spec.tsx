import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { OtList } from "./OtList.js";
import type { OtOrganism } from "../api/ot-admin.schemas.js";

const MOCK: OtOrganism[] = [
  {
    id: "00000000-0000-0000-0000-000000000001",
    tenantId: "00000000-0000-0000-0000-000000000099",
    slug: "ot-bogota",
    name: "Secretaría Bogotá",
    mode: "dashboard",
    quipuxEnabled: false,
    quipuxConfig: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
  },
  {
    id: "00000000-0000-0000-0000-000000000002",
    tenantId: "00000000-0000-0000-0000-000000000099",
    slug: "ot-medellin",
    name: "Secretaría Medellín",
    mode: "qx",
    quipuxEnabled: true,
    quipuxConfig: '{"endpoint":"https://qx.example.com"}',
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
  },
];

describe("OtList", () => {
  const defaultProps = {
    organisms: [],
    isLoading: false,
    error: null,
    onRetry: vi.fn(),
    onSelect: vi.fn(),
    onDelete: vi.fn(),
  };

  it("shows loading skeleton (aria-busy)", () => {
    render(<OtList {...defaultProps} isLoading={true} />);
    expect(screen.getByLabelText(/cargando organismos de tránsito/i)).toHaveAttribute(
      "aria-busy",
      "true",
    );
  });

  it("shows empty state", () => {
    render(<OtList {...defaultProps} />);
    expect(screen.getByText(/no hay ots configurados/i)).toBeInTheDocument();
  });

  it("shows error state with retry", async () => {
    const onRetry = vi.fn();
    const user = userEvent.setup();
    render(<OtList {...defaultProps} error={new Error("Error de red")} onRetry={onRetry} />);
    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(onRetry).toHaveBeenCalled();
  });

  it("renders OT rows with mode badges and actions", async () => {
    const onSelect = vi.fn();
    const user = userEvent.setup();
    render(<OtList {...defaultProps} organisms={MOCK} onSelect={onSelect} />);
    expect(screen.getByText("Secretaría Bogotá")).toBeInTheDocument();
    expect(screen.getByText("Modo QX")).toBeInTheDocument();
    await user.click(screen.getAllByRole("button", { name: /configurar/i })[0]!);
    expect(onSelect).toHaveBeenCalledWith(MOCK[0]);
  });
});
