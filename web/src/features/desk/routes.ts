import type { RouteObject } from "react-router";
import type { NavItem } from "../../app/featureContract";
import { DeskCounter } from "./DeskCounter";
import { DeskPage } from "./DeskPage";
import { deskPaths } from "./deskTexts";

/** The desk is every user's first menu entry, with the count of their expected actions. */
export const navItems: NavItem[] = [
  {
    key: "desk",
    label: "Рабочий стол",
    path: deskPaths.desk,
    order: 0,
    badge: DeskCounter,
  },
];

const builder = () =>
  import("./MatrixBuilderPage").then((page) => ({
    Component: page.MatrixBuilderPage,
  }));
const drafts = () =>
  import("./DraftsQueuePage").then((page) => ({
    Component: page.DraftsQueuePage,
  }));
const trackPositions = () =>
  import("./TrackPositionsPage").then((page) => ({
    Component: page.TrackPositionsPage,
  }));

/**
 * The desk page, and the scenario screens it links to. The screens load on first use, so the desk and the rest of the
 * application do not carry them.
 */
export const routes: RouteObject[] = [
  { path: deskPaths.desk, Component: DeskPage },
  {
    path: "/desk/review/:versionId",
    lazy: () =>
      import("./ReviewPage").then((page) => ({ Component: page.ReviewPage })),
  },
  {
    path: "/desk/rework/:versionId",
    lazy: () =>
      import("./ReworkPage").then((page) => ({ Component: page.ReworkPage })),
  },
  { path: deskPaths.drafts, lazy: drafts },
  { path: "/desk/drafts/:kind/:entryId", lazy: drafts },
  { path: deskPaths.matrixBuilder, lazy: builder },
  { path: "/desk/matrix-builder/:versionId", lazy: builder },
  { path: deskPaths.trackPositions, lazy: trackPositions },
  { path: "/desk/track-positions/:trackId", lazy: trackPositions },
  {
    path: deskPaths.catalogCleanup,
    lazy: () =>
      import("./CatalogCleanupPage").then((page) => ({
        Component: page.CatalogCleanupPage,
      })),
  },
];
