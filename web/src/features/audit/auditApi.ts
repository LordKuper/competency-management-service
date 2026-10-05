import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** An audit event as the administrator API returns it. */
export type AuditEventResponse = components["schemas"]["AuditEventResponse"];

/** Filters of the audit journal; empty values mean no filter. */
export interface AuditFilters {
  from?: string;
  to?: string;
  actor?: string;
  action?: string;
  entityType?: string;
  entityId?: string;
  requestId?: string;
}

/** One page of events, newest first; the previous page stays on screen while the next loads. */
export function auditListQuery(
  filters: AuditFilters,
  page: number,
  pageSize: number,
) {
  return queryOptions({
    queryKey: ["audit", filters, page, pageSize],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/audit", {
          params: { query: { ...filters, page, pageSize } },
        }),
      ).data,
    placeholderData: keepPreviousData,
  });
}
