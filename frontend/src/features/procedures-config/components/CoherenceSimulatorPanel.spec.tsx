import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CoherenceSimulatorPanel } from "./CoherenceSimulatorPanel.js";

describe("CoherenceSimulatorPanel", () => {
  it("AC2 muestra conflictos cuando isCoherent es false", () => {
    render(
      <CoherenceSimulatorPanel
        isCoherent={false}
        conflicts={[
          {
            rule1: "Mostrar firma",
            rule2: "Ocultar firma",
            description: "Conflicto show/hide mismo target",
          },
        ]}
        onVerify={vi.fn()}
      />,
    );
    expect(screen.getByText(/conflictos detectados/i)).toBeInTheDocument();
    expect(screen.getByText(/conflicto show\/hide mismo target/i)).toBeInTheDocument();
  });

  it("AC2 invoca onVerify al hacer clic", async () => {
    const user = userEvent.setup();
    const onVerify = vi.fn();
    render(<CoherenceSimulatorPanel isCoherent={null} conflicts={[]} onVerify={onVerify} />);
    await user.click(screen.getByRole("button", { name: /verificar coherencia/i }));
    expect(onVerify).toHaveBeenCalled();
  });
});
