import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { UsersTable } from "./UsersTable.js";
import type { User } from "../api/users.schemas.js";

const MOCK_USERS: User[] = [
  {
    id: "00000000-0000-0000-0000-000000000001",
    email: "ana@flit.co",
    full_name: "Ana García",
    status: "active",
    must_reset_pwd: false,
    last_login_at: "2026-06-10T10:00:00Z",
    created_at: "2026-01-01T00:00:00Z",
    roles: [{ id: "r1", slug: "admin", name: "Admin", is_system: true }],
  },
  {
    id: "00000000-0000-0000-0000-000000000002",
    email: "carlos@flit.co",
    full_name: "Carlos López",
    status: "pending",
    must_reset_pwd: false,
    last_login_at: null,
    created_at: "2026-01-02T00:00:00Z",
    roles: [],
  },
];

describe("UsersTable", () => {
  const defaultProps = {
    users: [],
    isLoading: false,
    error: null,
    onRetry: vi.fn(),
    onManageRoles: vi.fn(),
  };

  it("shows loading skeleton rows (aria-busy)", () => {
    render(<UsersTable {...defaultProps} isLoading={true} />);

    const busy = screen.getByRole("table").closest("[aria-busy='true']");
    expect(busy).toBeInTheDocument();
  });

  it("shows error state with retry button", () => {
    const onRetry = vi.fn();
    render(
      <UsersTable
        {...defaultProps}
        error={new Error("Servidor no disponible")}
        onRetry={onRetry}
      />,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText(/servidor no disponible/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /reintentar/i })).toBeInTheDocument();
  });

  it("calls onRetry when retry button is clicked", async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    render(
      <UsersTable
        {...defaultProps}
        error={new Error("Error")}
        onRetry={onRetry}
      />,
    );

    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("shows empty state when no users", () => {
    render(<UsersTable {...defaultProps} users={[]} />);

    expect(screen.getByText(/no hay usuarios en este tenant/i)).toBeInTheDocument();
  });

  it("renders user rows when data is available", () => {
    render(<UsersTable {...defaultProps} users={MOCK_USERS} />);

    expect(screen.getByRole("table", { name: /lista de usuarios/i })).toBeInTheDocument();
    expect(screen.getByText("Ana García")).toBeInTheDocument();
    expect(screen.getByText("Carlos López")).toBeInTheDocument();
    expect(screen.getByText("Activo")).toBeInTheDocument();
    expect(screen.getByText("Pendiente")).toBeInTheDocument();
  });

  it("calls onManageRoles with correct user when Roles button is clicked", async () => {
    const user = userEvent.setup();
    const onManageRoles = vi.fn();
    render(<UsersTable {...defaultProps} users={MOCK_USERS} onManageRoles={onManageRoles} />);

    await user.click(screen.getByRole("button", { name: /gestionar roles de ana garcía/i }));
    expect(onManageRoles).toHaveBeenCalledWith(MOCK_USERS[0]);
  });

  it("shows 'Nunca' when last_login_at is null", () => {
    render(<UsersTable {...defaultProps} users={MOCK_USERS} />);

    expect(screen.getByText("Nunca")).toBeInTheDocument();
  });

  it("shows 'Sin roles' italic when user has no roles", () => {
    render(<UsersTable {...defaultProps} users={MOCK_USERS} />);

    expect(screen.getByText("Sin roles")).toBeInTheDocument();
  });
});
