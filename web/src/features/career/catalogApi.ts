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

/** Column filters of the track list: the direction, and the unit that owns the track, match exactly. */
export interface TrackListParams extends CatalogListParams {
  directionId?: string;
  ownerOrgUnitId?: string;
}

/** Column filters of the draft list. */
export interface DraftListParams {
  kind?: CatalogKind;
  orgUnitId?: string;
}

/** The first page of a catalogue table. */
export const FIRST_PAGE: CatalogListParams = { page: 1, pageSize: 20 };

/** The server's largest page, by which the levels of a track are read. */
const LEVEL_PAGE_SIZE = 200;

export function directionsQuery(params: CatalogListParams) {
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

export function tracksQuery(params: TrackListParams) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "tracks", params],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/tracks", { params: { query: params } }),
      ),
  });
}

/** Every level of one track in track order, read page by page, since the track is ordered and edited as a whole. */
export function levelsQuery(trackId: string) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "levels", trackId],
    queryFn: async () => {
      const items: CareerLevel[] = [];
      for (let page = 1; ; page += 1) {
        const read = unwrap(
          await api.GET("/api/v1/career/levels", {
            params: { query: { trackId, page, pageSize: LEVEL_PAGE_SIZE } },
          }),
        );
        items.push(...read.items);
        if (items.length >= read.total || read.items.length === 0) {
          return { ...read, items };
        }
      }
    },
  });
}

export function categoriesQuery(params: CatalogListParams) {
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

/** One entry of a catalogue by its id, for the name of one that no list on the screen holds. */
export function entryQuery(segment: CatalogSegment, id: string) {
  return queryOptions({
    queryKey: [...catalogQueryKey, segment, "entry", id],
    queryFn: async () =>
      unwrap(
        await api.GET(
          `/api/v1/career/${segment}/{id}` as "/api/v1/career/directions/{id}",
          { params: { path: { id } } },
        ),
      ),
    retry: false,
  });
}

/** One competency by its id, for the name and type of one that no list on the screen holds. */
export function competencyQuery(id: string) {
  return queryOptions({
    queryKey: [...catalogQueryKey, "competencies", "entry", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/competencies/{id}", {
          params: { path: { id } },
        }),
      ),
    retry: false,
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

export function scalesQuery(params: ScaleListParams) {
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

/** Approves a draft the drafts list names, under the version the list showed. */
export async function approveDraft(draft: CatalogDraft) {
  return changeCatalogStatus(SEGMENT_OF_KIND[draft.kind], draft, "approve");
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

/** Puts the track's levels in the given order, the most junior first; the track takes a new version. */
export async function orderLevels(
  track: { id: string; version: number },
  levelIds: string[],
) {
  return unwrap(
    await api.PUT("/api/v1/career/tracks/{id}/level-order", {
      params: {
        path: { id: track.id },
        header: { "If-Match": ifMatchOf(track.version) },
      },
      body: { levelIds },
    }),
  );
}
