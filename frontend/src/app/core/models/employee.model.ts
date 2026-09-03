export interface Employee {
  id: number;
  fullName: string;
  timeBalanceHours: number;
  remainingVacationDays: number;
  /** ISO date, e.g. 2026-08-10, or null when no vacation is planned. */
  vacationStartDate: string | null;
  managerInformed: boolean;
  handoverCompleted: boolean;
}

export type EmployeeRequest = Omit<Employee, 'id'>;
