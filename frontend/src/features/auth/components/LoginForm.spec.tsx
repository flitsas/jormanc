import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { LoginForm } from "./LoginForm.js";

vi.mock("../api/auth.api.js", () => ({
  useLogin: vi.fn(),
}));

import { useLogin } from "../api/auth.api.js";

const mockUseLogin = useLogin as ReturnType<typeof vi.fn>;

function renderWithProviders(ui: React.ReactElement, { initialEntries = ["/"] } = {}) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("LoginForm", () => {
  beforeEach(() => {
    mockUseLogin.mockReturnValue({ mutateAsync: vi.fn(), isPending: false });
  });

  it("renders all fields and submit button", () => {
    renderWithProviders(<LoginForm />);

    expect(screen.getByLabelText(/correo electrónico/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/contraseña/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/organización/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /ingresar/i })).toBeInTheDocument();
  });

  it("shows validation errors when submitted empty", async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginForm />);

    await user.click(screen.getByRole("button", { name: /ingresar/i }));

    await waitFor(() => {
      expect(screen.getByText(/email inválido/i)).toBeInTheDocument();
    });
  });

  it("shows session revoked banner when reason=session_revoked in URL", () => {
    renderWithProviders(<LoginForm />, { initialEntries: ["/login?reason=session_revoked"] });

    expect(screen.getByRole("alert")).toHaveTextContent(/sesión fue cerrada/i);
  });

  it("shows spinner when isPending", () => {
    mockUseLogin.mockReturnValue({ mutateAsync: vi.fn(), isPending: true });
    renderWithProviders(<LoginForm />);

    expect(screen.getByRole("button", { name: /ingresando/i })).toBeInTheDocument();
    expect(screen.getByRole("button")).toBeDisabled();
  });

  it("calls login mutateAsync with valid credentials", async () => {
    const user = userEvent.setup();
    const mutateAsync = vi.fn().mockResolvedValue({});
    mockUseLogin.mockReturnValue({ mutateAsync, isPending: false });

    renderWithProviders(<LoginForm />);

    await user.type(screen.getByLabelText(/correo electrónico/i), "test@flit.co");
    await user.type(screen.getByLabelText(/contraseña/i), "secreto");
    await user.type(screen.getByLabelText(/organización/i), "flit");
    await user.click(screen.getByRole("button", { name: /ingresar/i }));

    await waitFor(() => {
      expect(mutateAsync).toHaveBeenCalledWith({
        email: "test@flit.co",
        password: "secreto",
        tenant_slug: "flit",
      });
    });
  });

  it("shows API error message when login fails", async () => {
    const user = userEvent.setup();
    const mutateAsync = vi.fn().mockRejectedValue(new Error("Credenciales inválidas"));
    mockUseLogin.mockReturnValue({ mutateAsync, isPending: false });

    renderWithProviders(<LoginForm />);

    await user.type(screen.getByLabelText(/correo electrónico/i), "bad@flit.co");
    await user.type(screen.getByLabelText(/contraseña/i), "wrong");
    await user.type(screen.getByLabelText(/organización/i), "flit");
    await user.click(screen.getByRole("button", { name: /ingresar/i }));

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent(/credenciales inválidas/i);
    });
  });
});
