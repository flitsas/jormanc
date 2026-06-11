import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { FieldEditor } from "./FieldEditor.js";
import type { FormField } from "../api/procedures-config.schemas.js";

const FIELD: FormField = {
  id: "00000000-0000-0000-0000-000000000010",
  sectionId: "00000000-0000-0000-0000-000000000020",
  orderIndex: 1,
  slug: "tipo_doc",
  name: "Tipo documento",
  fieldType: "text",
  isRequired: false,
  config: {},
};

describe("FieldEditor", () => {
  it("AC2 muestra panel de opciones al seleccionar dropdown", async () => {
    const user = userEvent.setup();
    render(<FieldEditor field={FIELD} onClose={vi.fn()} onSave={vi.fn()} />);

    await user.selectOptions(screen.getByRole("combobox"), "dropdown");
    expect(screen.getByText(/opciones del dropdown/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /agregar opción/i })).toBeInTheDocument();
  });

  it("AC2 guarda campo dropdown con opciones", async () => {
    const user = userEvent.setup();
    const onSave = vi.fn().mockResolvedValue(undefined);
    render(<FieldEditor field={FIELD} onClose={vi.fn()} onSave={onSave} />);

    await user.selectOptions(screen.getByRole("combobox"), "dropdown");
    await user.click(screen.getByRole("button", { name: /agregar opción/i }));

    const valueInputs = screen.getAllByLabelText(/opción 1 valor/i);
    await user.type(valueInputs[0], "cc");
    await user.type(screen.getByLabelText(/opción 1 etiqueta/i), "Cédula");

    await user.click(screen.getByRole("button", { name: /guardar campo/i }));

    expect(onSave).toHaveBeenCalledWith(
      expect.objectContaining({
        fieldType: "dropdown",
        config: { options: [{ value: "cc", label: "Cédula" }] },
      }),
    );
  });
});
