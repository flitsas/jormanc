import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { QuipuxConfigForm } from "./QuipuxConfigForm.js";
import type { OtOrganism } from "../api/ot-admin.schemas.js";

const ORGANISM: OtOrganism = {
  id: "00000000-0000-0000-0000-000000000001",
  tenantId: "00000000-0000-0000-0000-000000000099",
  slug: "ot-bogota",
  name: "Secretaría Bogotá",
  mode: "qx",
  quipuxEnabled: true,
  quipuxConfig: '{"endpoint":"https://qx.example.com","webhook_token_hash":"abc"}',
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
};

const mutateAsync = vi.fn();

vi.mock("../api/ot-admin.api.js", () => ({
  useUpdateQuipuxConfig: () => ({
    mutateAsync,
    isPending: false,
  }),
}));

function renderWithProviders(ui: React.ReactElement) {
  const qc = new QueryClient();
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

describe("QuipuxConfigForm", () => {
  it("pre-fills endpoint from organism config", () => {
    renderWithProviders(<QuipuxConfigForm organism={ORGANISM} />);
    expect(screen.getByLabelText(/endpoint quipux/i)).toHaveValue("https://qx.example.com");
    expect(screen.getByText(/ya existe un token configurado/i)).toBeInTheDocument();
  });

  it("submits endpoint update", async () => {
    mutateAsync.mockResolvedValue(ORGANISM);
    const user = userEvent.setup();
    renderWithProviders(<QuipuxConfigForm organism={ORGANISM} />);

    await user.clear(screen.getByLabelText(/endpoint quipux/i));
    await user.type(screen.getByLabelText(/endpoint quipux/i), "https://qx-new.example.com");
    await user.click(screen.getByRole("button", { name: /guardar configuración/i }));

    expect(mutateAsync).toHaveBeenCalledWith({
      endpoint: "https://qx-new.example.com",
      webhookToken: undefined,
    });
  });
});
