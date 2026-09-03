import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuditEntry, PagedResult } from '../models/audit.model';

/** Reads the audit trail. The endpoints are Admin only. */
@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/audit`;

  search(page: number, pageSize: number): Observable<PagedResult<AuditEntry>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);

    return this.http.get<PagedResult<AuditEntry>>(this.baseUrl, { params });
  }
}
