/** Extrae dígitos de un valor mostrado en input (moneda COP con separadores). */
export function parseCopDigitsFromInputValue(raw: string): number | null {
  const digits = raw.replace(/\D/g, "");
  if (digits.length === 0) {
    return null;
  }
  const parsed = Number(digits);
  return Number.isFinite(parsed) ? parsed : null;
}
