interface VehicleWarningBannerProps {
  warnings: string[];
}

export function VehicleWarningBanner({ warnings }: VehicleWarningBannerProps) {
  if (warnings.length === 0) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      className="rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-900 dark:border-amber-600 dark:bg-amber-950/40 dark:text-amber-100"
    >
      <p className="font-medium">Hallazgos informativos</p>
      <ul className="mt-1 list-disc pl-5">
        {warnings.map((warning) => (
          <li key={warning}>{warning}</li>
        ))}
      </ul>
    </div>
  );
}
