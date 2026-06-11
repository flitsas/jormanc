import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { QueryRulesEditor, VERIFICATION_TYPES } from "./QueryRulesEditor.js";

describe("QueryRulesEditor", () => {
  it("AC3 muestra todos los tipos de verificación con toggle", () => {
    render(<QueryRulesEditor actorRole="vendedor" onSave={vi.fn()} />);
    for (const type of VERIFICATION_TYPES) {
      expect(screen.getByLabelText(new RegExp(`verificación ${type}`, "i"))).toBeInTheDocument();
    }
  });

  it("AC3 guarda con liveness activo", async () => {
    const user = userEvent.setup();
    const onSave = vi.fn().mockResolvedValue(undefined);
    render(<QueryRulesEditor actorRole="vendedor" onSave={onSave} />);

    const livenessToggle = screen.getByLabelText(/verificación liveness/i);
    if (!(livenessToggle as HTMLInputElement).checked) {
      await user.click(livenessToggle);
    }

    await user.click(screen.getByRole("button", { name: /guardar verificaciones/i }));

    expect(onSave).toHaveBeenCalledWith(
      expect.arrayContaining([expect.objectContaining({ type: "liveness", isActive: true })]),
    );
  });
});
