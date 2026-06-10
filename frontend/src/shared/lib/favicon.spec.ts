import { existsSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

// Uso de ejemplo: validar que index.html y public/favicon.svg cumplen AC de HU #9393

const indexHtml = readFileSync(resolve(process.cwd(), "index.html"), "utf8");
const faviconPath = resolve(process.cwd(), "public/favicon.svg");

describe("favicon (HU #9393)", () => {
  it("AC1 — index.html referencia /favicon.svg como icono de pestaña", () => {
    expect(indexHtml).toMatch(
      /<link\s+rel="icon"\s+type="image\/svg\+xml"\s+href="\/favicon\.svg"\s*\/>/,
    );
    expect(existsSync(faviconPath)).toBe(true);
  });

  it("AC1 — favicon.svg contiene identidad FLIT (fondo azul e icono de traspaso)", () => {
    const svg = readFileSync(faviconPath, "utf8");
    expect(svg).toContain('fill="#3B82F6"');
    expect(svg).toContain('stroke="#FFFFFF"');
    expect(svg).toContain('aria-label="FLIT"');
  });

  it("AC2 — sin link roto cuando favicon.svg existe en public/", () => {
    expect(existsSync(faviconPath)).toBe(true);
    expect(indexHtml).toContain('href="/favicon.svg"');
  });

  it("AC3 — index.html conserva el título base de la aplicación", () => {
    expect(indexHtml).toContain("<title>FLIT — Equipo de Desarrollo</title>");
  });

  it("AC3 — favicon no altera el elemento title", () => {
    const titleMatch = indexHtml.match(/<title>[^<]+<\/title>/);
    expect(titleMatch?.[0]).toBe("<title>FLIT — Equipo de Desarrollo</title>");
  });

  it("AC4 — index.html incluye apple-touch-icon apuntando al mismo asset", () => {
    expect(indexHtml).toMatch(/<link\s+rel="apple-touch-icon"\s+href="\/favicon\.svg"\s*\/>/);
  });
});
