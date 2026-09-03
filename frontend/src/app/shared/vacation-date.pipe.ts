import { Pipe, PipeTransform } from '@angular/core';
import { daysUntil, fromIsoDate } from '../core/util/date.util';

/**
 * Renders a vacation start as an absolute date plus how far away it is – the
 * two things HR looks at together.
 */
@Pipe({ name: 'vacationDate' })
export class VacationDatePipe implements PipeTransform {
  private readonly formatter = new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });

  transform(value: string | null): string {
    const date = fromIsoDate(value);

    if (!date) {
      return 'Not planned';
    }

    const days = daysUntil(value);
    const formatted = this.formatter.format(date);

    if (days === null) {
      return formatted;
    }

    if (days < 0) {
      return `${formatted} · started`;
    }

    if (days === 0) {
      return `${formatted} · today`;
    }

    return `${formatted} · in ${days} day${days === 1 ? '' : 's'}`;
  }
}
