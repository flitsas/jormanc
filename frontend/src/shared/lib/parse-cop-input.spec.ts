import { describe, expect, it } from "vitest";
import { parseCopDigitsFromInputValue } from "./parse-cop-input.js";

describe("parseCopDigitsFromInputValue", () => {
  it("ignora símbolos y separadores", () => {
    expect(parseCopDigitsFromInputValue("$ 32.000.000")).toBe(32_000_000);
    expect(parseCopDigitsFromInputValue("5.000.000")).toBe(5_000_000);
  });

  it("retorna null si no hay dígitos", () => {
    expect(parseCopDigitsFromInputValue("")).toBeNull();
    expect(parseCopDigitsFromInputValue("$ ")).toBeNull();
  });
});
