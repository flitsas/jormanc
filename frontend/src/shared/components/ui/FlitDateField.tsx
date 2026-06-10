import { Calendar } from "primereact/calendar";

interface FlitDateFieldProps {
  id?: string;
  value: Date | null;
  onChange: (value: Date | null) => void;
  disabled?: boolean;
  maxDate?: Date;
  minDate?: Date;
  placeholder?: string;
  className?: string;
}

/** Calendario con input y trigger integrados en un solo control. */
export function FlitDateField({
  id,
  value,
  onChange,
  disabled = false,
  maxDate,
  minDate,
  placeholder = "Seleccione fecha",
  className = "",
}: FlitDateFieldProps) {
  return (
    <Calendar
      inputId={id}
      value={value}
      onChange={(e) => onChange((e.value as Date) ?? null)}
      dateFormat="dd/mm/yy"
      showIcon
      icon="pi pi-calendar"
      className={`flit-date-field w-full ${className}`.trim()}
      maxDate={maxDate}
      minDate={minDate}
      disabled={disabled}
      placeholder={placeholder}
    />
  );
}
