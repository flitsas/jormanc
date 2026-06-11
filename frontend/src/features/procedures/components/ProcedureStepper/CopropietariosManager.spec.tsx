import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CopropietariosManager } from "./CopropietariosManager.js";
import { CuotaValidationError } from "../../api/procedures.api.js";

describe("CopropietariosManager — AC3", () => {
  it("muestra error inline CUOTA_SUM_EXCEEDS_100 y deshabilita Continuar", async () => {
    const user = userEvent.setup();
    const onAddActor = vi.fn().mockRejectedValue(new CuotaValidationError(80, 30));

    render(
      <CopropietariosManager
        procedureId="00000000-0000-0000-0000-000000000001"
        actorDefinitionId="00000000-0000-0000-0000-000000000099"
        entries={[]}
        onEntriesChange={vi.fn()}
        onAddActor={onAddActor}
        onContinue={vi.fn()}
      />,
    );

    await user.type(screen.getByLabelText(/número de documento/i), "12345678");
    const cuotaInput = screen.getByLabelText(/cuota/i);
    await user.clear(cuotaInput);
    await user.type(cuotaInput, "30");
    await user.click(screen.getByRole("button", { name: /agregar copropietario/i }));

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent(/supera el 100%/i);
    });

    expect(screen.getByRole("button", { name: /continuar/i })).toBeDisabled();
  });
});
