import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ConditionTreeEditor } from "./ConditionTreeEditor.js";
import { newConditionLeaf } from "../lib/conditionTree.js";

describe("ConditionTreeEditor", () => {
  it("AC1 muestra árbol con operador OR y nodos", () => {
    render(
      <ConditionTreeEditor
        operator="OR"
        leaves={[
          { ...newConditionLeaf(), field: "actor.nature", op: "==", value: "juridica" },
          {
            ...newConditionLeaf(),
            field: "vehicle.restrictions",
            op: "Contains",
            value: "EMBARGO",
          },
        ]}
        onOperatorChange={vi.fn()}
        onLeavesChange={vi.fn()}
      />,
    );
    expect(screen.getByLabelText(/json de condiciones generado/i)).toHaveTextContent(
      '"operator": "OR"',
    );
    expect(screen.getByLabelText(/json de condiciones generado/i)).toHaveTextContent(
      "actor.nature",
    );
  });

  it("AC1 cambia operador a AND", async () => {
    const user = userEvent.setup();
    const onOperatorChange = vi.fn();
    render(
      <ConditionTreeEditor
        operator="OR"
        leaves={[{ ...newConditionLeaf(), field: "a", op: "==", value: "1" }]}
        onOperatorChange={onOperatorChange}
        onLeavesChange={vi.fn()}
      />,
    );
    await user.selectOptions(screen.getByLabelText(/operador lógico raíz/i), "AND");
    expect(onOperatorChange).toHaveBeenCalledWith("AND");
  });
});
