import { useId, useState } from "react";
import { Button } from "primereact/button";
import { InputNumber } from "primereact/inputnumber";
import { InputText } from "primereact/inputtext";
import { FlitFormField } from "../../../../shared/components/ui/FlitFormField.js";
import { CuotaValidationError } from "../../api/procedures.api.js";
import { computeCuotaSum } from "../../lib/cuotaValidation.js";

export interface CopropietarioEntry {
  id: string;
  documentNumber: string;
  cuotaPct: number;
}

interface CopropietariosManagerProps {
  procedureId: string;
  actorDefinitionId: string;
  maxBuyers?: number;
  entries: CopropietarioEntry[];
  onEntriesChange: (entries: CopropietarioEntry[]) => void;
  onAddActor: (payload: {
    actorDefinitionId: string;
    nature: "natural";
    documentNumber: string;
    cuotaPct: number;
  }) => Promise<void>;
  onContinue: () => void;
}

export function CopropietariosManager({
  actorDefinitionId,
  maxBuyers = 4,
  entries,
  onEntriesChange,
  onAddActor,
  onContinue,
}: CopropietariosManagerProps) {
  const docId = useId();
  const cuotaId = useId();
  const [documentNumber, setDocumentNumber] = useState("");
  const [cuotaPct, setCuotaPct] = useState<number | null>(null);
  const [inlineError, setInlineError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const totalCuota = computeCuotaSum(entries.map((entry) => entry.cuotaPct));
  const canAddMore = entries.length < maxBuyers;
  const continueDisabled = inlineError !== null || isSubmitting;

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    setInlineError(null);

    const doc = documentNumber.trim();
    const cuota = cuotaPct ?? 0;
    if (!doc || cuota <= 0) return;

    setIsSubmitting(true);
    try {
      await onAddActor({
        actorDefinitionId,
        nature: "natural",
        documentNumber: doc,
        cuotaPct: cuota,
      });

      onEntriesChange([
        ...entries,
        { id: crypto.randomUUID(), documentNumber: doc, cuotaPct: cuota },
      ]);
      setDocumentNumber("");
      setCuotaPct(null);
    } catch (err) {
      if (err instanceof CuotaValidationError) {
        setInlineError(err.message);
      } else if (err instanceof Error) {
        setInlineError(err.message);
      } else {
        setInlineError("No se pudo agregar el copropietario.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section aria-labelledby="copropietarios-title" className="space-y-4">
      <h2
        id="copropietarios-title"
        className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
      >
        Copropietarios
      </h2>

      {entries.length === 0 ? (
        <p className="text-sm text-flit-muted" role="status">
          Agregue el primer comprador con su porcentaje de cuota.
        </p>
      ) : (
        <ul className="space-y-2" aria-label="Lista de copropietarios">
          {entries.map((entry) => (
            <li
              key={entry.id}
              className="flex items-center justify-between rounded-md border border-flit-border dark:border-flit-border-dark px-3 py-2 text-sm"
            >
              <span>{entry.documentNumber}</span>
              <span className="font-medium">{entry.cuotaPct}%</span>
            </li>
          ))}
        </ul>
      )}

      <p className="text-sm text-flit-muted" aria-live="polite">
        Total cuotas: <strong>{totalCuota}%</strong> (debe sumar exactamente 100%)
      </p>

      {canAddMore ? (
        <form onSubmit={handleAdd} className="grid gap-4 sm:grid-cols-2 max-w-2xl">
          <FlitFormField label="Número de documento" htmlFor={docId} required>
            <InputText
              id={docId}
              value={documentNumber}
              onChange={(e) => setDocumentNumber(e.target.value)}
              disabled={isSubmitting}
              className="w-full"
            />
          </FlitFormField>

          <FlitFormField label="Cuota (%)" htmlFor={cuotaId} required>
            <InputNumber
              inputId={cuotaId}
              value={cuotaPct}
              onValueChange={(e) => setCuotaPct(e.value ?? null)}
              min={0}
              max={100}
              suffix="%"
              disabled={isSubmitting}
              className="w-full"
            />
          </FlitFormField>

          {inlineError ? (
            <p id="cuota-inline-error" className="sm:col-span-2 text-sm text-red-600" role="alert">
              {inlineError}
            </p>
          ) : null}

          <div className="sm:col-span-2">
            <Button
              type="submit"
              label="Agregar copropietario"
              icon="pi pi-user-plus"
              className="flit-btn flit-btn-outline"
              disabled={isSubmitting || !documentNumber.trim() || !cuotaPct}
              loading={isSubmitting}
            />
          </div>
        </form>
      ) : (
        <p className="text-sm text-flit-muted">Máximo de {maxBuyers} compradores alcanzado.</p>
      )}

      <Button
        type="button"
        label="Continuar"
        icon="pi pi-arrow-right"
        iconPos="right"
        className="flit-btn flit-btn-primary"
        disabled={continueDisabled}
        aria-describedby={inlineError ? "cuota-inline-error" : undefined}
        onClick={onContinue}
      />
    </section>
  );
}
