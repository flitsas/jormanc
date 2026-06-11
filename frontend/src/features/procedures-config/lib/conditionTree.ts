export type ConditionOperator = "AND" | "OR";

export interface ConditionLeaf {
  id: string;
  field: string;
  op: string;
  value: string;
}

export interface ConditionTreeJson {
  operator: ConditionOperator;
  nodes: Array<{ field: string; op: string; value: string }>;
}

export function buildConditionTree(
  operator: ConditionOperator,
  leaves: ConditionLeaf[],
): ConditionTreeJson {
  return {
    operator,
    nodes: leaves
      .filter((l) => l.field.trim())
      .map(({ field, op, value }) => ({ field: field.trim(), op, value: value.trim() })),
  };
}

export function newConditionLeaf(): ConditionLeaf {
  return {
    id: crypto.randomUUID(),
    field: "",
    op: "==",
    value: "",
  };
}
