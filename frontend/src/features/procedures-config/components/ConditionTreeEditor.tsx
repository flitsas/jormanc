import {
  buildConditionTree,
  newConditionLeaf,
  type ConditionLeaf,
  type ConditionOperator,
  type ConditionTreeJson,
} from "../lib/conditionTree.js";

interface ConditionTreeEditorProps {
  operator: ConditionOperator;
  leaves: ConditionLeaf[];
  onOperatorChange: (op: ConditionOperator) => void;
  onLeavesChange: (leaves: ConditionLeaf[]) => void;
  onTreeChange?: (tree: ConditionTreeJson) => void;
}

export function ConditionTreeEditor({
  operator,
  leaves,
  onOperatorChange,
  onLeavesChange,
  onTreeChange,
}: ConditionTreeEditorProps) {
  function updateLeaves(next: ConditionLeaf[]) {
    onLeavesChange(next);
    onTreeChange?.(buildConditionTree(operator, next));
  }

  function updateLeaf(id: string, patch: Partial<ConditionLeaf>) {
    updateLeaves(leaves.map((l) => (l.id === id ? { ...l, ...patch } : l)));
  }

  return (
    <div className="space-y-3" aria-label="Editor de condiciones AND/OR">
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium text-flit-heading dark:text-flit-heading-dark">
          Operador raíz
        </span>
        <select
          value={operator}
          onChange={(e) => {
            const op = e.target.value as ConditionOperator;
            onOperatorChange(op);
            onTreeChange?.(buildConditionTree(op, leaves));
          }}
          aria-label="Operador lógico raíz"
          className="rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
        >
          <option value="AND">AND</option>
          <option value="OR">OR</option>
        </select>
      </div>

      <ul className="space-y-2" role="list">
        {leaves.map((leaf, index) => (
          <li
            key={leaf.id}
            className="rounded-lg border border-flit-border p-3 dark:border-flit-border-dark"
            aria-label={`Nodo condición ${index + 1}`}
          >
            <div className="mb-1 text-xs font-semibold text-flit-muted">
              {index > 0 ? operator : "Nodo"}
            </div>
            <div className="grid gap-2 sm:grid-cols-3">
              <input
                type="text"
                placeholder="actor.nature"
                aria-label={`Campo nodo ${index + 1}`}
                value={leaf.field}
                onChange={(e) => updateLeaf(leaf.id, { field: e.target.value })}
                className="rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
              />
              <select
                value={leaf.op}
                aria-label={`Operador nodo ${index + 1}`}
                onChange={(e) => updateLeaf(leaf.id, { op: e.target.value })}
                className="rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
              >
                <option value="==">==</option>
                <option value="!=">!=</option>
                <option value=">">&gt;</option>
                <option value="<">&lt;</option>
                <option value="Contains">Contains</option>
              </select>
              <input
                type="text"
                placeholder="valor"
                aria-label={`Valor nodo ${index + 1}`}
                value={leaf.value}
                onChange={(e) => updateLeaf(leaf.id, { value: e.target.value })}
                className="rounded border border-flit-border px-2 py-1 text-sm dark:border-flit-border-dark dark:bg-flit-surface-dark"
              />
            </div>
          </li>
        ))}
      </ul>

      <button
        type="button"
        onClick={() => updateLeaves([...leaves, newConditionLeaf()])}
        className="text-sm font-medium text-flit-primary hover:underline"
      >
        + Agregar condición
      </button>

      <pre
        className="mt-2 overflow-x-auto rounded bg-flit-surface p-2 text-xs dark:bg-flit-surface-dark"
        aria-label="JSON de condiciones generado"
      >
        {JSON.stringify(buildConditionTree(operator, leaves), null, 2)}
      </pre>
    </div>
  );
}
