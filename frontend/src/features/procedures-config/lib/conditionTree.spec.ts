import { describe, expect, it } from "vitest";
import { buildConditionTree } from "./conditionTree.js";

describe("buildConditionTree", () => {
  it("AC1 genera JSON con operator OR y nodes", () => {
    const tree = buildConditionTree("OR", [
      { id: "1", field: "actor.nature", op: "==", value: "juridica" },
      { id: "2", field: "vehicle.restrictions", op: "Contains", value: "EMBARGO" },
    ]);
    expect(tree).toEqual({
      operator: "OR",
      nodes: [
        { field: "actor.nature", op: "==", value: "juridica" },
        { field: "vehicle.restrictions", op: "Contains", value: "EMBARGO" },
      ],
    });
  });
});
