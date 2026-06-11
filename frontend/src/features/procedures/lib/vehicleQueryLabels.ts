const VEHICLE_QUERY_LABELS: Record<string, string> = {
  placa: "Placa",
  vin: "VIN / Número de chasis",
};

export function getVehicleQueryLabel(vehicleQueryKey: string): string {
  return VEHICLE_QUERY_LABELS[vehicleQueryKey.toLowerCase()] ?? "Identificador del vehículo";
}
