import { describe, expect, it, beforeEach, afterEach } from "vitest";
import {
  PRIME_THEME_DARK,
  PRIME_THEME_LIGHT,
  PRIME_THEME_LINK_ID,
  getActivePrimeThemeId,
} from "./prime-theme.js";
import {
  THEME_STORAGE_KEY,
  applyThemeClass,
  persistTheme,
  readStoredTheme,
  resolveIsDark,
} from "./theme.js";

describe("theme utilities", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    document.getElementById(PRIME_THEME_LINK_ID)?.remove();
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    document.getElementById(PRIME_THEME_LINK_ID)?.remove();
  });

  it("defaults to system when storage is empty", () => {
    expect(readStoredTheme()).toBe("system");
  });

  it("persists and reads theme preference", () => {
    persistTheme("dark");
    expect(readStoredTheme()).toBe("dark");
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");
  });

  it("resolveIsDark respects explicit light and dark", () => {
    expect(resolveIsDark("light")).toBe(false);
    expect(resolveIsDark("dark")).toBe(true);
  });

  it("applyThemeClass syncs PrimeReact theme with html.dark", () => {
    applyThemeClass(true);
    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_DARK);

    applyThemeClass(false);
    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_LIGHT);
  });
});
