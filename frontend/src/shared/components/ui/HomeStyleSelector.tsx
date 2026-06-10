import { FlitListPanelError } from "./FlitListPanelError.js";
import { useHomeStyle } from "../../hooks/use-home-style.js";
import {
  HOME_STYLE_PRESETS,
  type HomeStyleId,
  type HomeStylePreset,
} from "../../lib/home-style.js";

export type HomeStyleSelectorStatus = "loading" | "error" | "empty" | "ready";

export type HomeStyleSelectorProps = {
  status?: HomeStyleSelectorStatus;
  presets?: readonly HomeStylePreset[];
  errorMessage?: string;
  onRetry?: () => void;
};

function PalettePreview({ preset }: { preset: HomeStylePreset }) {
  const { primary, canvas, accent } = preset.preview;
  return (
    <div
      className="flex gap-1 rounded-md overflow-hidden border border-flit-border dark:border-flit-border-dark shrink-0"
      aria-hidden="true"
    >
      <span className="w-7 h-7" style={{ backgroundColor: primary }} title="Primario" />
      <span className="w-7 h-7" style={{ backgroundColor: canvas }} title="Fondo" />
      <span className="w-7 h-7" style={{ backgroundColor: accent }} title="Acento" />
    </div>
  );
}

export function HomeStyleSelector({
  status = "ready",
  presets: presetsProp,
  errorMessage = "No se pudo cargar el catálogo de estilos.",
  onRetry,
}: HomeStyleSelectorProps) {
  const { styleId, setStyleId } = useHomeStyle();
  const presets = presetsProp ?? HOME_STYLE_PRESETS;

  if (status === "loading") {
    return (
      <div
        className="flex items-center justify-center gap-3 py-8 text-sm text-flit-muted dark:text-flit-muted-dark"
        role="status"
        aria-live="polite"
      >
        <i className="pi pi-spin pi-spinner text-flit-primary" aria-hidden="true" />
        <span>Cargando estilos visuales…</span>
      </div>
    );
  }

  if (status === "error") {
    return (
      <FlitListPanelError title="Estilos no disponibles" message={errorMessage} onRetry={onRetry} />
    );
  }

  if (status === "empty" || presets.length === 0) {
    return (
      <div
        className="flex flex-col items-center justify-center gap-3 py-10 px-6 text-center"
        role="status"
      >
        <div className="flit-list-panel__empty-icon" aria-hidden="true">
          <i className="pi pi-palette" />
        </div>
        <div className="flit-list-panel__empty-copy">
          <p className="flit-list-panel__empty-title">Sin estilos disponibles</p>
          <p className="text-sm text-flit-muted dark:text-flit-muted-dark max-w-sm">
            El catálogo de estilos no está publicado en este momento. Intenta más tarde.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div
      role="radiogroup"
      aria-labelledby="home-style-selector-label"
      className="grid gap-3 sm:grid-cols-3"
    >
      <p id="home-style-selector-label" className="sr-only">
        Seleccionar estilo visual de la página de inicio
      </p>
      {presets.map((preset) => {
        const selected = styleId === preset.id;
        return (
          <button
            key={preset.id}
            type="button"
            role="radio"
            aria-checked={selected}
            aria-label={`${preset.label}. ${preset.description}`}
            onClick={() => setStyleId(preset.id as HomeStyleId)}
            className={[
              "flex flex-col gap-3 rounded-xl border p-4 text-left transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-flit-primary focus-visible:ring-offset-2",
              "dark:focus-visible:ring-offset-slate-900",
              selected
                ? "border-flit-primary bg-flit-primary/5 shadow-flit dark:border-blue-400 dark:bg-blue-500/10"
                : "border-flit-border bg-white hover:border-flit-primary/40 dark:border-flit-border-dark dark:bg-flit-surface-dark dark:hover:border-blue-400/50",
            ].join(" ")}
          >
            <PalettePreview preset={preset} />
            <span>
              <span className="block text-sm font-semibold text-flit-heading dark:text-flit-heading-dark">
                {preset.label}
              </span>
              <span className="block text-xs text-flit-muted dark:text-flit-muted-dark mt-1 leading-snug">
                {preset.description}
              </span>
            </span>
          </button>
        );
      })}
    </div>
  );
}
