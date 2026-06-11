import type { DateRangeValue } from "../lib/dateRange.js";

interface DateRangeFilterProps {
  value: DateRangeValue;
  onChange: (next: DateRangeValue) => void;
}

export function DateRangeFilter({ value, onChange }: DateRangeFilterProps) {
  function handleFromChange(e: React.ChangeEvent<HTMLInputElement>) {
    onChange({ ...value, from: e.target.value });
  }

  function handleToChange(e: React.ChangeEvent<HTMLInputElement>) {
    onChange({ ...value, to: e.target.value });
  }

  return (
    <div
      className="flit-toolbar"
      role="search"
      aria-label="Filtro de rango de fechas del dashboard"
    >
      <div className="flit-field">
        <label htmlFor="dashboard-date-from" className="flit-label">
          Desde
        </label>
        <input
          id="dashboard-date-from"
          type="date"
          value={value.from}
          onChange={handleFromChange}
          className="rounded-lg border border-flit-border bg-white px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-900"
          aria-label="Fecha inicial del período"
        />
      </div>

      <div className="flit-field">
        <label htmlFor="dashboard-date-to" className="flit-label">
          Hasta
        </label>
        <input
          id="dashboard-date-to"
          type="date"
          value={value.to}
          onChange={handleToChange}
          min={value.from || undefined}
          className="rounded-lg border border-flit-border bg-white px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-900"
          aria-label="Fecha final del período"
        />
      </div>
    </div>
  );
}
