import { describe, expect, it } from "vitest";
import { buildPieSegments, describeDonutSegment } from "./pieChart.js";
import type { FamilySummary } from "../api/dashboard.schemas.js";

const BY_FAMILY: FamilySummary[] = [
  {
    family: "matricula_inicial",
    count: 120,
    pct: 23.76,
    byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
  },
  {
    family: "traspasos",
    count: 340,
    pct: 67.33,
    byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
  },
  {
    family: "otros",
    count: 45,
    pct: 8.91,
    byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
  },
];

describe("pieChart utils", () => {
  it("buildPieSegments genera 3 segmentos con ángulos acumulados", () => {
    const segments = buildPieSegments(BY_FAMILY);
    expect(segments).toHaveLength(3);
    expect(segments[0]?.startAngle).toBe(0);
    expect(segments[2]?.endAngle).toBeCloseTo(360, 0);
  });

  it("describeDonutSegment retorna path SVG válido", () => {
    const path = describeDonutSegment(120, 120, 100, 58, 0, 90);
    expect(path).toMatch(/^M /);
    expect(path).toContain("A ");
    expect(path.endsWith("Z")).toBe(true);
  });
});
