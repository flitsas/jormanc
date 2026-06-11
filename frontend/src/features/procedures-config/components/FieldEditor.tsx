import { useState } from "react";
import { FlitModal } from "../../../shared/components/ui/FlitModal.js";
import type { DropdownOption, FormField } from "../api/procedures-config.schemas.js";
import { fieldTypeLabel } from "../lib/fieldTypeIcon.js";

const FIELD_TYPES = ["text", "dropdown", "checkbox", "numeric", "attachment", "list"] as const;

interface FieldEditorProps {
  field: FormField;
  onClose: () => void;
  onSave: (patch: {
    name?: string;
    fieldType?: string;
    isRequired?: boolean;
    config?: Record<string, unknown>;
  }) => Promise<void>;
  isSaving?: boolean;
}

function parseDropdownOptions(config: FormField["config"]): DropdownOption[] {
  if (!config || typeof config !== "object" || Array.isArray(config)) return [];
  const options = (config as { options?: DropdownOption[] }).options;
  return Array.isArray(options) ? options : [];
}

export function FieldEditor({ field, onClose, onSave, isSaving = false }: FieldEditorProps) {
  const [name, setName] = useState(field.name);
  const [fieldType, setFieldType] = useState(field.fieldType);
  const [isRequired, setIsRequired] = useState(field.isRequired);
  const [options, setOptions] = useState<DropdownOption[]>(parseDropdownOptions(field.config));

  async function handleSave() {
    const config: Record<string, unknown> =
      fieldType === "dropdown"
        ? { options: options.filter((o) => o.value.trim() && o.label.trim()) }
        : (field.config as Record<string, unknown>) ?? {};

    await onSave({ name, fieldType, isRequired, config });
    onClose();
  }

  function addOption() {
    setOptions((prev) => [...prev, { value: "", label: "" }]);
  }

  function removeOption(index: number) {
    setOptions((prev) => prev.filter((_, i) => i !== index));
  }

  function updateOption(index: number, key: keyof DropdownOption, value: string) {
    setOptions((prev) => prev.map((o, i) => (i === index ? { ...o, [key]: value } : o)));
  }

  return (
    <FlitModal title="Editar campo" subtitle={field.slug} onClose={onClose} maxWidthClass="max-w-xl">
      <div className="space-y-4">
        <label className="block text-sm">
          <span className="font-medium text-flit-heading dark:text-flit-heading-dark">Nombre</span>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="mt-1 w-full rounded-lg border border-flit-border px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
          />
        </label>

        <label className="block text-sm">
          <span className="font-medium text-flit-heading dark:text-flit-heading-dark">Tipo de campo</span>
          <select
            value={fieldType}
            onChange={(e) => setFieldType(e.target.value)}
            className="mt-1 w-full rounded-lg border border-flit-border px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
          >
            {FIELD_TYPES.map((t) => (
              <option key={t} value={t}>
                {fieldTypeLabel(t)}
              </option>
            ))}
          </select>
        </label>

        {fieldType === "dropdown" && (
          <fieldset className="rounded-lg border border-flit-border p-3 dark:border-flit-border-dark">
            <legend className="px-1 text-sm font-medium text-flit-heading dark:text-flit-heading-dark">
              Opciones del dropdown
            </legend>
            <div className="space-y-2">
              {options.map((opt, index) => (
                <div key={index} className="flex gap-2">
                  <input
                    type="text"
                    placeholder="value"
                    aria-label={`Opción ${index + 1} valor`}
                    value={opt.value}
                    onChange={(e) => updateOption(index, "value", e.target.value)}
                    className="flex-1 rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
                  />
                  <input
                    type="text"
                    placeholder="label"
                    aria-label={`Opción ${index + 1} etiqueta`}
                    value={opt.label}
                    onChange={(e) => updateOption(index, "label", e.target.value)}
                    className="flex-1 rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
                  />
                  <button
                    type="button"
                    aria-label={`Eliminar opción ${index + 1}`}
                    onClick={() => removeOption(index)}
                    className="rounded p-1 text-red-600 hover:bg-red-50"
                  >
                    <i className="pi pi-trash" aria-hidden="true" />
                  </button>
                </div>
              ))}
              <button
                type="button"
                onClick={addOption}
                className="text-sm font-medium text-flit-primary hover:underline"
              >
                + Agregar opción
              </button>
            </div>
          </fieldset>
        )}

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={isRequired}
            onChange={(e) => setIsRequired(e.target.checked)}
          />
          Campo obligatorio
        </label>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-flit-border px-4 py-2 text-sm dark:border-flit-border-dark"
          >
            Cancelar
          </button>
          <button
            type="button"
            disabled={isSaving}
            onClick={() => void handleSave()}
            className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
          >
            {isSaving ? "Guardando…" : "Guardar campo"}
          </button>
        </div>
      </div>
    </FlitModal>
  );
}
