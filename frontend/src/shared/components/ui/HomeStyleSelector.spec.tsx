// @vitest-environment jsdom

import { createElement, type ReactElement } from "react";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HomeStyleProvider } from "../../hooks/use-home-style.js";
import { ThemeProvider } from "../../hooks/use-theme.js";
import { HOME_STYLE_DATA_ATTR } from "../../lib/home-style.js";
import { HomeStyleSelector } from "./HomeStyleSelector.js";

function renderSelector(ui: ReactElement = createElement(HomeStyleSelector)) {
  return render(createElement(ThemeProvider, null, createElement(HomeStyleProvider, null, ui)));
}

describe("HomeStyleSelector", () => {
  beforeEach(() => {
    localStorage.clear();
    delete document.documentElement.dataset[HOME_STYLE_DATA_ATTR];
  });

  afterEach(() => {
    cleanup();
  });

  it("AC1 — muestra al menos 3 estilos con nombre, descripción y vista previa", () => {
    renderSelector();

    expect(screen.getByRole("radiogroup")).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: /Clásico FLIT/i })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: /Corporativo/i })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: /Alto contraste/i })).toBeInTheDocument();
  });

  it("selecciona un estilo y actualiza data-home-style", async () => {
    const user = userEvent.setup();
    renderSelector();

    await user.click(screen.getByRole("radio", { name: /Corporativo/i }));

    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("corporate");
    expect(screen.getByRole("radio", { name: /Corporativo/i })).toHaveAttribute(
      "aria-checked",
      "true",
    );
  });

  it("AC2 — estado vacío cuando no hay presets", () => {
    renderSelector(createElement(HomeStyleSelector, { presets: [], status: "ready" }));

    expect(screen.getByText(/Sin estilos disponibles/i)).toBeInTheDocument();
    expect(screen.queryByRole("radio")).not.toBeInTheDocument();
  });

  it("AC2 — estado error con mensaje y reintentar", async () => {
    const onRetry = vi.fn();
    renderSelector(
      createElement(HomeStyleSelector, {
        status: "error",
        errorMessage: "Fallo de red",
        onRetry,
      }),
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText(/Fallo de red/i)).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /Reintentar/i }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("estado cargando sin opciones interactivas", () => {
    renderSelector(createElement(HomeStyleSelector, { status: "loading" }));

    expect(screen.getByRole("status")).toHaveTextContent(/Cargando estilos/i);
    expect(screen.queryByRole("radio")).not.toBeInTheDocument();
  });
});
