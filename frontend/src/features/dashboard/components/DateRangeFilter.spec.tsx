import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { DateRangeFilter } from "./DateRangeFilter.js";

describe("DateRangeFilter — AC3", () => {
  it("propaga cambio de fecha desde al padre", () => {
    const onChange = vi.fn();
    render(
      <DateRangeFilter
        value={{ from: "2026-01-01", to: "2026-06-30" }}
        onChange={onChange}
      />,
    );

    const fromInput = screen.getByLabelText(/fecha inicial del período/i);
    fireEvent.change(fromInput, { target: { value: "2026-01-15" } });

    expect(onChange).toHaveBeenCalledWith({
      from: "2026-01-15",
      to: "2026-06-30",
    });
  });

  it("propaga cambio de fecha hasta al padre", () => {
    const onChange = vi.fn();
    render(
      <DateRangeFilter
        value={{ from: "2026-01-01", to: "2026-06-30" }}
        onChange={onChange}
      />,
    );

    const toInput = screen.getByLabelText(/fecha final del período/i);
    fireEvent.change(toInput, { target: { value: "2026-01-31" } });

    expect(onChange).toHaveBeenCalledWith({
      from: "2026-01-01",
      to: "2026-01-31",
    });
  });

  it("expone labels accesibles para ambos campos de fecha", () => {
    render(
      <DateRangeFilter
        value={{ from: "2026-01-01", to: "2026-01-31" }}
        onChange={vi.fn()}
      />,
    );
    expect(screen.getByLabelText(/fecha inicial del período/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/fecha final del período/i)).toBeInTheDocument();
  });
});
