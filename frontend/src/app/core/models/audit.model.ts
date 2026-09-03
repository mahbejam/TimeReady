export type AuditAction = 'Created' | 'Updated' | 'Deleted';

export interface AuditEntry {
  id: number;
  entityName: string;
  entityId: string;
  action: AuditAction;
  userId: string | null;
  userName: string;
  timestampUtc: string;
  changedColumns: string[] | null;
  oldValues: Record<string, string | null> | null;
  newValues: Record<string, string | null> | null;
  traceId: string | null;
}

/** Matches PagedResult<T> from the API. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
}
