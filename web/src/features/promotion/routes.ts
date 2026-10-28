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

/** Waves and commissions are run by HR and administrators; the settings are the administrator's alone. */
export const navItems: NavItem[] = [
  {
    key: "promotion-waves",
    label: "Волны",
    path: promotionPaths.waves,
    order: 1,
    group: "promotion",
    roles: ["GlobalAdmin", "Hr"],
  },
  {
    key: "promotion-commissions",
    label: "Комиссии",
    path: promotionPaths.commissions,
    order: 2,
    group: "promotion",
    roles: ["GlobalAdmin", "Hr"],
  },
  {
    key: "promotion-settings",
    label: "Настройки",
    path: promotionPaths.settings,
    order: 3,
    group: "promotion",
    roles: ["GlobalAdmin"],
  },
];

const wavePage = () =>
  import("./WavePage").then((page) => ({ Component: page.WavePage }));

/** The screens of HR and administrators; they load on first use, so the rest of the application does not carry them. */
export const routes: RouteObject[] = [
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
    ],
  },
];
