import { useState } from "react";
import type { FamilySummary } from "../api/dashboard.schemas.js";
import type { DashboardFamily } from "../api/dashboard.schemas.js";
import { buildPieSegments, describeDonutSegment } from "../lib/pieChart.js";
import { FAMILY_COLORS, familyLabel } from "../lib/familyLabels.js";

interface FamilyPieChartProps {
  byFamily: FamilySummary[];
  total: number;
  isLoading: boolean;
  error: Error | null;
  selectedFamily: DashboardFamily | null;
  onFamilySelect: (family: DashboardFamily) => void;
  onRetry: () => void;
}

const CX = 120;
const CY = 120;
const OUTER_R = 100;
const INNER_R = 58;

function PieSkeleton() {
  return (
    <div
      className="flex flex-col items-center gap-4 py-6"
      aria-label="Cargando gráfico circular"
      aria-busy="true"
    >
      <div className="h-60 w-60 animate-pulse rounded-full bg-flit-border/40 dark:bg-flit-border-dark/40" />
      <div className="flex gap-4">
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="h-4 w-24 animate-pulse rounded bg-flit-border/40 dark:bg-flit-border-dark/40"
          />
        ))}
      </div>
    </div>
  );
}

export function FamilyPieChart({
  byFamily,
  total,
  isLoading,
  error,
  selectedFamily,
  onFamilySelect,
  onRetry,
}: FamilyPieChartProps) {
  const [hoveredFamily, setHoveredFamily] = useState<DashboardFamily | null>(null);
  const segments = buildPieSegments(byFamily);

  if (isLoading) {
    return (
      <div className="flit-card">
        <h2 className="text-base font-semibold text-flit-heading dark:text-flit-heading-dark mb-4">
          Trámites por familia
        </h2>
        <PieSkeleton />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flit-card">
        <h2 className="text-base font-semibold text-flit-heading dark:text-flit-heading-dark mb-4">
          Trámites por familia
        </h2>
        <div className="flit-list-panel__error" role="alert">
          <div className="flit-list-panel__error-icon" aria-hidden="true">
            <i className="pi pi-exclamation-circle" />
          </div>
          <div className="flit-list-panel__error-copy">
            <p className="flit-list-panel__error-title">No se pudo cargar el gráfico</p>
            <p className="flit-list-panel__error-desc">{error.message}</p>
          </div>
          <button
            type="button"
            onClick={onRetry}
            className="inline-flex items-center gap-1.5 rounded-lg border border-flit-border bg-white px-3 py-2 text-sm font-medium"
          >
            <i className="pi pi-refresh" aria-hidden="true" />
            Reintentar
          </button>
        </div>
      </div>
    );
  }

  if (total === 0 || segments.length === 0) {
    return (
      <div className="flit-card">
        <h2 className="text-base font-semibold text-flit-heading dark:text-flit-heading-dark mb-4">
          Trámites por familia
        </h2>
        <div className="flit-list-panel__empty py-12" role="status">
          <i className="pi pi-chart-pie text-3xl text-flit-muted mb-2" aria-hidden="true" />
          <p className="font-medium text-flit-heading dark:text-flit-heading-dark">
            Sin trámites en el período
          </p>
          <p className="text-sm text-flit-muted mt-1">
            Ajusta el rango de fechas para ver la distribución por familia.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="flit-card">
      <h2 className="text-base font-semibold text-flit-heading dark:text-flit-heading-dark mb-4">
        Trámites por familia
      </h2>

      <div className="flex flex-col items-center gap-6 lg:flex-row lg:items-start lg:justify-center">
        <div className="relative">
          <svg
            viewBox="0 0 240 240"
            className="h-60 w-60"
            role="img"
            aria-label="Gráfico circular de trámites por familia"
          >
            <title>Distribución de trámites por familia en el período seleccionado</title>
            {segments.map((segment) => {
              const path = describeDonutSegment(
                CX,
                CY,
                OUTER_R,
                INNER_R,
                segment.startAngle,
                segment.endAngle,
              );
              if (!path) return null;

              const isSelected = selectedFamily === segment.family;
              const isHovered = hoveredFamily === segment.family;

              return (
                <path
                  key={segment.family}
                  d={path}
                  fill={segment.color}
                  stroke={isSelected || isHovered ? "#162744" : "transparent"}
                  strokeWidth={isSelected || isHovered ? 2 : 0}
                  className="cursor-pointer transition-opacity"
                  opacity={selectedFamily && !isSelected ? 0.45 : 1}
                  onClick={() => onFamilySelect(segment.family)}
                  onMouseEnter={() => setHoveredFamily(segment.family)}
                  onMouseLeave={() => setHoveredFamily(null)}
                  onFocus={() => setHoveredFamily(segment.family)}
                  onBlur={() => setHoveredFamily(null)}
                  tabIndex={0}
                  role="button"
                  aria-label={`${segment.label}: ${segment.count} trámites (${segment.pct}%)`}
                  aria-pressed={isSelected}
                >
                  <title>
                    {segment.label}: {segment.count} ({segment.pct}%)
                  </title>
                </path>
              );
            })}
            <text
              x={CX}
              y={CY - 6}
              textAnchor="middle"
              className="fill-flit-heading text-lg font-bold"
              style={{ fontSize: "18px", fontWeight: 700 }}
            >
              {total}
            </text>
            <text
              x={CX}
              y={CY + 14}
              textAnchor="middle"
              className="fill-flit-muted"
              style={{ fontSize: "12px" }}
            >
              total
            </text>
          </svg>

          {hoveredFamily ? (
            <div
              className="pointer-events-none absolute left-1/2 top-2 -translate-x-1/2 rounded-lg border border-flit-border bg-white px-3 py-1.5 text-xs shadow-flit dark:border-flit-border-dark dark:bg-slate-900"
              role="tooltip"
            >
              {(() => {
                const seg = segments.find((s) => s.family === hoveredFamily);
                if (!seg) return null;
                return (
                  <span>
                    <strong>{seg.label}</strong>: {seg.count} ({seg.pct}%)
                  </span>
                );
              })()}
            </div>
          ) : null}
        </div>

        <ul className="flex flex-col gap-3 min-w-[12rem]" aria-label="Leyenda del gráfico">
          {byFamily.map((item) => (
            <li key={item.family}>
              <button
                type="button"
                onClick={() => onFamilySelect(item.family)}
                className={`flex w-full items-center gap-3 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
                  selectedFamily === item.family
                    ? "border-flit-primary bg-flit-primary/5"
                    : "border-flit-border hover:bg-flit-canvas dark:border-flit-border-dark"
                }`}
                aria-pressed={selectedFamily === item.family}
              >
                <span
                  className="h-3 w-3 shrink-0 rounded-full"
                  style={{ backgroundColor: FAMILY_COLORS[item.family] }}
                  aria-hidden="true"
                />
                <span className="flex-1 font-medium text-flit-heading dark:text-flit-heading-dark">
                  {familyLabel(item.family)}
                </span>
                <span className="text-flit-muted tabular-nums">
                  {item.count} ({item.pct}%)
                </span>
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
