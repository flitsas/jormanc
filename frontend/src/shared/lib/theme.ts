export const THEME_STORAGE_KEY = "flit-theme";

export type ThemePreference = "light" | "dark" | "system";

export function resolveIsDark(preference: ThemePreference): boolean {
  if (preference === "dark") return true;
  if (preference === "light") return false;
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return false;
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

export function readStoredTheme(): ThemePreference {
  if (typeof window === "undefined") return "system";
  const stored = localStorage.getItem(THEME_STORAGE_KEY);
  if (stored === "light" || stored === "dark" || stored === "system") return stored;
  return "system";
}

import { applyPrimeTheme } from "./prime-theme.js";

export function applyThemeClass(isDark: boolean): void {
  document.documentElement.classList.toggle("dark", isDark);
  applyPrimeTheme(isDark);
}

export function persistTheme(preference: ThemePreference): void {
  localStorage.setItem(THEME_STORAGE_KEY, preference);
}
