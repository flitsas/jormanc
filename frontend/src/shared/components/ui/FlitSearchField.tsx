import { InputText } from "primereact/inputtext";

interface FlitSearchFieldProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  id?: string;
  "aria-label"?: string;
  disabled?: boolean;
}

/** Búsqueda con icono en columna fija (sin solapar el texto). */
export function FlitSearchField({
  value,
  onChange,
  placeholder = "Buscar…",
  className = "",
  id,
  "aria-label": ariaLabel = "Buscar",
  disabled = false,
}: FlitSearchFieldProps) {
  return (
    <div className={`flit-search-field ${className}`.trim()}>
      <span className="flit-search-field__icon-wrap" aria-hidden="true">
        <i className="pi pi-search" />
      </span>
      <InputText
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="flit-search-field__input"
        aria-label={ariaLabel}
        disabled={disabled}
      />
    </div>
  );
}
