import { useState } from "react";
import { useParams } from "react-router-dom";
import {
  useProcedureType,
  useUpdateApiConnector,
  useUpdateFormField,
  useUpdateProcedureStep,
} from "../api/procedures-config.api.js";
import type { FormField } from "../api/procedures-config.schemas.js";
import { ActorsBuilder } from "../components/ActorsBuilder.js";
import { ApiConnectorsPanel } from "../components/ApiConnectorsPanel.js";
import { FieldEditor } from "../components/FieldEditor.js";
import { PipelineBuilder } from "../components/PipelineBuilder.js";
import { RulesBuilder } from "../components/RulesBuilder.js";
import { SectionEditor } from "../components/SectionEditor.js";

export function ProcedureTypeEditorPage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, error, refetch } = useProcedureType(id);
  const updateStep = useUpdateProcedureStep(id ?? "");
  const updateConnector = useUpdateApiConnector(id ?? "");

  const [selectedStepId, setSelectedStepId] = useState<string | null>(null);
  const [editingField, setEditingField] = useState<{
    field: FormField;
    stepId: string;
    sectionId: string;
  } | null>(null);

  const updateField = useUpdateFormField(
    id ?? "",
    editingField?.stepId ?? "",
    editingField?.sectionId ?? "",
  );

  const selectedStep =
    data?.steps.find((s) => s.id === selectedStepId) ??
    data?.steps.slice().sort((a, b) => a.orderIndex - b.orderIndex)[0] ??
    null;

  if (isLoading) {
    return (
      <div className="p-6" aria-busy="true" aria-label="Cargando tipo de trámite">
        <div className="h-8 w-48 animate-pulse rounded bg-flit-border dark:bg-flit-border-dark" />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="p-6">
        <p className="text-red-600" role="alert">
          {error?.message ?? "Tipo de trámite no encontrado."}
        </p>
        <button
          type="button"
          onClick={() => void refetch()}
          className="mt-2 text-sm text-flit-primary hover:underline"
        >
          Reintentar
        </button>
      </div>
    );
  }

  return (
    <div className="p-4 sm:p-6 space-y-6">
      <header>
        <h1 className="flit-section-title">
          <i className="pi pi-sitemap text-flit-primary" aria-hidden="true" />
          {data.name}
        </h1>
        <p className="text-sm text-flit-muted">
          {data.family} · v{data.version} · {data.slug}
        </p>
      </header>

      <section aria-labelledby="pipeline-heading">
        <h2
          id="pipeline-heading"
          className="mb-3 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
        >
          Pipeline
        </h2>
        <PipelineBuilder
          steps={data.steps}
          selectedStepId={selectedStep?.id ?? null}
          onSelectStep={setSelectedStepId}
          isReordering={updateStep.isPending}
          onReorder={(stepId, newOrderIndex) => {
            updateStep.mutate({ stepId, orderIndex: newOrderIndex });
          }}
        />
      </section>

      {selectedStep && (
        <section aria-labelledby="sections-heading">
          <h2
            id="sections-heading"
            className="mb-3 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
          >
            Secciones — {selectedStep.name}
          </h2>
          <div className="grid gap-4 md:grid-cols-2">
            {selectedStep.sections
              .slice()
              .sort((a, b) => a.orderIndex - b.orderIndex)
              .map((section) => (
                <SectionEditor
                  key={section.id}
                  section={section}
                  onEditField={(field) =>
                    setEditingField({
                      field,
                      stepId: selectedStep.id,
                      sectionId: section.id,
                    })
                  }
                />
              ))}
          </div>
        </section>
      )}

      <section aria-labelledby="rules-heading">
        <h2
          id="rules-heading"
          className="mb-3 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
        >
          Reglas de negocio
        </h2>
        <RulesBuilder procedureTypeId={data.id} />
      </section>

      <section aria-labelledby="actors-heading">
        <h2
          id="actors-heading"
          className="mb-3 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
        >
          Actores y verificaciones
        </h2>
        <ActorsBuilder procedureTypeId={data.id} />
      </section>

      <section aria-labelledby="connectors-heading">
        <h2
          id="connectors-heading"
          className="mb-3 text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
        >
          Conectores API
        </h2>
        <ApiConnectorsPanel
          connectors={data.apiConnectors}
          isSaving={updateConnector.isPending}
          onSaveBindings={async (connectorId, paramBindings) => {
            await updateConnector.mutateAsync({ connectorId, paramBindings });
          }}
        />
      </section>

      {editingField && (
        <FieldEditor
          field={editingField.field}
          isSaving={updateField.isPending}
          onClose={() => setEditingField(null)}
          onSave={async (patch) => {
            await updateField.mutateAsync({
              fieldId: editingField.field.id,
              patch,
            });
          }}
        />
      )}
    </div>
  );
}
