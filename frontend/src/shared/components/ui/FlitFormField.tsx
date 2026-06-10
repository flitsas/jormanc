import type { ReactNode } from "react";

interface FlitFormFieldProps {
  label: string;
  htmlFor?: string;
  required?: boolean;
  children: ReactNode;
  className?: string;
  hint?: string;
}

export function FlitFormField({
  label,
  htmlFor,
  required = false,
  children,
  className = "",
  hint,
}: FlitFormFieldProps) {
  return (
    <div className={`flit-field min-w-0 ${className}`.trim()}>
      <label htmlFor={htmlFor} className="flit-label">
        {label}
        {required ? <span className="text-flit-primary"> *</span> : null}
      </label>
      {children}
      {hint ? <p className="flit-hint">{hint}</p> : null}
    </div>
  );
}
