// @vitest-environment jsdom

import { createElement } from "react";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, beforeEach } from "vitest";
import { ThemeProvider } from "../../hooks/use-theme.js";
import { ThemeToggle } from "./ThemeToggle.js";
import {
  PRIME_THEME_DARK,
  PRIME_THEME_LIGHT,
  getActivePrimeThemeId,
} from "../../lib/prime-theme.js";
import { THEME_STORAGE_KEY } from "../../lib/theme.js";

function renderToggle() {
  return render(createElement(ThemeProvider, null, createElement(ThemeToggle)));
}

describe("ThemeToggle", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    document.getElementById("flit-prime-theme")?.remove();
  });

  afterEach(() => {
    cleanup();
  });

  it("activates dark theme and persists preference", async () => {
    const user = userEvent.setup();
    renderToggle();

    await user.click(screen.getByRole("button", { name: "Activar tema oscuro" }));

    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_DARK);
    expect(screen.getByRole("button", { name: "Activar tema claro" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
  });

  it("returns to light theme without residual dark class", async () => {
    const user = userEvent.setup();
    localStorage.setItem(THEME_STORAGE_KEY, "dark");
    document.documentElement.classList.add("dark");
    renderToggle();

    await user.click(screen.getAllByRole("button", { name: "Activar tema claro" })[0]!);

    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe("light");
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_LIGHT);
  });
});
