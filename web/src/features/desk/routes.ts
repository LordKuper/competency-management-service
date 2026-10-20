import { type RouteObject, redirect } from "react-router";
import { AdminOnly } from "../../app/AdminOnly";
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
const connectUnit = () =>
  import("./ConnectUnitPage").then((page) => ({
    Component: page.ConnectUnitPage,
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
  {
    path: "/desk/drafts/category/:entryId",
    loader: () => redirect(deskPaths.drafts),
  },
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
  {
    path: deskPaths.myPlace,
    lazy: () =>
      import("./MyPlacePage").then((page) => ({ Component: page.MyPlacePage })),
  },
  {
    Component: AdminOnly,
    children: [
      { path: deskPaths.connectUnit, lazy: connectUnit },
      { path: "/desk/connect-unit/:unitId", lazy: connectUnit },
      {
        path: deskPaths.onboardEmployee,
        lazy: () =>
          import("./OnboardEmployeePage").then((page) => ({
            Component: page.OnboardEmployeePage,
          })),
      },
      {
        path: deskPaths.orgCleanup,
        lazy: () =>
          import("./OrgCleanupPage").then((page) => ({
            Component: page.OrgCleanupPage,
          })),
      },
    ],
  },
];
