import { useState } from "react";
import { useCreateRuleSet, useSimulateCoherence } from "../api/procedures-config.api.js";
import {
  buildConditionTree,
  newConditionLeaf,
  type ConditionLeaf,
  type ConditionOperator,
} from "../lib/conditionTree.js";
import { CoherenceSimulatorPanel } from "./CoherenceSimulatorPanel.js";
import { ConditionTreeEditor } from "./ConditionTreeEditor.js";

interface RulesBuilderProps {
  procedureTypeId: string;
}

export function RulesBuilder({ procedureTypeId }: RulesBuilderProps) {
  const [ruleName, setRuleName] = useState("Nueva regla");
  const [operator, setOperator] = useState<ConditionOperator>("OR");
  const [leaves, setLeaves] = useState<ConditionLeaf[]>([
    { ...newConditionLeaf(), field: "actor.nature", op: "==", value: "juridica" },
    { ...newConditionLeaf(), field: "vehicle.restrictions", op: "Contains", value: "EMBARGO" },
  ]);
  const [actionType, setActionType] = useState<"show" | "hide">("hide");
  const [actionTarget, setActionTarget] = useState("field.firma_natural");
  const [isCoherent, setIsCoherent] = useState<boolean | null>(null);
  const [conflicts, setConflicts] = useState<
    Array<{ rule1: string; rule2: string; description: string }>
  >([]);

  const simulate = useSimulateCoherence(procedureTypeId);
  const createRule = useCreateRuleSet(procedureTypeId);

  const conditions = buildConditionTree(operator, leaves);
  const actions = [{ type: actionType, target: actionTarget }];

  const canSave = isCoherent === true && !createRule.isPending;

  async function handleVerify() {
    const result = await simulate.mutateAsync({
      name: ruleName,
      conditions,
      actions,
    });
    setIsCoherent(result.isCoherent);
    setConflicts(result.conflicts);
  }

  async function handleSave() {
    if (!canSave) return;
    await createRule.mutateAsync({ name: ruleName, conditions, actions });
    setIsCoherent(null);
    setConflicts([]);
  }

  return (
    <div className="space-y-4" aria-label="Diseñador de reglas">
      <label className="block text-sm">
        <span className="font-medium">Nombre de la regla</span>
        <input
          type="text"
          value={ruleName}
          onChange={(e) => {
            setRuleName(e.target.value);
            setIsCoherent(null);
          }}
          className="mt-1 w-full max-w-md rounded-lg border border-flit-border px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
        />
      </label>

      <ConditionTreeEditor
        operator={operator}
        leaves={leaves}
        onOperatorChange={(op) => {
          setOperator(op);
          setIsCoherent(null);
        }}
        onLeavesChange={(next) => {
          setLeaves(next);
          setIsCoherent(null);
        }}
      />

      <fieldset className="rounded-lg border border-flit-border p-3 dark:border-flit-border-dark">
        <legend className="px-1 text-sm font-medium">Acción UI</legend>
        <div className="flex flex-wrap gap-3">
          <select
            value={actionType}
            onChange={(e) => {
              setActionType(e.target.value as "show" | "hide");
              setIsCoherent(null);
            }}
            aria-label="Tipo de acción"
            className="rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
          >
            <option value="show">show</option>
            <option value="hide">hide</option>
          </select>
          <input
            type="text"
            value={actionTarget}
            onChange={(e) => {
              setActionTarget(e.target.value);
              setIsCoherent(null);
            }}
            aria-label="Target de la acción"
            placeholder="field.firma_natural"
            className="flex-1 min-w-[200px] rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
          />
        </div>
      </fieldset>

      <CoherenceSimulatorPanel
        isCoherent={isCoherent}
        conflicts={conflicts}
        isVerifying={simulate.isPending}
        onVerify={() => void handleVerify()}
      />

      <button
        type="button"
        disabled={!canSave}
        onClick={() => void handleSave()}
        className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-40"
      >
        Guardar regla
      </button>
    </div>
  );
}
