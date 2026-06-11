import type { DashboardFamily, FamilySummary } from "../api/dashboard.schemas.js";
import { FAMILY_COLORS, familyLabel } from "./familyLabels.js";

export interface PieSegment {
  family: DashboardFamily;
  count: number;
  pct: number;
  color: string;
  label: string;
  startAngle: number;
  endAngle: number;
}

function polarToCartesian(cx: number, cy: number, radius: number, angleDeg: number) {
  const rad = ((angleDeg - 90) * Math.PI) / 180;
  return {
    x: cx + radius * Math.cos(rad),
    y: cy + radius * Math.sin(rad),
  };
}

export function describeDonutSegment(
  cx: number,
  cy: number,
  outerR: number,
  innerR: number,
  startAngle: number,
  endAngle: number,
): string {
  const sweep = endAngle - startAngle;
  if (sweep <= 0) return "";
  if (sweep >= 360) {
    endAngle = startAngle + 359.99;
  }

  const largeArc = endAngle - startAngle > 180 ? 1 : 0;
  const outerStart = polarToCartesian(cx, cy, outerR, startAngle);
  const outerEnd = polarToCartesian(cx, cy, outerR, endAngle);
  const innerEnd = polarToCartesian(cx, cy, innerR, endAngle);
  const innerStart = polarToCartesian(cx, cy, innerR, startAngle);

  return [
    `M ${outerStart.x} ${outerStart.y}`,
    `A ${outerR} ${outerR} 0 ${largeArc} 1 ${outerEnd.x} ${outerEnd.y}`,
    `L ${innerEnd.x} ${innerEnd.y}`,
    `A ${innerR} ${innerR} 0 ${largeArc} 0 ${innerStart.x} ${innerStart.y}`,
    "Z",
  ].join(" ");
}

export function buildPieSegments(byFamily: FamilySummary[]): PieSegment[] {
  const total = byFamily.reduce((sum, item) => sum + item.count, 0);
  if (total === 0) return [];

  let currentAngle = 0;
  return byFamily.map((item) => {
    const sweep = (item.count / total) * 360;
    const segment: PieSegment = {
      family: item.family,
      count: item.count,
      pct: item.pct,
      color: FAMILY_COLORS[item.family],
      label: familyLabel(item.family),
      startAngle: currentAngle,
      endAngle: currentAngle + sweep,
    };
    currentAngle += sweep;
    return segment;
  });
}
