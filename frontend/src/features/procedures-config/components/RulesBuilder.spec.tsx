import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { RulesBuilder } from "./RulesBuilder.js";

vi.mock("../api/procedures-config.api.js", () => ({
  useSimulateCoherence: () => ({
    mutateAsync: vi.fn().mockResolvedValue({
      isCoherent: false,
      conflicts: [{ rule1: "A", rule2: "B", description: "Conflicto" }],
    }),
    isPending: false,
  }),
  useCreateRuleSet: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

describe("RulesBuilder", () => {
  it("AC2 deshabilita Guardar regla tras simulación con conflictos", async () => {
    const user = userEvent.setup();
    render(<RulesBuilder procedureTypeId="00000000-0000-0000-0000-000000000099" />);

    const saveBtn = screen.getByRole("button", { name: /guardar regla/i });
    expect(saveBtn).toBeDisabled();

    await user.click(screen.getByRole("button", { name: /verificar coherencia/i }));

    expect(screen.getByText(/conflictos detectados/i)).toBeInTheDocument();
    expect(saveBtn).toBeDisabled();
  });
});
