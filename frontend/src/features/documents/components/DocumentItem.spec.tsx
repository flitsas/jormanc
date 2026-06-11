import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DocumentItem } from "./DocumentItem.js";
import type { ProcedureDocumentItem } from "../api/documents.schemas.js";

const BASE_DOC: ProcedureDocumentItem = {
  id: "00000000-0000-0000-0000-000000000001",
  documentType: {
    id: "00000000-0000-0000-0000-0000000000d1",
    name: "Contrato de compraventa",
    loadType: "generacion",
  },
  origin: "generated",
  status: "ready",
  templateVersion: 2,
  fileRef: "docs/contrato.pdf",
  generatedAt: "2026-06-01T10:00:00Z",
  uploadedBy: null,
  isRequired: true,
  orderIndex: 1,
};

describe("DocumentItem — AC1 estado documental", () => {
  it("muestra nombre, origen, versión de plantilla y badge de estado listo", () => {
    render(<DocumentItem document={BASE_DOC} />);

    expect(screen.getByText("Contrato de compraventa")).toBeInTheDocument();
    expect(screen.getByText(/Generado · Plantilla v2/i)).toBeInTheDocument();
    expect(screen.getByText("Listo")).toBeInTheDocument();
  });

  it("marca documento pendiente con badge accesible", () => {
    render(
      <DocumentItem
        document={{
          ...BASE_DOC,
          status: "pending",
          templateVersion: null,
          generatedAt: null,
          origin: "uploaded",
          documentType: { ...BASE_DOC.documentType, loadType: "carga", name: "SOAT" },
        }}
      />,
    );

    expect(screen.getByText("SOAT")).toBeInTheDocument();
    expect(screen.getByText("Cargado")).toBeInTheDocument();
    expect(screen.getByText("Pendiente")).toBeInTheDocument();
  });
});
