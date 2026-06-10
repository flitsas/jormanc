import { describe, expect, it } from "vitest";
import { formatCopAmountInWords } from "./format-cop-amount-words.js";

describe("formatCopAmountInWords", () => {
  it("retorna vacío para valores no positivos", () => {
    expect(formatCopAmountInWords(null)).toBe("");
    expect(formatCopAmountInWords(0)).toBe("");
    expect(formatCopAmountInWords(-100)).toBe("");
  });

  it("describe montos típicos de compraventa", () => {
    expect(formatCopAmountInWords(1)).toBe("Uno peso");
    expect(formatCopAmountInWords(1000)).toBe("Mil pesos");
    expect(formatCopAmountInWords(21_500)).toBe("Veintiuno mil quinientos pesos");
    expect(formatCopAmountInWords(50_000_000)).toBe("Cincuenta millones pesos");
  });
});
