import { render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { TopUsersCard } from "./TopUsersCard.js";
import type { TopUser } from "../api/dashboard.schemas.js";

const TOP_USERS: TopUser[] = [
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
  {
    userId: "33333333-3333-3333-3333-333333333333",
    fullName: "Ana Ruiz",
    count: 40,
    pctOfTotal: 15,
  },
  {
    userId: "44444444-4444-4444-4444-444444444444",
    fullName: "Luis Gómez",
    count: 20,
    pctOfTotal: 7.5,
  },
  {
    userId: "55555555-5555-5555-5555-555555555555",
    fullName: "Sofía Díaz",
    count: 6,
    pctOfTotal: 2.5,
  },
];

const defaultProps = {
  users: TOP_USERS,
  selectableUsers: TOP_USERS,
  selectedUserIds: [] as string[],
  isLoading: false,
  error: null,
  onSelectedUserIdsChange: vi.fn(),
  onRetry: vi.fn(),
};

describe("TopUsersCard — AC1", () => {
  it("muestra hasta 5 usuarios con nombre, conteo y progress bar", () => {
    render(<TopUsersCard {...defaultProps} />);

    const ranking = screen.getByRole("list", { name: /ranking de radicadores/i });
    expect(within(ranking).getByText("María Pérez")).toBeInTheDocument();
    expect(within(ranking).getByText("120 (45%)")).toBeInTheDocument();
    expect(within(ranking).getByText("Carlos López")).toBeInTheDocument();
    expect(within(ranking).getByText("Sofía Díaz")).toBeInTheDocument();

    const progressBars = within(ranking).getAllByRole("progressbar");
    expect(progressBars).toHaveLength(5);
  });

  it("el primer usuario tiene la barra más larga", () => {
    render(<TopUsersCard {...defaultProps} />);

    const mariaBar = screen.getByTestId("progress-11111111-1111-1111-1111-111111111111");
    const carlosBar = screen.getByTestId("progress-22222222-2222-2222-2222-222222222222");

    expect(mariaBar).toHaveStyle({ width: "45%" });
    expect(carlosBar).toHaveStyle({ width: "30%" });
    expect(Number.parseFloat(mariaBar.style.width)).toBeGreaterThan(
      Number.parseFloat(carlosBar.style.width),
    );
    expect(mariaBar).toHaveAttribute("data-is-leader", "true");
  });

  it("muestra EmptyUserCard cuando un usuario no tiene trámites", () => {
    const usersWithEmpty: TopUser[] = [
      ...TOP_USERS.slice(0, 4),
      {
        userId: "66666666-6666-6666-6666-666666666666",
        fullName: "Pedro Vacío",
        count: 0,
        pctOfTotal: 0,
      },
    ];

    render(<TopUsersCard {...defaultProps} users={usersWithEmpty} selectableUsers={usersWithEmpty} />);

    expect(
      screen.getByText("Este usuario no ha radicado ningún trámite"),
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/pedro vacío no ha radicado trámites en el período/i)).toBeInTheDocument();
  });

  it("muestra skeleton mientras carga", () => {
    render(<TopUsersCard {...defaultProps} isLoading={true} users={[]} />);
    expect(screen.getByLabelText(/cargando top radicadores/i)).toHaveAttribute("aria-busy", "true");
  });

  it("muestra estado de error con reintentar", () => {
    const onRetry = vi.fn();
    render(
      <TopUsersCard
        {...defaultProps}
        users={[]}
        error={new Error("Fallo de red")}
        onRetry={onRetry}
      />,
    );
    expect(screen.getByRole("alert")).toHaveTextContent(/no se pudo cargar el ranking/i);
  });
});

describe("TopUsersCard — AC2 selector", () => {
  it("muestra badge con radicadores seleccionados", () => {
    render(
      <TopUsersCard
        {...defaultProps}
        selectedUserIds={[
          "11111111-1111-1111-1111-111111111111",
          "22222222-2222-2222-2222-222222222222",
        ]}
      />,
    );

    expect(screen.getByRole("status")).toHaveTextContent("2 radicadores seleccionados");
  });

  it("expone selector multiselección accesible por usuario", () => {
    render(<TopUsersCard {...defaultProps} />);
    const group = screen.getByRole("group", { name: /selector multiselección de radicadores/i });
    expect(within(group).getByLabelText("María Pérez")).toBeInTheDocument();
    expect(within(group).getByLabelText("Carlos López")).toBeInTheDocument();
  });
});
