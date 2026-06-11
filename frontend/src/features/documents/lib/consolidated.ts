import type { ConsolidatedPackage } from "../api/documents.schemas.js";

export function getLatestConsolidatedPackage(
  packages: ConsolidatedPackage[],
): ConsolidatedPackage | undefined {
  if (packages.length === 0) return undefined;
  return [...packages].sort((a, b) => b.version - a.version)[0];
}
