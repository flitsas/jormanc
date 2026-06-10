import type { ReactNode } from "react";

interface FlitModalProps {
  title: string;
  subtitle?: string;
  onClose: () => void;
  children: ReactNode;
  closeLabel?: string;
  maxWidthClass?: string;
}

export function FlitModal({
  title,
  subtitle,
  onClose,
  children,
  closeLabel = "Cerrar",
  maxWidthClass = "max-w-2xl",
}: FlitModalProps) {
  return (
    <div
      className="flit-modal-overlay"
      role="dialog"
      aria-modal="true"
      aria-labelledby="flit-modal-title"
      onKeyDown={(e) => {
        if (e.key === "Escape") onClose();
      }}
    >
      <div className={`flit-modal ${maxWidthClass}`.trim()}>
        <header className="flit-modal__header">
          <div>
            <h2 id="flit-modal-title" className="flit-modal__title">
              {title}
            </h2>
            {subtitle ? <p className="flit-modal__subtitle">{subtitle}</p> : null}
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label={closeLabel}
            className="flit-modal__close"
          >
            <i className="pi pi-times" aria-hidden="true" />
          </button>
        </header>
        <div className="flit-modal__body">{children}</div>
      </div>
    </div>
  );
}
