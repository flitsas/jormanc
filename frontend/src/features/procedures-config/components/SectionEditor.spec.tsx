import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { SectionEditor } from "./SectionEditor.js";
import type { FormSection } from "../api/procedures-config.schemas.js";

const SECTION: FormSection = {
  id: "00000000-0000-0000-0000-000000000020",
  stepId: "00000000-0000-0000-0000-000000000001",
  orderIndex: 1,
  slug: "datos",
  name: "Datos",
  isCollapsible: false,
  fields: [
    {
      id: "00000000-0000-0000-0000-000000000010",
      sectionId: "00000000-0000-0000-0000-000000000020",
      orderIndex: 1,
      slug: "lista",
      name: "Lista",
      fieldType: "dropdown",
      isRequired: true,
      config: { options: [] },
    },
  ],
};

describe("SectionEditor", () => {
  it("AC2 muestra ícono correcto para campo dropdown", () => {
    render(<SectionEditor section={SECTION} onEditField={vi.fn()} />);
    const icon = document.querySelector(".pi-list");
    expect(icon).toBeTruthy();
    expect(screen.getByText("Lista")).toBeInTheDocument();
  });
});
