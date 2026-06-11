import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TemplateVersionsList } from "./TemplateVersionsList.js";
import type { DocumentTemplate } from "../api/documents.schemas.js";

const TEMPLATES: DocumentTemplate[] = [
  {
    templateId: "00000000-0000-0000-0000-000000000001",
    documentTypeId: "00000000-0000-0000-0000-000000000099",
    version: 3,
    status: "active",
    contentRef: "templates/t/doc/v3.html",
    notes: "Versión vigente",
    markersDetected: ["vehicle.plate"],
    createdAt: "2026-06-03T10:00:00Z",
  },
  {
    templateId: "00000000-0000-0000-0000-000000000002",
    documentTypeId: "00000000-0000-0000-0000-000000000099",
    version: 2,
    status: "deprecated",
    contentRef: "templates/t/doc/v2.html",
    notes: "Ajuste menor",
    markersDetected: ["vehicle.plate"],
    createdAt: "2026-06-02T10:00:00Z",
  },
  {
    templateId: "00000000-0000-0000-0000-000000000003",
    documentTypeId: "00000000-0000-0000-0000-000000000099",
    version: 1,
    status: "deprecated",
    contentRef: "templates/t/doc/v1.html",
    notes: null,
    markersDetected: [],
    createdAt: "2026-06-01T10:00:00Z",
  },
];

describe("TemplateVersionsList", () => {
  const defaultProps = {
    templates: [] as DocumentTemplate[],
    isLoading: false,
    error: null,
    onRetry: vi.fn(),
  };

  it("AC2 muestra versiones con badge Activa y Deprecada", () => {
    render(<TemplateVersionsList {...defaultProps} templates={TEMPLATES} />);

    expect(screen.getByText("v3")).toBeInTheDocument();
    expect(screen.getByText("v2")).toBeInTheDocument();
    expect(screen.getByText("v1")).toBeInTheDocument();

    const activeBadges = screen.getAllByText("Activa");
    const deprecatedBadges = screen.getAllByText("Deprecada");
    expect(activeBadges).toHaveLength(1);
    expect(deprecatedBadges).toHaveLength(2);

    expect(screen.getByText("Versión vigente")).toBeInTheDocument();
    expect(screen.getByText("Ajuste menor")).toBeInTheDocument();
  });

  it("shows loading skeleton (aria-busy)", () => {
    render(<TemplateVersionsList {...defaultProps} isLoading={true} />);
    expect(screen.getByLabelText(/cargando versiones/i)).toHaveAttribute("aria-busy", "true");
  });

  it("shows empty state", () => {
    render(<TemplateVersionsList {...defaultProps} />);
    expect(screen.getByText(/sin plantillas registradas/i)).toBeInTheDocument();
  });

  it("shows error state with retry", async () => {
    const onRetry = vi.fn();
    const user = userEvent.setup();
    render(
      <TemplateVersionsList
        {...defaultProps}
        error={new Error("Error de red")}
        onRetry={onRetry}
      />,
    );
    await user.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(onRetry).toHaveBeenCalled();
  });
});
