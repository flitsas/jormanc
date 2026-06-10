/**
 * PrimeReact theme switching (lara-light-blue ↔ lara-dark-blue).
 * Documented in prime-flit.css; synced with Tailwind `html.dark` via applyThemeClass.
 */
import laraLightThemeUrl from "primereact/resources/themes/lara-light-blue/theme.css?url";
import laraDarkThemeUrl from "primereact/resources/themes/lara-dark-blue/theme.css?url";

export const PRIME_THEME_LINK_ID = "flit-prime-theme";

export const PRIME_THEME_LIGHT = "lara-light-blue";
export const PRIME_THEME_DARK = "lara-dark-blue";

const themeUrls: Record<typeof PRIME_THEME_LIGHT | typeof PRIME_THEME_DARK, string> = {
  [PRIME_THEME_LIGHT]: laraLightThemeUrl,
  [PRIME_THEME_DARK]: laraDarkThemeUrl,
};

function getPrimeThemeLink(): HTMLLinkElement {
  let link = document.getElementById(PRIME_THEME_LINK_ID) as HTMLLinkElement | null;
  if (!link) {
    link = document.createElement("link");
    link.id = PRIME_THEME_LINK_ID;
    link.rel = "stylesheet";
    link.setAttribute("data-flit-prime-theme", "true");
    document.head.appendChild(link);
  }
  return link;
}

/** Applies PrimeReact Lara theme without full page reload. */
export function applyPrimeTheme(isDark: boolean): void {
  if (typeof document === "undefined") return;
  const themeId = isDark ? PRIME_THEME_DARK : PRIME_THEME_LIGHT;
  const link = getPrimeThemeLink();
  if (link.getAttribute("data-theme") === themeId) return;
  link.href = themeUrls[themeId];
  link.setAttribute("data-theme", themeId);
}

/** Sync Prime theme from `html.dark` (e.g. after index.html bootstrap). */
export function syncPrimeThemeFromDocument(): void {
  if (typeof document === "undefined") return;
  applyPrimeTheme(document.documentElement.classList.contains("dark"));
}

export function getActivePrimeThemeId(): string | null {
  if (typeof document === "undefined") return null;
  const link = document.getElementById(PRIME_THEME_LINK_ID) as HTMLLinkElement | null;
  return link?.getAttribute("data-theme") ?? null;
}
