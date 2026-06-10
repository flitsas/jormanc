// @vitest-environment jsdom

import { createElement, type ReactElement } from "react";
import { act, cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { HomeStyleSelector } from "../components/ui/HomeStyleSelector.js";
import { ThemeToggle } from "../components/ui/ThemeToggle.js";
import { HOME_STYLE_DATA_ATTR, HOME_STYLE_STORAGE_KEY } from "../lib/home-style.js";
import { THEME_STORAGE_KEY } from "../lib/theme.js";
import { HomeStyleProvider } from "./use-home-style.js";
import { ThemeProvider } from "./use-theme.js";

function renderHomeTree(ui: ReactElement) {
  return render(createElement(ThemeProvider, null, createElement(HomeStyleProvider, null, ui)));
}

describe("useHomeStyle persistence and theme", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    delete document.documentElement.dataset[HOME_STYLE_DATA_ATTR];
  });

  afterEach(() => {
    cleanup();
    localStorage.clear();
    document.documentElement.classList.remove("dark");
  });

  it("AC1 — persiste corporate en flit-home-style y reaplica tras simular F5", async () => {
    const user = userEvent.setup();
    renderHomeTree(createElement(HomeStyleSelector));

    await user.click(screen.getByRole("radio", { name: /Corporativo/i }));

    expect(localStorage.getItem(HOME_STYLE_STORAGE_KEY)).toBe("corporate");
    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("corporate");

    cleanup();
    document.documentElement.removeAttribute("data-home-style");

    renderHomeTree(createElement(HomeStyleSelector));

    expect(localStorage.getItem(HOME_STYLE_STORAGE_KEY)).toBe("corporate");
    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("corporate");
    expect(screen.getByRole("radio", { name: /Corporativo/i })).toHaveAttribute(
      "aria-checked",
      "true",
    );
  });

  it("AC1 — aplica preset de inmediato en data-home-style sin recarga", async () => {
    const user = userEvent.setup();
    renderHomeTree(createElement(HomeStyleSelector));

    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("classic");

    await user.click(screen.getByRole("radio", { name: /Corporativo/i }));

    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("corporate");
    expect(screen.getByRole("radio", { name: /Corporativo/i })).toHaveAttribute(
      "aria-checked",
      "true",
    );
  });

  it("sincroniza estilo desde storage event de otra pestaña", () => {
    renderHomeTree(createElement(HomeStyleSelector));
    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("classic");

    act(() => {
      localStorage.setItem(HOME_STYLE_STORAGE_KEY, "corporate");
      window.dispatchEvent(
        new StorageEvent("storage", {
          key: HOME_STYLE_STORAGE_KEY,
          newValue: "corporate",
          storageArea: localStorage,
        }),
      );
    });

    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("corporate");
    expect(screen.getByRole("radio", { name: /Corporativo/i })).toHaveAttribute(
      "aria-checked",
      "true",
    );
  });

  it("AC2 — mantiene preset corporate al activar tema oscuro", async () => {
    const user = userEvent.setup();
    renderHomeTree(
      createElement("div", null, createElement(ThemeToggle), createElement(HomeStyleSelector)),
    );

    await user.click(screen.getByRole("radio", { name: /Corporativo/i }));
    await user.click(screen.getByRole("button", { name: "Activar tema oscuro" }));

    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("corporate");
    expect(localStorage.getItem(HOME_STYLE_STORAGE_KEY)).toBe("corporate");
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");
    expect(screen.getByRole("radio", { name: /Corporativo/i })).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByRole("button", { name: "Activar tema claro" })).toBeInTheDocument();
  });
});
