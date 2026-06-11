import type { DashboardFamily } from "../api/dashboard.schemas.js";

export const FAMILY_LABELS: Record<DashboardFamily, string> = {
  matricula_inicial: "Matrículas",
  traspasos: "Traspasos",
  otros: "Otros",
};

/** Colores FLIT por familia (tokens brand/state). */
export const FAMILY_COLORS: Record<DashboardFamily, string> = {
  matricula_inicial: "#4FD4CC",
  traspasos: "#4F74C9",
  otros: "#70CF3A",
};

export function familyLabel(family: DashboardFamily): string {
  return FAMILY_LABELS[family];
}
