import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** Promotion settings: the shortest interval between two promotions and the default length of each stage. */
export type PromotionSettings = components["schemas"]["SettingsResponse"];

/** The settings an administrator saves. */
export type SettingsInput = components["schemas"]["SettingsRequest"];

/** The commission of a unit: its members, with the ones that belong to it by position marked, and its chair. */
export type Commission = components["schemas"]["CommissionResponse"];

/** One member of a commission. */
export type CommissionMember =
  components["schemas"]["CommissionMemberResponse"];

/** The members and chair a commission is saved with. */
export type CommissionInput = components["schemas"]["CommissionRequest"];

/** A wave as its managers see it; `stage` is 0 before it starts, 1 to 7 while a stage runs and 8 once it is over. */
export type Wave = components["schemas"]["WaveResponse"];

/** The dates and units a wave is announced or changed with. */
export type WaveInput = components["schemas"]["WaveRequest"];

/** One row of the applications of a wave. */
export type WaveApplication =
  components["schemas"]["ApplicationSummaryResponse"];

/** An application as the viewer's role allows seeing it. */
export type PromotionApplication = components["schemas"]["ApplicationResponse"];

/** What the HR enters for the formal check of an application. */
export type FormalInput = components["schemas"]["FormalRequest"];

/** A condition of the formal check an application may fail. */
export type FormalFailure = components["schemas"]["FormalFailure"];

/** A record together with the version the server tagged it with, which a save sends back. */
export type Versioned<TRecord> = TRecord & { version: number };

/** Cache key prefix of every promotion query, so one invalidation rereads what a change touched. */
export const promotionQueryKey = ["promotion"] as const;

/** Rows of one page of the wave and application lists. */
export const PROMOTION_PAGE_SIZE = 20;

/** The version a response is tagged with: the server sends it as the quoted entity tag, since bodies carry no version. */
function versionOf(response: Response): number {
  return Number(response.headers.get("ETag")?.replaceAll('"', ""));
}

function withVersion<
  TResult extends { data?: object; error?: unknown; response: Response },
>(result: TResult) {
  return { ...unwrap(result), version: versionOf(result.response) };
}

/** The settings, dropped once nothing shows them, so a change always starts from the current version. */
export const settingsQuery = queryOptions({
  queryKey: [...promotionQueryKey, "settings"],
  queryFn: async () => withVersion(await api.GET("/api/v1/promotion/settings")),
  gcTime: 0,
});

/** Replaces the settings. */
export async function saveSettings(version: number, body: SettingsInput) {
  return withVersion(
    await api.PUT("/api/v1/promotion/settings", {
      params: { header: { "If-Match": ifMatchOf(version) } },
      body,
    }),
  );
}

/** The commission of a unit; one never saved is read with version 0. */
export function commissionQuery(unitId: string) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "commission", unitId],
    queryFn: async () =>
      withVersion(
        await api.GET("/api/v1/promotion/commissions/{unitId}", {
          params: { path: { unitId } },
        }),
      ),
    gcTime: 0,
  });
}

/** Replaces the members and the chair of a commission. */
export async function saveCommission(
  unitId: string,
  version: number,
  body: CommissionInput,
) {
  return withVersion(
    await api.PUT("/api/v1/promotion/commissions/{unitId}", {
      params: {
        path: { unitId },
        header: { "If-Match": ifMatchOf(version) },
      },
      body,
    }),
  );
}

/** One page of the waves the signed-in user manages; the previous page stays on screen while the next loads. */
export function wavesQuery(page: number) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "waves", page],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/promotion/waves", {
          params: { query: { page, pageSize: PROMOTION_PAGE_SIZE } },
        }),
      ),
    placeholderData: keepPreviousData,
  });
}

/** One wave, dropped once nothing shows it, so a change always starts from the current version. */
export function waveQuery(id: string) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "wave", id],
    queryFn: async () =>
      withVersion(
        await api.GET("/api/v1/promotion/waves/{id}", {
          params: { path: { id } },
        }),
      ),
    gcTime: 0,
  });
}

/** Announces a wave. */
export async function createWave(body: WaveInput) {
  return withVersion(await api.POST("/api/v1/promotion/waves", { body }));
}

/** Replaces the dates and units of a wave. */
export async function saveWave(id: string, version: number, body: WaveInput) {
  return withVersion(
    await api.PUT("/api/v1/promotion/waves/{id}", {
      params: { path: { id }, header: { "If-Match": ifMatchOf(version) } },
      body,
    }),
  );
}

/** One page of the applications of a wave that the signed-in user may see. */
export function waveApplicationsQuery(waveId: string, page: number) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "wave", waveId, "applications", page],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/promotion/waves/{waveId}/applications", {
          params: {
            path: { waveId },
            query: { page, pageSize: PROMOTION_PAGE_SIZE },
          },
        }),
      ),
    placeholderData: keepPreviousData,
  });
}

/** One application, dropped once nothing shows it, so a change always starts from the current version. */
export function applicationQuery(id: string) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "application", id],
    queryFn: async () =>
      withVersion(
        await api.GET("/api/v1/promotion/applications/{id}", {
          params: { path: { id } },
        }),
      ),
    gcTime: 0,
  });
}

/** Enters the date of hiring or of the last promotion of an application. */
export async function saveFormal(
  id: string,
  version: number,
  body: FormalInput,
) {
  return withVersion(
    await api.PUT("/api/v1/promotion/applications/{id}/formal", {
      params: { path: { id }, header: { "If-Match": ifMatchOf(version) } },
      body,
    }),
  );
}
