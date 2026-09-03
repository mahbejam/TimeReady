/** Converts a picker value to the `yyyy-MM-dd` string the API expects. */
export function toIsoDate(value: Date | null): string | null {
  if (!value) {
    return null;
  }

  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');

  return `${value.getFullYear()}-${month}-${day}`;
}

/** Parses `yyyy-MM-dd` into a local date, avoiding the UTC shift of `new Date(string)`. */
export function fromIsoDate(value: string | null): Date | null {
  if (!value) {
    return null;
  }

  const [year, month, day] = value.split('-').map(Number);

  return new Date(year, month - 1, day);
}

/** Whole days from today until the given date. Negative when it is in the past. */
export function daysUntil(value: string | null): number | null {
  const date = fromIsoDate(value);

  if (!date) {
    return null;
  }

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  return Math.round((date.getTime() - today.getTime()) / 86_400_000);
}
