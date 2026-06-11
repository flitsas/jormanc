import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { AttachmentsPanel } from "./AttachmentsPanel.js";
import type { AttachmentListItem } from "../api/procedures.schemas.js";

const ATTACHMENTS: AttachmentListItem[] = [
  {
    id: "00000000-0000-0000-0000-0000000000a1",
    fileName: "licencia.pdf",
    labelSlug: "licencia_transito",
    sizeBytes: 204800,
    uploadedAt: "2026-02-01T14:30:00Z",
  },
  {
    id: "00000000-0000-0000-0000-0000000000a2",
    fileName: "soat.pdf",
    labelSlug: "soat",
    sizeBytes: 102400,
    uploadedAt: "2026-02-01T15:00:00Z",
  },
];

const defaultProps = {
  attachments: [] as AttachmentListItem[],
  isLoading: false,
  error: null,
  isUploading: false,
  uploadError: null,
  onRetry: vi.fn(),
  onUpload: vi.fn(),
};

describe("AttachmentsPanel — AC3", () => {
  it("lista adjuntos agrupados por etiqueta con nombre, tamaño y fecha", () => {
    render(<AttachmentsPanel {...defaultProps} attachments={ATTACHMENTS} />);

    expect(screen.getByText("licencia_transito")).toBeInTheDocument();
    expect(screen.getByText("soat")).toBeInTheDocument();
    expect(screen.getByText("licencia.pdf")).toBeInTheDocument();
    expect(screen.getByText(/200\.0 KB/i)).toBeInTheDocument();
    expect(screen.getByText("soat.pdf")).toBeInTheDocument();
  });

  it("muestra formulario de upload con label_slug", () => {
    render(<AttachmentsPanel {...defaultProps} />);
    expect(screen.getByLabelText(/etiqueta/i)).toHaveValue("licencia_transito");
    expect(screen.getByRole("button", { name: /subir adjunto/i })).toBeInTheDocument();
  });
});
