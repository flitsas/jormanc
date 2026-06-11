import type { FormField, FormSection } from "../api/procedures-config.schemas.js";
import { fieldTypeIcon, fieldTypeLabel } from "../lib/fieldTypeIcon.js";

interface SectionEditorProps {
  section: FormSection;
  onEditField: (field: FormField) => void;
}

export function SectionEditor({ section, onEditField }: SectionEditorProps) {
  const fields = [...section.fields].sort((a, b) => a.orderIndex - b.orderIndex);

  return (
    <div className="rounded-lg border border-flit-border p-4 dark:border-flit-border-dark">
      <h3 className="font-semibold text-flit-heading dark:text-flit-heading-dark">
        {section.name}
      </h3>
      <p className="text-xs text-flit-muted">{section.slug}</p>

      {fields.length === 0 ? (
        <p className="mt-3 text-sm text-flit-muted" role="status">
          Sin campos en esta sección.
        </p>
      ) : (
        <ul className="mt-3 space-y-2" aria-label={`Campos de ${section.name}`}>
          {fields.map((field) => (
            <li key={field.id}>
              <button
                type="button"
                onClick={() => onEditField(field)}
                className="flex w-full items-center gap-3 rounded-lg border border-flit-border px-3 py-2 text-left text-sm hover:bg-flit-surface dark:border-flit-border-dark dark:hover:bg-flit-surface-dark"
              >
                <i
                  className={`pi ${fieldTypeIcon(field.fieldType)} text-flit-primary`}
                  aria-hidden="true"
                  title={fieldTypeLabel(field.fieldType)}
                />
                <span className="font-medium">{field.name}</span>
                <span className="text-xs text-flit-muted">{field.slug}</span>
                {field.isRequired && (
                  <span className="ml-auto text-xs text-amber-600">requerido</span>
                )}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
