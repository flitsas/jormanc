import { afterEach, beforeEach, describe, expect, it } from "vitest";
import {
  PRIME_THEME_DARK,
  PRIME_THEME_LIGHT,
  PRIME_THEME_LINK_ID,
  applyPrimeTheme,
  getActivePrimeThemeId,
  syncPrimeThemeFromDocument,
} from "./prime-theme.js";

describe("prime-theme", () => {
  beforeEach(() => {
    document.getElementById(PRIME_THEME_LINK_ID)?.remove();
    document.documentElement.classList.remove("dark");
  });

  afterEach(() => {
    document.getElementById(PRIME_THEME_LINK_ID)?.remove();
    document.documentElement.classList.remove("dark");
  });

  it("loads lara-light-blue when theme is light", () => {
    applyPrimeTheme(false);
    const link = document.getElementById(PRIME_THEME_LINK_ID) as HTMLLinkElement;
    expect(link).toBeTruthy();
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_LIGHT);
    expect(link.href.length).toBeGreaterThan(0);
  });

  it("loads lara-dark-blue when theme is dark", () => {
    applyPrimeTheme(true);
    expect(document.getElementById(PRIME_THEME_LINK_ID)).toBeTruthy();
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_DARK);
  });

  it("switches Prime theme without duplicating link elements", () => {
    applyPrimeTheme(false);
    applyPrimeTheme(true);
    applyPrimeTheme(false);
    expect(document.querySelectorAll(`#${PRIME_THEME_LINK_ID}`)).toHaveLength(1);
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_LIGHT);
  });

  it("syncPrimeThemeFromDocument follows html.dark class", () => {
    document.documentElement.classList.add("dark");
    syncPrimeThemeFromDocument();
    expect(getActivePrimeThemeId()).toBe(PRIME_THEME_DARK);
  });
});
