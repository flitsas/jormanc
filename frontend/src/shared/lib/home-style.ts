export const HOME_STYLE_STORAGE_KEY = "flit-home-style";
export const HOME_STYLE_DATA_ATTR = "homeStyle";

export const DEFAULT_HOME_STYLE_ID = "classic" as const;

export type HomeStyleId = "classic" | "corporate" | "high-contrast";

export type HomeStylePreviewSwatch = {
  primary: string;
  canvas: string;
  accent: string;
};

export type HomeStylePreset = {
  id: HomeStyleId;
  label: string;
  description: string;
  preview: HomeStylePreviewSwatch;
};

/** Catálogo publicado v1 — única fuente de verdad para presets de la página de inicio. */
export const HOME_STYLE_PRESETS: readonly HomeStylePreset[] = [
  {
    id: "classic",
    label: "Clásico FLIT",
    description: "Paleta azul y teal actual del producto.",
    preview: { primary: "#3B82F6", canvas: "#F8FAFC", accent: "#2DD4BF" },
  },
  {
    id: "corporate",
    label: "Corporativo",
    description: "Tonos navy neutros orientados a marca institucional.",
    preview: { primary: "#1E3A5F", canvas: "#F1F5F9", accent: "#0EA5E9" },
  },
  {
    id: "high-contrast",
    label: "Alto contraste",
    description: "Contraste reforzado para legibilidad (WCAG AA).",
    preview: { primary: "#1D4ED8", canvas: "#FFFFFF", accent: "#047857" },
  },
] as const;

export const HOME_STYLE_IDS: readonly HomeStyleId[] = HOME_STYLE_PRESETS.map((p) => p.id);

export function isHomeStyleId(value: string): value is HomeStyleId {
  return (HOME_STYLE_IDS as readonly string[]).includes(value);
}

export function resolveHomeStyleId(stored: string | null | undefined): HomeStyleId {
  if (stored && isHomeStyleId(stored)) return stored;
  return DEFAULT_HOME_STYLE_ID;
}

export function readStoredHomeStyle(): HomeStyleId {
  if (typeof window === "undefined") return DEFAULT_HOME_STYLE_ID;
  return resolveHomeStyleId(localStorage.getItem(HOME_STYLE_STORAGE_KEY));
}

export function applyHomeStyle(id: HomeStyleId): void {
  if (typeof document === "undefined") return;
  document.documentElement.dataset[HOME_STYLE_DATA_ATTR] = id;
}

export function persistHomeStyle(id: HomeStyleId): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(HOME_STYLE_STORAGE_KEY, id);
}

/** Aplica preset desde almacenamiento (arranque / post-F5). */
export function initHomeStyleFromStorage(): HomeStyleId {
  const id = readStoredHomeStyle();
  applyHomeStyle(id);
  return id;
}

export function readHomeStyleCssVar(token: "--home-canvas" | "--home-primary"): string {
  if (typeof document === "undefined") return "";
  return getComputedStyle(document.documentElement).getPropertyValue(token).trim();
}

export function getHomeStylePreset(id: HomeStyleId): HomeStylePreset {
  return HOME_STYLE_PRESETS.find((p) => p.id === id) ?? HOME_STYLE_PRESETS[0];
}
