import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { OtModeSwitch } from "./OtModeSwitch.js";
import type { OtOrganism } from "../api/ot-admin.schemas.js";

const BASE: OtOrganism = {
  id: "00000000-0000-0000-0000-000000000001",
  tenantId: "00000000-0000-0000-0000-000000000099",
  slug: "ot-bogota",
  name: "Secretaría Bogotá",
  mode: "dashboard",
  quipuxEnabled: false,
  quipuxConfig: null,
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
};

const mutateAsync = vi.fn();

vi.mock("../api/ot-admin.api.js", () => ({
  useUpdateOtMode: () => ({
    mutateAsync,
    isPending: false,
  }),
}));

function renderWithProviders(ui: React.ReactElement) {
  const qc = new QueryClient();
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

describe("OtModeSwitch", () => {
  it("shows dashboard mode by default", () => {
    renderWithProviders(<OtModeSwitch organism={BASE} />);
    expect(screen.getByRole("switch")).toHaveAttribute("aria-checked", "false");
    expect(screen.getByRole("switch")).toHaveAttribute("aria-label", "Cambiar a Modo QX");
  });

  it("shows confirmation before switching to QX mode", async () => {
    const user = userEvent.setup();
    renderWithProviders(<OtModeSwitch organism={BASE} />);
    await user.click(screen.getByRole("switch"));
    expect(screen.getByText(/activar modo qx/i)).toBeInTheDocument();
  });

  it("calls update mutation when QX mode is confirmed", async () => {
    mutateAsync.mockResolvedValue({ ...BASE, mode: "qx", quipuxEnabled: true });
    const onModeChanged = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<OtModeSwitch organism={BASE} onModeChanged={onModeChanged} />);
    await user.click(screen.getByRole("switch"));
    await user.click(screen.getByRole("button", { name: /confirmar modo qx/i }));
    expect(mutateAsync).toHaveBeenCalledWith("qx");
  });

  it("switches back to dashboard from QX without confirmation", async () => {
    mutateAsync.mockResolvedValue({ ...BASE, mode: "dashboard", quipuxEnabled: false });
    const user = userEvent.setup();
    renderWithProviders(
      <OtModeSwitch organism={{ ...BASE, mode: "qx", quipuxEnabled: true }} />,
    );
    await user.click(screen.getByRole("switch"));
    expect(mutateAsync).toHaveBeenCalledWith("dashboard");
  });
});
