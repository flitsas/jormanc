import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { FlitModal } from "./FlitModal.js";

describe("FlitModal", () => {
  it("renders dialog with title, subtitle and shared modal classes", () => {
    const { container } = render(
      <FlitModal title="Nuevo registro" subtitle="Complete los campos" onClose={() => {}}>
        <p>Contenido del formulario</p>
      </FlitModal>,
    );

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByText("Nuevo registro")).toHaveAttribute("id", "flit-modal-title");
    expect(screen.getByText("Complete los campos")).toBeInTheDocument();
    expect(screen.getByText("Contenido del formulario")).toBeInTheDocument();
    expect(container.querySelector(".flit-modal")).not.toBeNull();
    expect(container.querySelector(".flit-modal-overlay")).not.toBeNull();
  });

  it("invokes onClose when close button is clicked", async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();

    render(
      <FlitModal title="Editar" onClose={onClose} closeLabel="Cerrar modal">
        <span>Body</span>
      </FlitModal>,
    );

    await user.click(screen.getByRole("button", { name: "Cerrar modal" }));
    expect(onClose).toHaveBeenCalledOnce();
  });
});
