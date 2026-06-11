import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { NO_MARKERS_WARNING, TemplateUploadModal } from "./TemplateUploadModal.js";
import type { DocumentTemplate } from "../api/documents.schemas.js";

const DOC_TYPE_ID = "00000000-0000-0000-0000-000000000010";

const UPLOAD_RESPONSE: DocumentTemplate = {
  templateId: "00000000-0000-0000-0000-000000000020",
  documentTypeId: DOC_TYPE_ID,
  version: 1,
  status: "active",
  contentRef: "templates/tenant/doc/v1.html",
  notes: null,
  markersDetected: ["actor[vendedor].full_name", "vehicle.plate"],
  createdAt: "2026-06-01T12:00:00Z",
};

function makeHtmlFile(content: string, name = "plantilla.html") {
  return new File([content], name, { type: "text/html" });
}

describe("TemplateUploadModal", () => {
  const onClose = vi.fn();
  const onUpload = vi.fn().mockResolvedValue(UPLOAD_RESPONSE);

  it("AC1 muestra marcadores detectados antes de Confirmar", async () => {
    const user = userEvent.setup();
    render(
      <TemplateUploadModal documentTypeId={DOC_TYPE_ID} onClose={onClose} onUpload={onUpload} />,
    );

    const html = "<p>{{actor[vendedor].full_name}} — placa {{vehicle.plate}}</p>";
    const input = screen.getByLabelText(/archivo html/i);
    await user.upload(input, makeHtmlFile(html));

    await waitFor(() => {
      expect(screen.getByText("{{actor[vendedor].full_name}}")).toBeInTheDocument();
      expect(screen.getByText("{{vehicle.plate}}")).toBeInTheDocument();
    });

    expect(screen.getByRole("button", { name: /confirmar/i })).toBeEnabled();
    expect(onUpload).not.toHaveBeenCalled();
  });

  it("AC1 permite confirmar o cancelar la subida", async () => {
    const user = userEvent.setup();
    render(
      <TemplateUploadModal documentTypeId={DOC_TYPE_ID} onClose={onClose} onUpload={onUpload} />,
    );

    await user.upload(
      screen.getByLabelText(/archivo html/i),
      makeHtmlFile("<span>{{vehicle.plate}}</span>"),
    );

    await waitFor(() => expect(screen.getByRole("button", { name: /confirmar/i })).toBeEnabled());

    await user.click(screen.getByRole("button", { name: /^cancelar$/i }));
    expect(onClose).toHaveBeenCalled();
    expect(onUpload).not.toHaveBeenCalled();
  });

  it("AC3 advierte cuando no hay marcadores pero mantiene Confirmar habilitado", async () => {
    const user = userEvent.setup();
    render(
      <TemplateUploadModal documentTypeId={DOC_TYPE_ID} onClose={onClose} onUpload={onUpload} />,
    );

    await user.upload(
      screen.getByLabelText(/archivo html/i),
      makeHtmlFile("<html><body>Sin marcadores</body></html>"),
    );

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent(NO_MARKERS_WARNING);
    });

    const confirm = screen.getByRole("button", { name: /confirmar/i });
    expect(confirm).toBeEnabled();

    onUpload.mockResolvedValueOnce({
      ...UPLOAD_RESPONSE,
      markersDetected: [],
    });
    await user.click(confirm);
    await waitFor(() => expect(onUpload).toHaveBeenCalled());
  });
});
