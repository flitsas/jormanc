export const CUOTA_SUM_ERROR = "CUOTA_SUM_EXCEEDS_100";

export interface CuotaErrorPayload {
  error: string;
  current_sum: number;
  proposed: number;
}

export function isCuotaSumError(data: unknown): data is CuotaErrorPayload {
  if (!data || typeof data !== "object") return false;
  const record = data as Record<string, unknown>;
  return record.error === CUOTA_SUM_ERROR;
}

export function formatCuotaErrorMessage(currentSum: number, proposed: number): string {
  return `La suma de cuotas (${currentSum}% + ${proposed}%) supera el 100%. Ajuste los porcentajes antes de continuar.`;
}

export function computeCuotaSum(cuotas: number[]): number {
  return cuotas.reduce((acc, value) => acc + value, 0);
}

export function isCuotaSumValid(cuotas: number[]): boolean {
  return computeCuotaSum(cuotas) <= 100;
}
