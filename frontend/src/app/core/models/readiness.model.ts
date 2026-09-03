export type ReadinessSeverity = 'Info' | 'Warning' | 'Critical';

export interface ReadinessWarning {
  code: string;
  severity: ReadinessSeverity;
  message: string;
  recommendation: string;
}

export interface ReadinessResult {
  employeeId: number;
  fullName: string;
  isReady: boolean;
  status: string;
  warnings: ReadinessWarning[];
}
