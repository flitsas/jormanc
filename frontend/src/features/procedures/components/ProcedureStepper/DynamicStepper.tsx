import { useMemo, useState } from "react";
import { Button } from "primereact/button";
import { FlitListPanelError } from "../../../../shared/components/ui/FlitListPanelError.js";
import { useAddActor, useCaptureVehicle, useProcedureDetail } from "../../api/procedures.api.js";
import type { CopropietarioEntry } from "./CopropietariosManager.js";
import { CopropietariosManager } from "./CopropietariosManager.js";
import { DynamicFormStep } from "./DynamicFormStep.js";
import { VehicleCaptureStep } from "./VehicleCaptureStep.js";

interface DynamicStepperProps {
  procedureId: string;
  /** Actor definition id for copropietario/comprador step (from parametrizador). */
  copropietarioActorDefinitionId?: string;
}

export function DynamicStepper({
  procedureId,
  copropietarioActorDefinitionId = "00000000-0000-0000-0000-000000000099",
}: DynamicStepperProps) {
  const { data, isLoading, error, refetch } = useProcedureDetail(procedureId);
  const captureVehicle = useCaptureVehicle(procedureId);
  const addActor = useAddActor(procedureId);

  const [activeStepIndex, setActiveStepIndex] = useState(0);
  const [fieldValues, setFieldValues] = useState<Record<string, unknown>>({});
  const [vehicleResult, setVehicleResult] =
    useState<ReturnType<typeof useCaptureVehicle>["data"]>(undefined);
  const [copropietarios, setCopropietarios] = useState<CopropietarioEntry[]>([]);

  const steps = useMemo(() => {
    if (!data?.snapshotConfig.steps.length) return [];
    return [...data.snapshotConfig.steps].sort((a, b) => a.orderIndex - b.orderIndex);
  }, [data?.snapshotConfig.steps]);

  const activeStep = steps[activeStepIndex];

  if (isLoading) {
    return (
      <div
        aria-label="Cargando configuración del trámite"
        aria-busy="true"
        className="space-y-3 p-4"
      >
        {Array.from({ length: 3 }).map((_, index) => (
          <div
            key={index}
            className="h-10 animate-pulse rounded-md bg-flit-border/40 dark:bg-flit-border-dark/40"
          />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <FlitListPanelError
        title="No se pudo cargar el trámite"
        message={error.message}
        onRetry={() => void refetch()}
      />
    );
  }

  if (!data || steps.length === 0) {
    return (
      <p className="text-sm text-flit-muted p-4" role="status">
        No hay pasos configurados en el snapshot del trámite.
      </p>
    );
  }

  const procedureDetail = data;

  function goNext() {
    setActiveStepIndex((current) => Math.min(current + 1, steps.length - 1));
  }

  function renderStepContent() {
    if (!activeStep) return null;

    const stepType = activeStep.stepType.toLowerCase();

    if (stepType === "vehicle") {
      return (
        <VehicleCaptureStep
          procedureId={procedureId}
          vehicleQueryKey={procedureDetail.vehicleQueryKey}
          isSubmitting={captureVehicle.isPending}
          error={captureVehicle.error}
          result={vehicleResult ?? captureVehicle.data ?? null}
          onCapture={async (payload) => {
            const response = await captureVehicle.mutateAsync(payload);
            setVehicleResult(response);
          }}
          onContinue={goNext}
        />
      );
    }

    if (stepType === "actors" || stepType === "copropietarios") {
      return (
        <CopropietariosManager
          procedureId={procedureId}
          actorDefinitionId={copropietarioActorDefinitionId}
          entries={copropietarios}
          onEntriesChange={setCopropietarios}
          onAddActor={async (payload) => {
            await addActor.mutateAsync(payload);
          }}
          onContinue={goNext}
        />
      );
    }

    return (
      <div className="space-y-4">
        <DynamicFormStep
          stepName={activeStep.name}
          sections={activeStep.sections}
          values={fieldValues}
          onChange={(slug, value) => setFieldValues((prev) => ({ ...prev, [slug]: value }))}
        />
        {activeStepIndex < steps.length - 1 ? (
          <Button
            type="button"
            label="Continuar"
            icon="pi pi-arrow-right"
            iconPos="right"
            className="flit-btn flit-btn-primary"
            onClick={goNext}
          />
        ) : null}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <nav aria-label="Pasos del trámite">
        <ol className="flex flex-wrap gap-2">
          {steps.map((step, index) => {
            const isActive = index === activeStepIndex;
            const isComplete = index < activeStepIndex;
            return (
              <li key={step.id}>
                <button
                  type="button"
                  onClick={() => setActiveStepIndex(index)}
                  aria-current={isActive ? "step" : undefined}
                  className={[
                    "rounded-full px-3 py-1 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-flit-primary text-white"
                      : isComplete
                        ? "bg-flit-primary/15 text-flit-primary"
                        : "bg-flit-border/30 text-flit-muted",
                  ].join(" ")}
                >
                  {step.orderIndex}. {step.name}
                </button>
              </li>
            );
          })}
        </ol>
      </nav>

      <div data-testid="dynamic-stepper-content" data-snapshot-id={data.procedureTypeSnapshotId}>
        {renderStepContent()}
      </div>
    </div>
  );
}
