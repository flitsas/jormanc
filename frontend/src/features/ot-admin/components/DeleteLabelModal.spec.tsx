import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DeleteLabelModal } from "./DeleteLabelModal.js";
import type { OtDocumentLabel } from "../api/ot-admin.schemas.js";

const LABEL: OtDocumentLabel = {
  id: "00000000-0000-0000-0000-000000000021",
  otId: "00000000-0000-0000-0000-000000000001",
  slug: "paz_y_salvo",
  displayName: "Paz y Salvo Municipal",
  isActive: true,
  createdAt: "2026-01-01T00:00:00Z",
};

describe("DeleteLabelModal", () => {
  it("habilita eliminar solo tras confirmar cuando hay impacto", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();

    render(
      <DeleteLabelModal
        label={LABEL}
        impactCount={23}
        isLoadingImpact={false}
        isDeleting={false}
        onConfirm={onConfirm}
        onClose={vi.fn()}
      />,
    );

    const deleteBtn = screen.getByRole("button", { name: /^eliminar$/i });
    expect(deleteBtn).toBeDisabled();

    await user.click(screen.getByRole("checkbox"));
    await user.click(deleteBtn);

    expect(onConfirm).toHaveBeenCalled();
  });

  it("permite eliminar sin checkbox cuando no hay impacto", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();

    render(
      <DeleteLabelModal
        label={LABEL}
        impactCount={0}
        isLoadingImpact={false}
        isDeleting={false}
        onConfirm={onConfirm}
        onClose={vi.fn()}
      />,
    );

    await user.click(screen.getByRole("button", { name: /^eliminar$/i }));
    expect(onConfirm).toHaveBeenCalled();
  });
});
