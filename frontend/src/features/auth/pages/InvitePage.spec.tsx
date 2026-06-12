import { render, screen } from "@testing-library/react";
import { describe, beforeEach, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { InvitePage } from "./InvitePage.js";

vi.mock("../api/auth.api.js", () => ({
  useValidateInvitation: vi.fn(),
  useAcceptInvitation: vi.fn(),
}));

import { useValidateInvitation, useAcceptInvitation } from "../api/auth.api.js";

const mockUseValidate = useValidateInvitation as ReturnType<typeof vi.fn>;
const mockUseAccept = useAcceptInvitation as ReturnType<typeof vi.fn>;

function renderPage(token = "test-token") {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/invite/${token}`]}>
        <Routes>
          <Route path="/invite/:token" element={<InvitePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("InvitePage", () => {
  beforeEach(() => {
    mockUseAccept.mockReturnValue({ mutateAsync: vi.fn(), isPending: false });
  });

  it("shows loading skeleton while validating token", () => {
    mockUseValidate.mockReturnValue({ isLoading: true, isError: false, data: undefined });
    renderPage();

    expect(screen.getByLabelText(/validando invitación/i)).toBeInTheDocument();
  });

  it("shows expired message when token is invalid", () => {
    mockUseValidate.mockReturnValue({
      isLoading: false,
      isError: true,
      data: undefined,
    });
    renderPage("invalid-token");

    expect(screen.getByText(/invitación expirada o inválida/i)).toBeInTheDocument();
    expect(screen.queryByRole("form")).not.toBeInTheDocument();
  });

  it("shows expired message when data is null (expired token)", () => {
    mockUseValidate.mockReturnValue({
      isLoading: false,
      isError: false,
      data: null,
    });
    renderPage("expired-token");

    expect(screen.getByText(/invitación expirada o inválida/i)).toBeInTheDocument();
  });

  it("renders invitation form when token is valid", () => {
    mockUseValidate.mockReturnValue({
      isLoading: false,
      isError: false,
      data: {
        email: "nuevo@empresa.com",
        tenantName: "Empresa FLIT",
        roles: ["admin"],
      },
    });
    renderPage("valid-token");

    expect(screen.getByRole("form", { name: /activación de cuenta/i })).toBeInTheDocument();
    expect(screen.getByText(/nuevo@empresa.com/)).toBeInTheDocument();
    expect(screen.getByText(/Empresa FLIT/)).toBeInTheDocument();
  });
});
