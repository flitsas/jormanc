import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { ExportExcelButton } from "./ExportExcelButton.js";

const mockMutateAsync = vi.fn();
const mockUseExportDashboardExcel = vi.fn();

vi.mock("../api/dashboard.api.js", () => ({
  useExportDashboardExcel: () => mockUseExportDashboardExcel(),
}));

const EXPORT_PARAMS = {
  from: "2026-01-01T00:00:00.000Z",
  to: "2026-06-30T23:59:59.999Z",
};

function renderButton(isPending = false) {
  mockUseExportDashboardExcel.mockReturnValue({
    mutateAsync: mockMutateAsync,
    isPending,
  });
  const qc = new QueryClient();
  return render(
    <QueryClientProvider client={qc}>
      <ExportExcelButton params={EXPORT_PARAMS} />
    </QueryClientProvider>,
  );
}

describe("ExportExcelButton — AC3", () => {
  beforeEach(() => {
    mockMutateAsync.mockReset();
    mockUseExportDashboardExcel.mockReset();
  });

  it("muestra spinner y queda deshabilitado durante la descarga", () => {
    renderButton(true);
    const button = screen.getByRole("button", { name: /exportar excel/i });
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("aria-busy", "true");
    expect(button.querySelector(".pi-spinner")).toBeTruthy();
  });

  it("dispara export al hacer clic", async () => {
    mockMutateAsync.mockResolvedValue(undefined);
    renderButton(false);
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /exportar excel/i }));

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith(EXPORT_PARAMS);
    });
  });

  it("muestra toast de error ante fallo HTTP", async () => {
    mockMutateAsync.mockRejectedValue(new Error("500"));
    renderButton(false);
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /exportar excel/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Error al exportar. Intenta nuevamente.",
    );
  });
});
