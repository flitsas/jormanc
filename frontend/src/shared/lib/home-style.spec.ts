import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  DEFAULT_HOME_STYLE_ID,
  HOME_STYLE_DATA_ATTR,
  HOME_STYLE_IDS,
  HOME_STYLE_PRESETS,
  HOME_STYLE_STORAGE_KEY,
  applyHomeStyle,
  getHomeStylePreset,
  initHomeStyleFromStorage,
  isHomeStyleId,
  persistHomeStyle,
  readStoredHomeStyle,
  resolveHomeStyleId,
} from "./home-style.js";

describe("HU 9241 — AC2 useHomeStyle / localStorage", () => {
  beforeEach(() => {
    localStorage.clear();
    delete document.documentElement.dataset.homeStyle;
  });

  afterEach(() => {
    localStorage.clear();
  });

  it("valida clave flit-home-style y fallback a classic si el valor es inválido", () => {
    localStorage.setItem(HOME_STYLE_STORAGE_KEY, "not-a-real-preset");
    expect(readStoredHomeStyle()).toBe("classic");
    expect(localStorage.getItem(HOME_STYLE_STORAGE_KEY)).toBe("not-a-real-preset");
    initHomeStyleFromStorage();
    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("classic");
  });

  it("persistHomeStyle escribe y readStoredHomeStyle lee corporate", () => {
    persistHomeStyle("corporate");
    expect(readStoredHomeStyle()).toBe("corporate");
  });
});

describe("home-style catalog", () => {
  beforeEach(() => {
    localStorage.clear();
    delete document.documentElement.dataset[HOME_STYLE_DATA_ATTR];
  });

  afterEach(() => {
    localStorage.clear();
    delete document.documentElement.dataset[HOME_STYLE_DATA_ATTR];
    vi.restoreAllMocks();
  });

  it("exposes at least three presets: classic, corporate, high-contrast", () => {
    expect(HOME_STYLE_PRESETS.length).toBeGreaterThanOrEqual(3);
    expect(HOME_STYLE_IDS).toEqual(["classic", "corporate", "high-contrast"]);
    for (const id of HOME_STYLE_IDS) {
      const preset = getHomeStylePreset(id);
      expect(preset.id).toBe(id);
      expect(preset.label.length).toBeGreaterThan(0);
      expect(preset.preview.primary).toMatch(/^#[0-9A-Fa-f]{6}$/);
    }
  });

  it("resolveHomeStyleId falls back to classic for unknown values", () => {
    expect(resolveHomeStyleId(null)).toBe("classic");
    expect(resolveHomeStyleId("unknown-preset")).toBe(DEFAULT_HOME_STYLE_ID);
    expect(resolveHomeStyleId("corporate")).toBe("corporate");
  });

  it("readStoredHomeStyle uses localStorage when valid", () => {
    localStorage.setItem(HOME_STYLE_STORAGE_KEY, "high-contrast");
    expect(readStoredHomeStyle()).toBe("high-contrast");
  });

  it("applyHomeStyle sets data-home-style on documentElement", () => {
    applyHomeStyle("corporate");
    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("corporate");
  });

  it("isHomeStyleId rejects invalid identifiers", () => {
    expect(isHomeStyleId("classic")).toBe(true);
    expect(isHomeStyleId("not-a-preset")).toBe(false);
  });

  it("resolveHomeStyleId does not throw for invalid input (AC2)", () => {
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => {});
    expect(() => resolveHomeStyleId("invalid")).not.toThrow();
    expect(consoleError).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("persistHomeStyle writes flit-home-style key", () => {
    persistHomeStyle("corporate");
    expect(localStorage.getItem(HOME_STYLE_STORAGE_KEY)).toBe("corporate");
  });

  it("initHomeStyleFromStorage applies dataset from storage", () => {
    localStorage.setItem(HOME_STYLE_STORAGE_KEY, "high-contrast");
    const id = initHomeStyleFromStorage();
    expect(id).toBe("high-contrast");
    expect(document.documentElement.dataset[HOME_STYLE_DATA_ATTR]).toBe("high-contrast");
  });
});
