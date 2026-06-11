export interface CoherenceConflict {
  rule1: string;
  rule2: string;
  description: string;
}

interface CoherenceSimulatorPanelProps {
  isCoherent: boolean | null;
  conflicts: CoherenceConflict[];
  isVerifying?: boolean;
  onVerify: () => void;
}

export function CoherenceSimulatorPanel({
  isCoherent,
  conflicts,
  isVerifying = false,
  onVerify,
}: CoherenceSimulatorPanelProps) {
  return (
    <div className="rounded-lg border border-flit-border p-4 dark:border-flit-border-dark">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="font-semibold text-flit-heading dark:text-flit-heading-dark">
          Simulador de coherencia
        </h3>
        <button
          type="button"
          onClick={onVerify}
          disabled={isVerifying}
          className="rounded-lg border border-flit-primary px-3 py-1.5 text-sm font-medium text-flit-primary disabled:opacity-50"
        >
          {isVerifying ? "Verificando…" : "Verificar coherencia"}
        </button>
      </div>

      {isCoherent === null ? (
        <p className="mt-2 text-sm text-flit-muted" role="status">
          Ejecute la verificación antes de guardar la regla.
        </p>
      ) : isCoherent ? (
        <p className="mt-2 text-sm text-green-700" role="status">
          Sin conflictos detectados. Puede guardar la regla.
        </p>
      ) : (
        <div className="mt-3" role="alert">
          <p className="text-sm font-medium text-red-700">Conflictos detectados:</p>
          <ul className="mt-2 list-disc space-y-1 pl-5 text-sm text-red-800">
            {conflicts.map((c, i) => (
              <li key={i}>
                <strong>
                  {c.rule1} ↔ {c.rule2}
                </strong>
                : {c.description}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
