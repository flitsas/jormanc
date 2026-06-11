import { useId, useState } from "react";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { FlitFormField } from "../../../../shared/components/ui/FlitFormField.js";
import { FlitListPanelError } from "../../../../shared/components/ui/FlitListPanelError.js";
import { VehicleWarningBanner } from "../VehicleWarningBanner.js";
import { getVehicleQueryLabel } from "../../lib/vehicleQueryLabels.js";
import type { z } from "zod";
import { CaptureVehicleResponseSchema } from "../../api/procedures.schemas.js";

type CaptureVehicleResponse = z.infer<typeof CaptureVehicleResponseSchema>;

interface VehicleCaptureStepProps {
  procedureId: string;
  vehicleQueryKey: string;
  isSubmitting: boolean;
  error: Error | null;
  result: CaptureVehicleResponse | null;
  onCapture: (value: { plate?: string; vin?: string }) => void;
  onContinue: () => void;
}

export function VehicleCaptureStep({
  vehicleQueryKey,
  isSubmitting,
  error,
  result,
  onCapture,
  onContinue,
}: VehicleCaptureStepProps) {
  const inputId = useId();
  const [value, setValue] = useState("");
  const label = getVehicleQueryLabel(vehicleQueryKey);
  const isVin = vehicleQueryKey.toLowerCase() === "vin";

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = value.trim();
    if (!trimmed) return;
    onCapture(isVin ? { vin: trimmed } : { plate: trimmed });
  }

  if (error && !result) {
    return (
      <FlitListPanelError
        title="Error al consultar vehículo"
        message={error.message}
        onRetry={() => {
          const trimmed = value.trim();
          if (trimmed) onCapture(isVin ? { vin: trimmed } : { plate: trimmed });
        }}
      />
    );
  }

  return (
    <section aria-labelledby="vehicle-step-title" className="space-y-4">
      <h2
        id="vehicle-step-title"
        className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
      >
        Captura de vehículo
      </h2>

      <form onSubmit={handleSubmit} className="space-y-4 max-w-md">
        <FlitFormField label={label} htmlFor={inputId} required>
          <InputText
            id={inputId}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            disabled={isSubmitting}
            aria-describedby={isSubmitting ? "vehicle-loading-hint" : undefined}
            className="w-full"
            autoComplete="off"
          />
        </FlitFormField>

        {isSubmitting ? (
          <p
            id="vehicle-loading-hint"
            className="text-sm text-flit-muted"
            role="status"
            aria-live="polite"
          >
            Consultando RUNT…
          </p>
        ) : null}

        {result?.warnings.length ? <VehicleWarningBanner warnings={result.warnings} /> : null}

        <Button
          type="submit"
          label="Consultar vehículo"
          icon="pi pi-search"
          className="flit-btn flit-btn-primary"
          disabled={isSubmitting || !value.trim()}
          loading={isSubmitting}
        />
      </form>

      {result ? (
        <div className="rounded-lg border border-flit-border dark:border-flit-border-dark p-4 space-y-2">
          <h3 className="font-medium text-flit-heading dark:text-flit-heading-dark">
            Datos del vehículo
          </h3>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-sm">
            {result.vehicle.plate ? (
              <>
                <dt className="text-flit-muted">Placa</dt>
                <dd>{result.vehicle.plate}</dd>
              </>
            ) : null}
            {result.vehicle.vin ? (
              <>
                <dt className="text-flit-muted">VIN</dt>
                <dd>{result.vehicle.vin}</dd>
              </>
            ) : null}
            {result.vehicle.brand ? (
              <>
                <dt className="text-flit-muted">Marca</dt>
                <dd>{result.vehicle.brand}</dd>
              </>
            ) : null}
            {result.vehicle.model ? (
              <>
                <dt className="text-flit-muted">Modelo</dt>
                <dd>{result.vehicle.model}</dd>
              </>
            ) : null}
          </dl>

          <Button
            type="button"
            label="Siguiente"
            icon="pi pi-arrow-right"
            iconPos="right"
            className="flit-btn flit-btn-primary"
            onClick={onContinue}
          />
        </div>
      ) : null}
    </section>
  );
}
