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

/** The stage a wave is sent back to and its new end dates. */
export type RollbackInput = components["schemas"]["RollbackRequest"];

/** What sending a wave back to a stage resets or computes again. */
export type RollbackPreview = components["schemas"]["RollbackPreviewResponse"];

/** What sending a wave back to a stage changes in one kind of way. */
export type RollbackEffectKind = components["schemas"]["RollbackEffectKind"];

/** One row of the applications of a wave. */
export type WaveApplication =
  components["schemas"]["ApplicationSummaryResponse"];

/** An application as the viewer's role allows seeing it. */
export type PromotionApplication = components["schemas"]["ApplicationResponse"];

/** What the HR enters for the formal check of an application. */
export type FormalInput = components["schemas"]["FormalRequest"];

/** A condition of the formal check an application may fail. */
export type FormalFailure = components["schemas"]["FormalFailure"];

/** A wave that takes applications or is announced, with the targets a candidate may aim at. */
export type OpenWave = components["schemas"]["OpenWaveResponse"];

/** What a candidate files an application with. */
export type ApplicationInput = components["schemas"]["ApplicationRequest"];

/** The levels and competencies of one target of a wave, which an application is filled in by. */
export type TargetForm = components["schemas"]["TargetFormResponse"];

/** A competency to score with the points of its scale. */
export type ScoredCompetency =
  components["schemas"]["TargetFormCompetencyResponse"];

/** A request to rate a candidate. */
export type RatingTask = components["schemas"]["RatingTaskResponse"];

/** What a rater sees of a candidate: the name and the competencies, with their own answers. */
export type Rating = components["schemas"]["RatingResponse"];

/** The answers a rater saves; a null score is «Не наблюдал». */
export type RatingAnswer = components["schemas"]["RatingAnswerInput"];

/** The artifact a candidate gives for one competency. */
export type ArtifactInput = components["schemas"]["ArtifactInput"];

/** The card of a candidate for the commission: what the candidate, the raters and the chair gave, and the rating the chair's scores earn. */
export type Review = components["schemas"]["ReviewResponse"];

/** The final scores and the decision the chair saves. */
export type ReviewInput = components["schemas"]["ReviewRequest"];

/** The decision of the commission. */
export type PromotionDecision = NonNullable<
  components["schemas"]["PromotionDecision"]
>;

/** One row of the ranking of the candidates of a commission. */
export type RankingItem = components["schemas"]["RankingItemResponse"];

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

/** What sending a wave back to a stage that has passed would reset or compute again. */
export async function rollbackPreview(id: string, stage: number) {
  return unwrap(
    await api.GET("/api/v1/promotion/waves/{id}/rollback", {
      params: { path: { id }, query: { stage } },
    }),
  );
}

/** Sends a wave back to a stage that has passed, with new end dates from that stage on. */
export async function rollbackWave(
  id: string,
  version: number,
  body: RollbackInput,
) {
  return withVersion(
    await api.POST("/api/v1/promotion/waves/{id}/rollback", {
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

/** The waves that are announced or take applications. */
export const openWavesQuery = queryOptions({
  queryKey: [...promotionQueryKey, "open-waves"],
  queryFn: async () => unwrap(await api.GET("/api/v1/promotion/waves/open")),
});

/** The levels and competencies of a target of a wave. */
export function targetFormQuery(waveId: string, matrixId: string) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "target", waveId, matrixId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/promotion/waves/{waveId}/targets/{matrixId}", {
          params: { path: { waveId, matrixId } },
        }),
      ),
  });
}

/** The applications of the signed-in employee, the latest first. */
export const myApplicationsQuery = queryOptions({
  queryKey: [...promotionQueryKey, "my-applications"],
  queryFn: async () =>
    unwrap(await api.GET("/api/v1/promotion/applications/mine")),
});

const UNFINISHED_STATUSES: readonly string[] = [
  "Submitted",
  "Admitted",
  "UnderReview",
];

/** The unfinished application of the viewer by the wave it was filed in. */
export function unfinishedApplicationIdByWave(
  applications: readonly WaveApplication[],
): Map<string, string> {
  return new Map(
    applications
      .filter((application) => UNFINISHED_STATUSES.includes(application.status))
      .map((application) => [application.waveId, application.id] as const),
  );
}

