import { describe, expect, it, vi } from "vitest";
import { parseContentDispositionFilename, triggerBlobDownload } from "./downloadBlob.js";

describe("downloadBlob", () => {
  it("parsea filename de Content-Disposition", () => {
    expect(parseContentDispositionFilename('attachment; filename="reporte.xlsx"')).toBe(
      "reporte.xlsx",
    );
    expect(parseContentDispositionFilename("attachment; filename=resumen.pdf")).toBe("resumen.pdf");
  });

  it("dispara descarga con anchor temporal", () => {
    const click = vi.fn();
    const anchor = {
      href: "",
      download: "",
      rel: "",
      click,
      remove: vi.fn(),
    } as unknown as HTMLAnchorElement;

    const createElementSpy = vi.spyOn(document, "createElement").mockReturnValue(anchor);
    const appendSpy = vi.spyOn(document.body, "appendChild").mockImplementation(() => anchor);
    const revokeSpy = vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => {});
    const createObjectUrlSpy = vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:mock-url");

    triggerBlobDownload(new Blob(["x"]), "test.xlsx");

    expect(createObjectUrlSpy).toHaveBeenCalled();
    expect(anchor.download).toBe("test.xlsx");
    expect(click).toHaveBeenCalled();
    expect(revokeSpy).toHaveBeenCalledWith("blob:mock-url");

    createElementSpy.mockRestore();
    appendSpy.mockRestore();
    revokeSpy.mockRestore();
    createObjectUrlSpy.mockRestore();
  });
});
