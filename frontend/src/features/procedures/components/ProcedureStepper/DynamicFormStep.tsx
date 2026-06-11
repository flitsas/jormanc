import { useId } from "react";
import { InputText } from "primereact/inputtext";
import { InputTextarea } from "primereact/inputtextarea";
import { Checkbox } from "primereact/checkbox";
import { FlitFormField } from "../../../../shared/components/ui/FlitFormField.js";
import type { FormField, FormSection } from "../../api/procedures.schemas.js";

interface DynamicFormStepProps {
  stepName: string;
  sections: FormSection[];
  values: Record<string, unknown>;
  onChange: (slug: string, value: unknown) => void;
}

function renderFieldInput(
  field: FormField,
  inputId: string,
  value: unknown,
  onChange: (slug: string, value: unknown) => void,
) {
  const stringValue = value == null ? "" : String(value);

  switch (field.fieldType) {
    case "textarea":
      return (
        <InputTextarea
          id={inputId}
          value={stringValue}
          onChange={(e) => onChange(field.slug, e.target.value)}
          rows={3}
          className="w-full"
        />
      );
    case "checkbox":
      return (
        <Checkbox
          inputId={inputId}
          checked={Boolean(value)}
          onChange={(e) => onChange(field.slug, e.checked ?? false)}
        />
      );
    default:
      return (
        <InputText
          id={inputId}
          value={stringValue}
          onChange={(e) => onChange(field.slug, e.target.value)}
          className="w-full"
        />
      );
  }
}

export function DynamicFormStep({ stepName, sections, values, onChange }: DynamicFormStepProps) {
  const baseId = useId();

  if (sections.length === 0) {
    return (
      <p className="text-sm text-flit-muted" role="status">
        El paso &quot;{stepName}&quot; no tiene secciones configuradas.
      </p>
    );
  }

  return (
    <section aria-labelledby={`${baseId}-title`} className="space-y-6">
      <h2 id={`${baseId}-title`} className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark">
        {stepName}
      </h2>

      {sections.map((section) => (
        <fieldset
          key={section.id}
          className="rounded-lg border border-flit-border dark:border-flit-border-dark p-4 space-y-4"
        >
          <legend className="px-1 text-sm font-medium text-flit-heading dark:text-flit-heading-dark">
            {section.name}
          </legend>

          {section.fields.length === 0 ? (
            <p className="text-sm text-flit-muted">Sin campos en esta sección.</p>
          ) : (
            section.fields.map((field) => {
              const inputId = `${baseId}-${field.slug}`;
              return (
                <FlitFormField
                  key={field.id}
                  label={field.name}
                  htmlFor={inputId}
                  required={field.isRequired}
                >
                  {renderFieldInput(field, inputId, values[field.slug], onChange)}
                </FlitFormField>
              );
            })
          )}
        </fieldset>
      ))}
    </section>
  );
}
