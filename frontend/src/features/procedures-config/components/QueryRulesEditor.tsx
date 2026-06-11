import { useState } from "react";

export const VERIFICATION_TYPES = [
  "datos_persona",
  "simit",
  "rues",
  "restricciones",
  "liveness",
] as const;

export type VerificationType = (typeof VERIFICATION_TYPES)[number];

export interface VerificationToggle {
  type: VerificationType;
  isActive: boolean;
  order: number;
}

interface QueryRulesEditorProps {
  actorRole: string;
  initialVerifications?: VerificationToggle[];
  isSaving?: boolean;
  onSave: (verifications: VerificationToggle[]) => Promise<void>;
}

function defaultToggles(): VerificationToggle[] {
  return VERIFICATION_TYPES.map((type, index) => ({
    type,
    isActive: type === "datos_persona" || type === "simit",
    order: index + 1,
  }));
}

export function QueryRulesEditor({
  actorRole,
  initialVerifications,
  isSaving = false,
  onSave,
}: QueryRulesEditorProps) {
  const [toggles, setToggles] = useState<VerificationToggle[]>(
    initialVerifications ?? defaultToggles(),
  );

  function toggleType(type: VerificationType, active: boolean) {
    setToggles((prev) => prev.map((t) => (t.type === type ? { ...t, isActive: active } : t)));
  }

  const activeOrdered = toggles.filter((t) => t.isActive).map((t, i) => ({ ...t, order: i + 1 }));

  return (
    <div className="rounded-lg border border-flit-border p-4 dark:border-flit-border-dark">
      <h3 className="font-semibold text-flit-heading dark:text-flit-heading-dark">
        Reglas de consulta — {actorRole}
      </h3>
      <ul className="mt-3 space-y-2" aria-label="Verificaciones disponibles">
        {VERIFICATION_TYPES.map((type) => {
          const entry = toggles.find((t) => t.type === type)!;
          return (
            <li key={type} className="flex items-center justify-between gap-3 text-sm">
              <span className="font-mono">{type}</span>
              <label className="flex items-center gap-2">
                <span className="text-flit-muted">{entry.isActive ? "activo" : "inactivo"}</span>
                <input
                  type="checkbox"
                  role="switch"
                  aria-label={`Verificación ${type}`}
                  checked={entry.isActive}
                  onChange={(e) => toggleType(type, e.target.checked)}
                />
              </label>
            </li>
          );
        })}
      </ul>
      <button
        type="button"
        disabled={isSaving}
        onClick={() => void onSave(activeOrdered)}
        className="mt-4 rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
      >
        {isSaving ? "Guardando…" : "Guardar verificaciones"}
      </button>
    </div>
  );
}
