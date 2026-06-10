import { Button } from "primereact/button";

interface FlitListPanelErrorProps {
  title?: string;
  message: string;
  onRetry?: () => void;
  retryLabel?: string;
}

export function FlitListPanelError({
  title = "No se pudo cargar el listado",
  message,
  onRetry,
  retryLabel = "Reintentar",
}: FlitListPanelErrorProps) {
  return (
    <div className="flit-list-panel__error" role="alert">
      <div className="flit-list-panel__error-icon" aria-hidden="true">
        <i className="pi pi-exclamation-circle" />
      </div>
      <div className="flit-list-panel__error-copy">
        <p className="flit-list-panel__error-title">{title}</p>
        <p className="flit-list-panel__error-desc">{message}</p>
      </div>
      {onRetry ? (
        <div className="flit-list-panel__error-action">
          <Button
            type="button"
            label={retryLabel}
            icon="pi pi-refresh"
            className="flit-btn flit-btn-outline"
            onClick={onRetry}
          />
        </div>
      ) : null}
    </div>
  );
}
