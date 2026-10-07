import type { RouteObject } from "react-router";
import type { components } from "../api/schema";

/** Address of the sign-in screen, the one place an unauthenticated session is sent to. */
export const LOGIN_PATH = "/login";

/** Account role; screens and navigation adapt to it, while the server alone enforces access. */
export type UserRole = components["schemas"]["UserRole"];

/** One entry of the shell navigation, contributed by a feature. */
export interface NavItem {
  /** Stable identifier of the entry. */
  key: string;
  /** Russian text shown in the menu. */
  label: string;
  /** Address the entry opens. */
  path: string;
  /** Position in the menu, ascending. */
  order: number;
  /** Roles that see the entry; omitted means every signed-in user. */
  roles?: ReadonlyArray<UserRole>;
}

/** What a feature's `routes.ts` may export. */
export interface FeatureRoutes {
  /** Routes rendered inside the shell, which requires a signed-in user. */
  routes?: RouteObject[];
  /** Routes rendered without the shell, such as the sign-in screen. */
  standaloneRoutes?: RouteObject[];
  /** Entries the shell adds to its navigation. */
  navItems?: NavItem[];
}
