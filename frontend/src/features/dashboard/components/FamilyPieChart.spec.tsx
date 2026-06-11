import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { FamilyPieChart } from "./FamilyPieChart.js";
import type { FamilySummary } from "../api/dashboard.schemas.js";

const MOCK_FAMILY: FamilySummary[] = [
  {
    family: "matricula_inicial",
    count: 120,
    pct: 23.76,
    byStatus: { draft: 10, submitted: 80, approved: 25, rejected: 5 },
  },
  {
    family: "traspasos",
    count: 340,
    pct: 67.33,
    byStatus: { draft: 20, submitted: 200, approved: 100, rejected: 20 },
  },
  {
    family: "otros",
    count: 45,
    pct: 8.91,
    byStatus: { draft: 5, submitted: 30, approved: 8, rejected: 2 },
  },
];

const defaultProps = {
  byFamily: MOCK_FAMILY,
  total: 505,
  isLoading: false,
  error: null,
  selectedFamily: null as const,
  onFamilySelect: vi.fn(),
  onRetry: vi.fn(),
};

describe("FamilyPieChart — AC1", () => {
  it("muestra 3 segmentos con nombre, conteo y porcentaje en la leyenda", () => {
    render(<FamilyPieChart {...defaultProps} />);
    const legend = screen.getByRole("list", { name: /leyenda del gráfico/i });
    expect(within(legend).getByRole("button", { name: /matrículas/i })).toHaveTextContent(
      /120 \(23\.76%\)/,
    );
    expect(within(legend).getByRole("button", { name: /traspasos/i })).toHaveTextContent(
      /340 \(67\.33%\)/,
    );
    expect(within(legend).getByRole("button", { name: /otros/i })).toHaveTextContent(
      /45 \(8\.91%\)/,
    );
  });

  it("muestra segmentos SVG con colores distintos por familia", () => {
    render(<FamilyPieChart {...defaultProps} />);
    const segments = document.querySelectorAll('[role="button"][aria-label*="trámites"]');
    expect(segments.length).toBe(3);
    const fills = Array.from(segments).map((el) => el.getAttribute("fill"));
    expect(new Set(fills).size).toBe(3);
    expect(fills).toContain("#4FD4CC");
    expect(fills).toContain("#4F74C9");
    expect(fills).toContain("#70CF3A");
  });

  it("muestra tooltip al pasar el mouse sobre un segmento", async () => {
    const user = userEvent.setup();
    render(<FamilyPieChart {...defaultProps} />);
    const traspasosSegment = screen.getByRole("button", {
      name: /traspasos: 340 trámites/i,
    });
    await user.hover(traspasosSegment);
    expect(screen.getByRole("tooltip")).toHaveTextContent(/Traspasos: 340 \(67\.33%\)/);
  });

  it("muestra skeleton mientras carga", () => {
    render(<FamilyPieChart {...defaultProps} isLoading={true} />);
    expect(screen.getByLabelText(/cargando gráfico circular/i)).toHaveAttribute(
      "aria-busy",
      "true",
    );
  });

  it("muestra estado vacío sin trámites en el período", () => {
    render(<FamilyPieChart {...defaultProps} byFamily={[]} total={0} />);
    expect(screen.getByText(/sin trámites en el período/i)).toBeInTheDocument();
  });
});
