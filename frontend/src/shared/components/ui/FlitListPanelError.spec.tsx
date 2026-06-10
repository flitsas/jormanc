import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { FlitListPanelError } from "./FlitListPanelError.js";

describe("FlitListPanelError", () => {
  it("renders error message with alert role and shared panel classes", () => {
    const { container } = render(
      <FlitListPanelError message="Error de red" title="Fallo al cargar" />,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText("Fallo al cargar")).toBeInTheDocument();
    expect(screen.getByText("Error de red")).toBeInTheDocument();
    expect(container.querySelector(".flit-list-panel__error")).not.toBeNull();
  });

  it("calls onRetry when retry button is clicked", async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();

    render(<FlitListPanelError message="Timeout" onRetry={onRetry} />);

    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("omits retry action when onRetry is not provided", () => {
    const { container } = render(<FlitListPanelError message="Sin conexión" />);
    expect(container.querySelector(".flit-list-panel__error-action")).toBeNull();
  });
});
