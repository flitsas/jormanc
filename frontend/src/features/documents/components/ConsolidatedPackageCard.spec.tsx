import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ConsolidatedPackageCard } from "./ConsolidatedPackageCard.js";
import type { ConsolidatedPackage } from "../api/documents.schemas.js";

vi.mock("../api/documents.api.js", () => ({
  downloadConsolidatedPdf: vi.fn(),
  fetchConsolidatedPdfBlob: vi.fn(),
}));

const PACKAGE: ConsolidatedPackage = {
  version: 3,
  mergedFileRef: "consolidated/v3.pdf",
  createdAt: "2026-06-10T14:30:00Z",
  downloadFilename: "TRAMITE_ABC123_traspaso_20260610.pdf",
  docCount: 5,
};

describe("ConsolidatedPackageCard — AC2 descarga y AC3 visor", () => {
  it("muestra versión, conteo y botones de descarga y visor cuando hay paquete", () => {
    render(
      <ConsolidatedPackageCard
        procedureId="00000000-0000-0000-0000-000000000099"
        latestPackage={PACKAGE}
        isConsolidating={false}
        consolidateError={null}
        onConsolidate={vi.fn()}
      />,
    );

    expect(screen.getByText(/Versión:/)).toBeInTheDocument();
    expect(screen.getByText("3")).toBeInTheDocument();
    expect(screen.getByText("5")).toBeInTheDocument();
    expect(screen.getByText(PACKAGE.downloadFilename)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /descargar pdf/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /ver pdf/i })).toBeInTheDocument();
  });

  it("muestra estado vacío y acción de consolidación cuando no hay paquete", () => {
    const onConsolidate = vi.fn();
    render(
      <ConsolidatedPackageCard
        procedureId="00000000-0000-0000-0000-000000000099"
        latestPackage={undefined}
        isConsolidating={false}
        consolidateError={null}
        onConsolidate={onConsolidate}
      />,
    );

    expect(screen.getByRole("status")).toHaveTextContent(/consolidación pendiente/i);
    expect(screen.getByRole("button", { name: /forzar consolidación/i })).toBeInTheDocument();
  });

  it("dispara onConsolidate al hacer clic en reconsolidar", async () => {
    const user = userEvent.setup();
    const onConsolidate = vi.fn();
    render(
      <ConsolidatedPackageCard
        procedureId="00000000-0000-0000-0000-000000000099"
        latestPackage={PACKAGE}
        isConsolidating={false}
        consolidateError={null}
        onConsolidate={onConsolidate}
      />,
    );

    await user.click(screen.getByRole("button", { name: /reconsolidar/i }));
    expect(onConsolidate).toHaveBeenCalledOnce();
  });
});
