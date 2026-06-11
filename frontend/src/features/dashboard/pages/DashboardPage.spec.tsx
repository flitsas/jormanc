import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { DashboardPage } from "./DashboardPage.js";
import type { DashboardSummary, DashboardProceduresPage, DashboardTopUsers } from "../api/dashboard.schemas.js";

const mockUseDashboardSummary = vi.fn();
const mockUseDashboardProcedures = vi.fn();
const mockUseDashboardTopUsers = vi.fn();
const mockUseExportDashboardExcel = vi.fn();
const mockUseExportDashboardPdf = vi.fn();

vi.mock("../api/dashboard.api.js", () => ({
  useDashboardSummary: (...args: unknown[]) => mockUseDashboardSummary(...args),
  useDashboardProcedures: (...args: unknown[]) => mockUseDashboardProcedures(...args),
  useDashboardTopUsers: (...args: unknown[]) => mockUseDashboardTopUsers(...args),
  useExportDashboardExcel: () => mockUseExportDashboardExcel(),
  useExportDashboardPdf: () => mockUseExportDashboardPdf(),
}));

const SUMMARY: DashboardSummary = {
  period: { from: "2026-01-01T00:00:00.000Z", to: "2026-06-30T23:59:59.999Z" },
  summary: {
    total: 505,
    byFamily: [
      {
        family: "matricula_inicial",
        count: 120,
        pct: 23.76,
        byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
      },
      {
        family: "traspasos",
        count: 340,
        pct: 67.33,
        byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
      },
      {
        family: "otros",
        count: 45,
        pct: 8.91,
        byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
      },
    ],
    byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
  },
};

const TOP_USERS: DashboardTopUsers = {
  data: [
    {
      userId: "11111111-1111-1111-1111-111111111111",
      fullName: "María Pérez",
      count: 120,
      pctOfTotal: 45,
    },
    {
      userId: "22222222-2222-2222-2222-222222222222",
      fullName: "Carlos López",
      count: 80,
      pctOfTotal: 30,
    },
  ],
};

const PROCEDURES: DashboardProceduresPage = {
  data: [
    {
      id: "TRASP-02_EVE-8841",
      submittedAt: "2026-01-15T10:00:00Z",
      status: "submitted",
      plate: "XYZ999",
      ownerName: "María López",
      approvedAt: null,
      updatedAt: "2026-01-16T08:00:00Z",
    },
  ],
  total: 1,
  page: 1,
  pageSize: 20,
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <DashboardPage />
    </QueryClientProvider>,
  );
}

describe("DashboardPage — AC2 y AC3 integración", () => {
  beforeEach(() => {
    mockUseDashboardSummary.mockReturnValue({
      data: SUMMARY,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    mockUseDashboardTopUsers.mockReturnValue({
      data: TOP_USERS,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    mockUseDashboardProcedures.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    mockUseExportDashboardExcel.mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
    });
    mockUseExportDashboardPdf.mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
    });
  });

  it("AC2 — multiselección de radicadores propaga user_ids a los hooks", async () => {
    mockUseDashboardProcedures.mockImplementation((_params, enabled) => ({
      data: enabled ? PROCEDURES : undefined,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    }));

    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByLabelText("María Pérez"));
    await user.click(screen.getByLabelText("Carlos López"));

    expect(screen.getByText("2 radicadores seleccionados")).toBeInTheDocument();

    const summaryCall = mockUseDashboardSummary.mock.calls.at(-1)?.[0];
    expect(summaryCall?.userIds).toEqual([
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
    ]);

    const topUsersCall = mockUseDashboardTopUsers.mock.calls.at(-1)?.[0];
    expect(topUsersCall?.userIds).toEqual([
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
    ]);

    await user.click(screen.getByRole("button", { name: /traspasos: 340 trámites/i }));

    const proceduresCall = mockUseDashboardProcedures.mock.calls.at(-1)?.[0];
    expect(proceduresCall?.userIds).toEqual([
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
    ]);
    expect(proceduresCall?.from).toBeTruthy();
    expect(proceduresCall?.to).toBeTruthy();
  });

  it("AC2 — clic en Traspasos habilita consulta de detalle y muestra tabla", async () => {
    mockUseDashboardProcedures.mockImplementation((_params, enabled) => ({
      data: enabled ? PROCEDURES : undefined,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    }));

    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: /traspasos: 340 trámites/i }));

    expect(mockUseDashboardProcedures).toHaveBeenCalled();
    expect(screen.getByText("TRASP-02_EVE-8841")).toBeInTheDocument();
  });

  it("AC3 — cambio de rango dispara nueva consulta de summary y top users", () => {
    renderPage();

    const callsBeforeSummary = mockUseDashboardSummary.mock.calls.length;
    const callsBeforeTop = mockUseDashboardTopUsers.mock.calls.length;
    const fromInput = screen.getByLabelText(/fecha inicial del período/i);
    fireEvent.change(fromInput, { target: { value: "2026-01-01" } });

    expect(mockUseDashboardSummary.mock.calls.length).toBeGreaterThan(callsBeforeSummary);
    expect(mockUseDashboardTopUsers.mock.calls.length).toBeGreaterThan(callsBeforeTop);
  });

  it("AC1 — TopUsersCard visible con ranking de radicadores", () => {
    renderPage();
    const card = screen.getByRole("region", { name: /top 5 radicadores/i });
    expect(within(card).getByRole("list", { name: /ranking de radicadores/i })).toBeInTheDocument();
    expect(within(card).getByText("120 (45%)")).toBeInTheDocument();
  });

  it("AC3 — botones de export visibles en el dashboard", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /exportar excel/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /exportar pdf/i })).toBeInTheDocument();
  });
});
