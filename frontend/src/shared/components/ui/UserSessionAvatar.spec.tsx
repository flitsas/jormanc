// @vitest-environment jsdom

import { createElement } from "react";
import { cleanup, render, screen, fireEvent, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { UserSessionAvatar } from "./UserSessionAvatar.js";
import { TOKEN_KEY, USER_KEY } from "../../api/client.js";

const mockPost = vi.fn();
const mockNavigate = vi.fn();

vi.mock("../../../features/auth/api/auth.api.js", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../../features/auth/api/auth.api.js")>();
  return {
    ...actual,
    useLogout: () => async () => {
      await mockPost("/auth/logout");
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      mockNavigate("/login", { replace: true });
    },
  };
});

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

function renderAvatar() {
  const queryClient = new QueryClient();
  return render(
    createElement(
      QueryClientProvider,
      { client: queryClient },
      createElement(MemoryRouter, null, createElement(UserSessionAvatar)),
    ),
  );
}

describe("UserSessionAvatar", () => {
  beforeEach(() => {
    mockPost.mockReset();
    mockNavigate.mockReset();
    localStorage.setItem(TOKEN_KEY, "test-token");
    localStorage.setItem(
      USER_KEY,
      JSON.stringify({
        id: "550e8400-e29b-41d4-a716-446655440000",
        email: "admin@acme.com",
        name: "Admin FLIT",
        roles: ["admin"],
        permissions: [],
        tenantId: "550e8400-e29b-41d4-a716-446655440001",
        tenantName: "Acme Corp",
      }),
    );
  });

  afterEach(() => {
    cleanup();
    localStorage.clear();
  });

  it("muestra menú de usuario y opción cerrar sesión", () => {
    renderAvatar();

    fireEvent.click(screen.getByRole("button", { name: /Menú de usuario/i }));

    expect(screen.getByRole("menu", { name: "Opciones de sesión" })).toBeTruthy();
    expect(screen.getByText("Admin FLIT")).toBeTruthy();
    expect(screen.getByRole("menuitem", { name: "Cerrar sesión" })).toBeTruthy();
  });

  it("ejecuta logout al pulsar cerrar sesión", async () => {
    renderAvatar();

    fireEvent.click(screen.getByRole("button", { name: /Menú de usuario/i }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Cerrar sesión" }));

    await waitFor(() => {
      expect(mockPost).toHaveBeenCalledWith("/auth/logout");
      expect(mockNavigate).toHaveBeenCalledWith("/login", { replace: true });
    });
  });
});
