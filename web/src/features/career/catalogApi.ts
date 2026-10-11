import { type QueryClient, queryOptions } from "@tanstack/react-query";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";
import { careerQueryKey } from "./careerApi";

type Schemas = components["schemas"];

/** The fields every career catalogue entry has: directions and categories carry no more. */
export type CatalogEntry = Schemas["CatalogEntryResponse"];
export type CatalogStatus = Schemas["CatalogStatus"];
export type CareerTrack = Schemas["CareerTrackResponse"];
export type CareerLevel = Schemas["CareerLevelResponse"];
export type LevelSalary = Schemas["CareerLevelSalaryResponse"];
export type Competency = Schemas["CompetencyResponse"];
export type CompetencyType = Schemas["CompetencyType"];
export type CompetencyVersion = Schemas["CompetencyVersionResponse"];
export type Scale = Schemas["CompetencyScaleResponse"];
export type ScalePoint = Schemas["ScalePoint"];
export type CatalogDraft = Schemas["CatalogDraftResponse"];
export type CatalogKind = Schemas["CatalogEntryKind"];

/** Cache key prefix of every catalogue query, so one invalidation rereads whatever a change touched. */
export const catalogQueryKey = [...careerQueryKey, "catalog"] as const;

/** Route segment of each catalogue under `/api/v1/career`. */
export type CatalogSegment =
  | "directions"
  | "tracks"
  | "levels"
  | "categories"
  | "competencies"
  | "scales";

const SEGMENT_OF_KIND: Record<CatalogKind, CatalogSegment> = {
  Direction: "directions",
  Track: "tracks",
  Category: "categories",
  Competency: "competencies",
  Scale: "scales",
};

/** Paging and search of a catalogue list; pages count from 1. */
export interface CatalogListParams {
  q?: string;
  status?: CatalogStatus;
  page: number;
  pageSize: number;
}

/** Column filters of the competency list. */
export interface CompetencyListParams extends CatalogListParams {
  categoryId?: string;
  directionId?: string;
  scaleId?: string;
  type?: CompetencyType;
}

/** Column filters of the scale list. */
export interface ScaleListParams extends CatalogListParams {
  isDefault?: boolean;
}

/** Column filters of the track list: the unit that owns the track matches exactly. */
export interface TrackFilters {
  status?: CatalogStatus;
  ownerOrgUnitId?: string;
}

/** Column filters of the draft list. */
export interface DraftListParams {
  kind?: CatalogKind;
  orgUnitId?: string;
}

/**
 * The first page at the server's largest page size, for pickers and lookups of names.
 * ponytail: a catalogue past this many entries is cut off; add server-side search in the pickers if one grows that far.
 */
export const ALL_ENTRIES: CatalogListParams = { page: 1, pageSize: 200 };

export function directionsQuery(params: CatalogListParams = ALL_ENTRIES) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "directions", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/directions", {
          params: { query: params },
        }),
      ),
  });
}

/** The tracks of one direction, or of every direction when none is given. */
export function tracksQuery(directionId?: string, filters: TrackFilters = {}) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "tracks", directionId ?? null, filters],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/tracks", {
          params: { query: { ...ALL_ENTRIES, directionId, ...filters } },
        }),
      ),
  });
}

/** The levels of one track, or of every track when none is given. */
export function levelsQuery(trackId?: string) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "levels", trackId ?? null],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/levels", {
          params: { query: { ...ALL_ENTRIES, trackId } },
        }),
      ),
  });
}

/** The salary range of a level; it answers 404 to a user who may not read it. */
export function levelSalaryQuery(levelId: string) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "level-salary", levelId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/levels/{id}/salary", {
          params: { path: { id: levelId } },
        }),
      ),
    retry: false,
  });
}

export function categoriesQuery(params: CatalogListParams = ALL_ENTRIES) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "categories", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/categories", {
          params: { query: params },
        }),
      ),
  });
}

export function competenciesQuery(params: CompetencyListParams) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "competencies", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/competencies", {
          params: { query: params },
        }),
      ),
  });
}

export function competencyVersionsQuery(competencyId: string) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "competency-versions", competencyId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/competencies/{id}/versions", {
          params: { path: { id: competencyId } },
        }),
      ),
  });
}

