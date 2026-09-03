import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmployeeRequest } from '../models/employee.model';
import { ReadinessResult } from '../models/readiness.model';

@Injectable({ providedIn: 'root' })
export class ReadinessService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/readiness`;

  list(): Observable<ReadinessResult[]> {
    return this.http.get<ReadinessResult[]>(this.baseUrl);
  }

  /** Evaluates data that has not been saved yet – used by the employee form. */
  evaluate(request: EmployeeRequest): Observable<ReadinessResult> {
    return this.http.post<ReadinessResult>(`${this.baseUrl}/evaluate`, request);
  }
}
