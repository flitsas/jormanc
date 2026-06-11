import { useState } from "react";
import { useCreateActor, useCreateQueryRule } from "../api/procedures-config.api.js";
import type { ActorDefinition } from "../api/procedures-config.schemas.js";
import { QueryRulesEditor } from "./QueryRulesEditor.js";

interface ActorsBuilderProps {
  procedureTypeId: string;
}

export function ActorsBuilder({ procedureTypeId }: ActorsBuilderProps) {
  const [actors, setActors] = useState<ActorDefinition[]>([]);
  const [selectedActorId, setSelectedActorId] = useState<string | null>(null);
  const [newRole, setNewRole] = useState("vendedor");

  const createActor = useCreateActor(procedureTypeId);
  const createQueryRule = useCreateQueryRule(procedureTypeId);

  const selectedActor = actors.find((a) => a.id === selectedActorId) ?? null;

  async function handleAddActor() {
    const created = await createActor.mutateAsync({
      role: newRole,
      allowedNature: "ambas",
      minCount: 1,
      maxCount: 1,
    });
    setActors((prev) => [...prev, created]);
    setSelectedActorId(created.id);
  }

  return (
    <div className="space-y-4" aria-label="Definición de actores">
      <div className="flex flex-wrap items-end gap-2">
        <label className="text-sm">
          <span className="font-medium">Rol</span>
          <input
            type="text"
            value={newRole}
            onChange={(e) => setNewRole(e.target.value)}
            className="mt-1 block rounded border border-flit-border px-2 py-1 dark:border-flit-border-dark dark:bg-flit-surface-dark"
          />
        </label>
        <button
          type="button"
          disabled={createActor.isPending}
          onClick={() => void handleAddActor()}
          className="rounded-lg bg-flit-primary px-3 py-2 text-sm font-semibold text-white disabled:opacity-50"
        >
          Agregar actor
        </button>
      </div>

      {actors.length > 0 && (
        <ul className="flex flex-wrap gap-2" role="list">
          {actors.map((actor) => (
            <li key={actor.id}>
              <button
                type="button"
                onClick={() => setSelectedActorId(actor.id)}
                className={[
                  "rounded-full px-3 py-1 text-sm",
                  selectedActorId === actor.id
                    ? "bg-flit-primary text-white"
                    : "border border-flit-border dark:border-flit-border-dark",
                ].join(" ")}
              >
                {actor.role}
              </button>
            </li>
          ))}
        </ul>
      )}

      {selectedActor && (
        <QueryRulesEditor
          actorRole={selectedActor.role}
          isSaving={createQueryRule.isPending}
          onSave={async (verifications) => {
            await createQueryRule.mutateAsync({
              actorId: selectedActor.id,
              subjectType: "persona_natural",
              entryKey: "document_number",
              isBlocking: true,
              verifications,
            });
          }}
        />
      )}
    </div>
  );
}