/** Files an application. */
export async function createApplication(
  waveId: string,
  body: ApplicationInput,
) {
  return withVersion(
    await api.POST("/api/v1/promotion/applications", {
      body: { waveId, ...body },
    }),
  );
}

/** Replaces the content of an application. */
export async function saveApplication(
  id: string,
  version: number,
  body: ApplicationInput,
) {
  return withVersion(
    await api.PUT("/api/v1/promotion/applications/{id}", {
      params: { path: { id }, header: { "If-Match": ifMatchOf(version) } },
      body,
    }),
  );
}

/** Withdraws an application. */
export async function withdrawApplication(id: string, version: number) {
  return withVersion(
    await api.POST("/api/v1/promotion/applications/{id}/withdraw", {
      params: { path: { id }, header: { "If-Match": ifMatchOf(version) } },
    }),
  );
}

/** Replaces the raters of an application. */
export async function saveRaters(
  id: string,
  version: number,
  raterEmployeeIds: string[],
) {
  return withVersion(
    await api.PUT("/api/v1/promotion/applications/{id}/raters", {
      params: { path: { id }, header: { "If-Match": ifMatchOf(version) } },
      body: {
        raters: raterEmployeeIds.map((employeeId) => ({
          employeeId,
          email: null,
        })),
      },
    }),
  );
}

/** Replaces the artifacts of an application. */
export async function saveArtifacts(
  id: string,
  version: number,
  artifacts: ArtifactInput[],
) {
  return withVersion(
    await api.PUT("/api/v1/promotion/applications/{id}/artifacts", {
      params: { path: { id }, header: { "If-Match": ifMatchOf(version) } },
      body: { artifacts },
    }),
  );
}

/** Marks that the candidate received the feedback. */
export async function markFeedbackReceived(id: string, version: number) {
  return withVersion(
    await api.POST("/api/v1/promotion/applications/{id}/feedback", {
      params: { path: { id }, header: { "If-Match": ifMatchOf(version) } },
    }),
  );
}

/** The candidates the signed-in employee is asked to rate. */
export const myRatingsQuery = queryOptions({
  queryKey: [...promotionQueryKey, "my-ratings"],
  queryFn: async () => unwrap(await api.GET("/api/v1/promotion/ratings/mine")),
});

/** What the signed-in rater sees of one candidate, with their own answers. */
export function ratingQuery(applicationId: string) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "rating", applicationId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/promotion/applications/{id}/rating", {
          params: { path: { id: applicationId } },
        }),
      ),
    gcTime: 0,
  });
}

/** Saves the answers of the signed-in rater. */
export async function saveRating(
  applicationId: string,
  answers: RatingAnswer[],
) {
  return unwrap(
    await api.PUT("/api/v1/promotion/applications/{id}/rating", {
      params: { path: { id: applicationId } },
      body: { answers },
    }),
  );
}

/** The card of a candidate for the commission, dropped once nothing shows it, so a change always starts from the current version. */
export function reviewQuery(applicationId: string) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "review", applicationId],
    queryFn: async () =>
      withVersion(
        await api.GET("/api/v1/promotion/applications/{id}/review", {
          params: { path: { id: applicationId } },
        }),
      ),
    gcTime: 0,
  });
}

/** Replaces the final scores of a candidate and records or changes the decision. */
export async function saveReview(
  applicationId: string,
  version: number,
  body: ReviewInput,
) {
  return withVersion(
    await api.PUT("/api/v1/promotion/applications/{id}/review", {
      params: {
        path: { id: applicationId },
        header: { "If-Match": ifMatchOf(version) },
      },
      body,
    }),
  );
}

/** The candidates the commission of a unit reviews in a wave, ranked by rating. */
export function rankingQuery(waveId: string, unitId: string) {
  return queryOptions({
    queryKey: [...promotionQueryKey, "ranking", waveId, unitId],
    queryFn: async () =>
      unwrap(
        await api.GET(
          "/api/v1/promotion/waves/{waveId}/units/{unitId}/ranking",
          {
            params: { path: { waveId, unitId } },
          },
        ),
      ),
  });
}
