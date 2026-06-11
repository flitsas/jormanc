export interface DateRangeValue {
  from: string;
  to: string;
}

const DATE_INPUT_RE = /^\d{4}-\d{2}-\d{2}$/;

export function isValidDateInput(date: string): boolean {
  if (!DATE_INPUT_RE.test(date)) return false;
  const parsed = new Date(`${date}T00:00:00`);
  return !Number.isNaN(parsed.getTime());
}

function toIsoStart(date: string): string {
  return new Date(`${date}T00:00:00`).toISOString();
}

function toIsoEnd(date: string): string {
  return new Date(`${date}T23:59:59.999`).toISOString();
}

export function dateRangeToIso(range: DateRangeValue): { from: string; to: string } | null {
  if (!isValidDateInput(range.from) || !isValidDateInput(range.to)) {
    return null;
  }
  return {
    from: toIsoStart(range.from),
    to: toIsoEnd(range.to),
  };
}

export function defaultDateRange(): DateRangeValue {
  const now = new Date();
  const from = new Date(now.getFullYear(), now.getMonth(), 1);
  const pad = (n: number) => String(n).padStart(2, "0");
  const fmt = (d: Date) =>
    `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

  return {
    from: fmt(from),
    to: fmt(now),
  };
}
