import type { RouteObject } from "react-router";
import { AdminOnly, AdminOrHrOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";

/** Addresses of every promotion screen; the desk items, letters and links between the screens use them. */
export const promotionPaths = {
  home: "/promotion",
  apply: (waveId: string) => `/promotion/apply/${waveId}`,
  application: (applicationId: string) =>
    `/promotion/applications/${applicationId}`,
  ratings: "/promotion/ratings",
  rating: (applicationId: string) => `/promotion/ratings/${applicationId}`,
  raters: (applicationId: string) => `/promotion/raters/${applicationId}`,
  waves: "/promotion/waves",
  newWave: "/promotion/waves/new",
  wave: (waveId: string) => `/promotion/waves/${waveId}`,
  commissions: "/promotion/commissions",
  commission: (waveId: string, unitId: string) =>
    `/promotion/commission/${waveId}/${unitId}`,
  review: (applicationId: string) => `/promotion/review/${applicationId}`,
  settings: "/promotion/settings",
  demo: "/promotion/demo",
} as const;

/** Every signed-in user reaches promotion from one entry; waves and commissions are run by HR and administrators, the settings are the administrator's alone. */
export const navItems: NavItem[] = [
  {
    key: "promotion",
    label: "Повышение",
    path: promotionPaths.home,
    order: 1,
  },
  {
    key: "promotion-waves",
    label: "Волны",
    path: promotionPaths.waves,
    order: 2,
    group: "promotion",
    roles: ["GlobalAdmin", "Hr"],
  },
  {
    key: "promotion-commissions",
    label: "Комиссии",
    path: promotionPaths.commissions,
    order: 3,
    group: "promotion",
    roles: ["GlobalAdmin", "Hr"],
  },
  {
    key: "promotion-settings",
    label: "Настройки",
    path: promotionPaths.settings,
    order: 4,
    group: "promotion",
    roles: ["GlobalAdmin"],
  },
  {
    key: "promotion-demo",
    label: "Демонстрация",
    path: promotionPaths.demo,
    order: 5,
    group: "promotion",
    roles: ["GlobalAdmin"],
  },
];

const wavePage = () =>
  import("./WavePage").then((page) => ({ Component: page.WavePage }));

/** The screens of candidates, raters, heads and commission members are open to every signed-in user, since the server alone says whose they are; the management screens are for HR and administrators. They load on first use, so the rest of the application does not carry them. */
export const routes: RouteObject[] = [
  {
    path: promotionPaths.home,
    lazy: () =>
      import("./PromotionHomePage").then((page) => ({
        Component: page.PromotionHomePage,
      })),
  },
  {
    path: "/promotion/apply/:waveId",
    lazy: () =>
      import("./ApplyPage").then((page) => ({ Component: page.ApplyPage })),
  },
  {
    path: "/promotion/applications/:id",
    lazy: () =>
      import("./ApplicationPage").then((page) => ({
        Component: page.ApplicationPage,
      })),
  },
  {
    path: promotionPaths.ratings,
    lazy: () =>
      import("./RatingsPage").then((page) => ({
        Component: page.RatingsPage,
      })),
  },
  {
    path: "/promotion/ratings/:applicationId",
    lazy: () =>
      import("./RatingPage").then((page) => ({ Component: page.RatingPage })),
  },
  {
    path: "/promotion/raters/:applicationId",
    lazy: () =>
      import("./RatersPage").then((page) => ({ Component: page.RatersPage })),
  },
  {
    path: "/promotion/commission/:waveId/:unitId",
    lazy: () =>
      import("./RankingPage").then((page) => ({ Component: page.RankingPage })),
  },
  {
    path: "/promotion/review/:applicationId",
    lazy: () =>
      import("./ReviewPage").then((page) => ({ Component: page.ReviewPage })),
  },
  {
    Component: AdminOrHrOnly,
    children: [
      {
        path: promotionPaths.waves,
        lazy: () =>
          import("./WavesPage").then((page) => ({ Component: page.WavesPage })),
      },
      { path: promotionPaths.newWave, lazy: wavePage },
      { path: "/promotion/waves/:id", lazy: wavePage },
      {
        path: promotionPaths.commissions,
        lazy: () =>
          import("./CommissionsPage").then((page) => ({
            Component: page.CommissionsPage,
          })),
      },
    ],
  },
  {
    Component: AdminOnly,
    children: [
      {
        path: promotionPaths.settings,
        lazy: () =>
          import("./SettingsPage").then((page) => ({
            Component: page.SettingsPage,
          })),
      },
      {
        path: promotionPaths.demo,
        lazy: () =>
          import("./demo/DemoPage").then((page) => ({
            Component: page.DemoPage,
          })),
      },
    ],
  },
];