export function scalesQuery(params: ScaleListParams = ALL_ENTRIES) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "scales", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/scales", {
          params: { query: params },
        }),
      ),
  });
}

/** Draft entries waiting for the signed-in HR. */
export function draftsQuery(params: DraftListParams = {}) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "drafts", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/drafts", { params: { query: params } }),
      ),
  });
}

/** Rereads every catalogue query after a change. */
export function invalidateCatalogs(queryClient: QueryClient) {
  return queryClient.invalidateQueries({ queryKey: catalogQueryKey });
}

/**
 * Entries a picker offers: those in work, plus the one already chosen even when it has been deactivated since.
 */
export function usableEntries<T extends CatalogEntry>(
  entries: readonly T[] | undefined,
  chosen: string | null | undefined,
): T[] {
  return (entries ?? []).filter(
    (entry) => entry.status !== "Inactive" || entry.id === chosen,
  );
}

export type CatalogAction = "approve" | "deactivate" | "activate";

/**
 * Approves, deactivates or reactivates an entry of any catalogue: the three endpoints have one shape in every catalogue,
 * so the path is typed by the first catalogue's.
 */
export async function changeCatalogStatus(
  segment: CatalogSegment,
  entry: { id: string; version: number },
  action: CatalogAction,
) {
  return unwrap(
    await api.POST(
      `/api/v1/career/${segment}/{id}/${action}` as "/api/v1/career/directions/{id}/approve",
      {
        params: {
          path: { id: entry.id },
          header: { "If-Match": ifMatchOf(entry.version) },
        },
      },
    ),
  );
}

/** Approves a draft the drafts list names; the list carries no version, so the entry is read first. */
export async function approveDraft(draft: CatalogDraft) {
  const segment = SEGMENT_OF_KIND[draft.kind];
  const current = unwrap(
    await api.GET(
      `/api/v1/career/${segment}/{id}` as "/api/v1/career/directions/{id}",
      { params: { path: { id: draft.id } } },
    ),
  );
  return changeCatalogStatus(segment, current, "approve");
}

/** Values of a form that names an entry of a catalogue with nothing else to it. */
export interface NamedEntryValues {
  name: string;
  /** Unit and direction of a track being created. */
  ownerOrgUnitId?: string;
  directionId?: string;
}

/**
 * Creates (no `base`) or renames a direction, track or category. A track is created in a unit and a direction; the
 * three catalogues share the rename endpoint's shape and a direction and a category the creation's.
 */
export async function saveNamedEntry(
  segment: "directions" | "tracks" | "categories",
  values: NamedEntryValues,
  base: { id: string; version: number } | undefined,
) {
  if (base) {
    return unwrap(
      await api.PUT(
        `/api/v1/career/${segment}/{id}` as "/api/v1/career/directions/{id}",
        {
          params: {
            path: { id: base.id },
            header: { "If-Match": ifMatchOf(base.version) },
          },
          body: { name: values.name },
        },
      ),
    );
  }
  if (segment === "tracks" && values.directionId && values.ownerOrgUnitId) {
    return unwrap(
      await api.POST("/api/v1/career/tracks", {
        body: {
          name: values.name,
          ownerOrgUnitId: values.ownerOrgUnitId,
          directionId: values.directionId,
        },
      }),
    );
  }
  return unwrap(
    await api.POST(`/api/v1/career/${segment}` as "/api/v1/career/directions", {
      body: { name: values.name },
    }),
  );
}

/** Makes an active scale the one a competency takes when none is named. */
export async function makeScaleDefault(scale: { id: string; version: number }) {
  return unwrap(
    await api.POST("/api/v1/career/scales/{id}/make-default", {
      params: {
        path: { id: scale.id },
        header: { "If-Match": ifMatchOf(scale.version) },
      },
    }),
  );
}

/** Deletes an entry of any catalogue; the server answers 409 with the reason to one that is in use. */
export async function deleteEntry(
  segment: CatalogSegment,
  entry: { id: string; version: number },
) {
  return unwrap(
    await api.DELETE(
      `/api/v1/career/${segment}/{id}` as "/api/v1/career/categories/{id}",
      {
        params: {
          path: { id: entry.id },
          header: { "If-Match": ifMatchOf(entry.version) },
        },
      },
    ),
  );
}
