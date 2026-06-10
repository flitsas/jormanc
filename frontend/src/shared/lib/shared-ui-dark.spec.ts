import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const indexCss = readFileSync(resolve(process.cwd(), "src/index.css"), "utf8");
const primeFlitCss = readFileSync(resolve(process.cwd(), "src/styles/prime-flit.css"), "utf8");

describe("shared UI dark theme tokens (HU 9088)", () => {
  it("defines dark overrides for flit-card and flit-list-panel", () => {
    expect(indexCss).toContain(".dark .flit-card");
    expect(indexCss).toContain(".dark .flit-list-panel");
    expect(indexCss).toContain("bg-flit-surface-dark");
    expect(indexCss).toContain("border-flit-border-dark");
  });

  it("defines distinguishable dark states for list panel (empty, loading, error)", () => {
    expect(indexCss).toContain(".dark .flit-list-panel__empty");
    expect(indexCss).toContain(".dark .flit-list-panel--loading");
    expect(indexCss).toContain(".dark .flit-list-panel__error");
    expect(indexCss).toContain(".flit-list-panel__error-title");
  });

  it("defines dark modal shell classes for shared dialogs", () => {
    expect(indexCss).toContain(".flit-modal-overlay");
    expect(indexCss).toContain(".dark .flit-modal");
    expect(indexCss).toContain(".dark .flit-modal__title");
  });

  it("defines PrimeReact dialog dark overrides for WCAG contrast", () => {
    expect(primeFlitCss).toContain("html.dark .p-dialog");
    expect(primeFlitCss).toContain("html.dark .p-dialog .p-dialog-header");
    expect(primeFlitCss).toContain("html.dark .p-dialog .p-dialog-content");
  });
});
