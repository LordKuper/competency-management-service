import { type QueryClient, queryOptions } from "@tanstack/react-query";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";
import { careerQueryKey } from "./careerApi";

type Schemas = components["schemas"];

export type Matrix = Schemas["MatrixResponse"];
export type MatrixVersion = Schemas["MatrixVersionResponse"];
export type MatrixVersionStatus = Schemas["MatrixVersionStatus"];
export type MatrixContent = Schemas["MatrixContent"];
export type MatrixRequirement = Schemas["MatrixRequirement"];
export type MatrixBlock = Schemas["MatrixBlock"];
export type MatrixAction = Schemas["MatrixAction"];
export type MatrixHistory = Schemas["MatrixHistoryResponse"];
export type MatrixDecision = Schemas["MatrixDecisionResponse"];
export type MatrixDiff = Schemas["MatrixDiffResponse"];
export type MatrixChange = Schemas["MatrixChange"];
export type Approval = Schemas["ApprovalResponse"];
export type NewMatrix = Schemas["NewMatrixRequest"];

/** Cache key prefix of every matrix query, so one invalidation rereads whatever a transition touched. */
export const matrixQueryKey = [...careerQueryKey, "matrices"] as const;

/** Transitions of a version that take no body. */
export type MatrixTransition =
  | "submit"
  | "approve"
  | "accept-changes"
  | "accept-and-revise"
  | "rollback-changes";

/** Paging and column filters of the matrix list; pages count from 1. */
export interface MatrixListParams {
  orgUnitId?: string;
  directionId?: string;
  trackId?: string;
  page: number;
  pageSize: number;
}

export function matricesQuery(params: MatrixListParams) {
  return queryOptions({
    queryKey: [...matrixQueryKey, "list", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/matrices", { params: { query: params } }),
      ),
  });
}

export function matrixQuery(id: string) {
  return queryOptions({
    queryKey: [...matrixQueryKey, "matrix", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/matrices/{id}", {
          params: { path: { id } },
        }),
      ),
  });
}

export function matrixVersionsQuery(matrixId: string) {
  return queryOptions({
    queryKey: [...matrixQueryKey, "versions", matrixId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/matrices/{id}/versions", {
          params: { path: { id: matrixId } },
        }),
      ),
  });
}

export function matrixVersionQuery(id: string) {
  return queryOptions({
    queryKey: [...matrixQueryKey, "version", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/matrix-versions/{id}", {
          params: { path: { id } },
        }),
      ),
  });
}

/** Variants and decisions of a version; the server answers 404 to a user who may not read them. */
export function matrixHistoryQuery(versionId: string) {
  return queryOptions({
    queryKey: [...matrixQueryKey, "history", versionId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/matrix-versions/{id}/history", {
          params: { path: { id: versionId } },
        }),
      ),
    retry: false,
  });
}

/** Changes a variant made against the one before it. */
export function matrixDiffQuery(versionId: string, number: number) {
  return queryOptions({
    queryKey: [...matrixQueryKey, "diff", versionId, number],
    queryFn: async () =>
      unwrap(
        await api.GET(
          "/api/v1/career/matrix-versions/{id}/variants/{number}/diff",
          { params: { path: { id: versionId, number } } },
        ),
      ),
    retry: false,
  });
}

/** Column filters of the approvals list. */
export interface ApprovalListParams {
  orgUnitId?: string;
  step?: NonNullable<Approval["step"]>;
}

/** Versions waiting for the signed-in user's decision. */
export function approvalsQuery(params: ApprovalListParams = {}) {
  return queryOptions({
    queryKey: [...matrixQueryKey, "approvals", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/approvals", {
          params: { query: params },
        }),
      ),
  });
}

/** Rereads every matrix query after a change. */
export function invalidateMatrices(queryClient: QueryClient) {
  return queryClient.invalidateQueries({ queryKey: matrixQueryKey });
}

export async function createMatrix(body: NewMatrix) {
  return unwrap(await api.POST("/api/v1/career/matrices", { body }));
}

/** Starts the next version of a matrix as a draft copy of the published one. */
export async function createMatrixVersion(matrixId: string) {
  return unwrap(
    await api.POST("/api/v1/career/matrices/{id}/versions", {
      params: { path: { id: matrixId } },
    }),
  );
}

/** Saves the content of a version under the version the form was built from. */
export async function saveMatrixContent(
  version: MatrixVersion,
  content: MatrixContent,
) {
  return unwrap(
    await api.PUT("/api/v1/career/matrix-versions/{id}/content", {
      params: {
        path: { id: version.id },
        header: { "If-Match": ifMatchOf(version.version) },
      },
      body: content,
    }),
  );
}

/** Moves a version along its route; the transitions share one shape, so the path is typed by the first. */
export async function transitMatrixVersion(
  version: MatrixVersion,
  transition: MatrixTransition,
) {
  return unwrap(
    await api.POST(
      `/api/v1/career/matrix-versions/{id}/${transition}` as "/api/v1/career/matrix-versions/{id}/submit",
      {
        params: {
          path: { id: version.id },
          header: { "If-Match": ifMatchOf(version.version) },
        },
      },
    ),
  );
}

/** Returns a version to its author with the changes made and a comment. */
export async function returnMatrixVersion(
  version: MatrixVersion,
  comment: string,
) {
  return unwrap(
    await api.POST("/api/v1/career/matrix-versions/{id}/return", {
      params: {
        path: { id: version.id },
        header: { "If-Match": ifMatchOf(version.version) },
      },
      body: { comment: comment.trim() || null },
    }),
  );
}
