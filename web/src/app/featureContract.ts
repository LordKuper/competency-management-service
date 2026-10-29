import type { ComponentType } from "react";
import type { RouteObject } from "react-router";
import type { components } from "../api/schema";

/** Address of the sign-in screen, the one place an unauthenticated session is sent to. */
export const LOGIN_PATH = "/login";

/** Address the application root and a sign-in without a target open. */
export const HOME_PATH = "/desk";

/** Account role; screens and navigation adapt to it, while the server alone enforces access. */
export type UserRole = components["schemas"]["UserRole"];

/** Submenu of the shell navigation an entry may be grouped under. */
export type NavGroup = "reference" | "admin" | "promotion";

/** One entry of the shell navigation, contributed by a feature. */
export interface NavItem {
  /** Stable identifier of the entry. */
  key: string;
  /** Russian text shown in the menu. */
  label: string;
  /** Address the entry opens; the entry is current on this address and every address under it. */
  path: string;
  /** Position in the menu, ascending. */
  order: number;
  /** Submenu the entry is grouped under; omitted means a top-level entry. */
  group?: NavGroup;
  /** Roles that see the entry: an account with any of them does; omitted means every signed-in user. */
  roles?: ReadonlyArray<UserRole>;
  /** Roles that never see the entry, even with one of `roles`: an account with any of them has the same page under another entry. */
  hiddenFromRoles?: ReadonlyArray<UserRole>;
  /** Mark shown after the label, such as a count; it renders nothing when there is nothing to show. */
  badge?: ComponentType;
}

/** One step of the guided tour a screen shows from the shell's help button. */
export interface TourStep {
  /** Value of the `data-tour` attribute of the element the step points at; omitted means a step about the whole screen. A step whose element is not on the screen is skipped. */
  target?: string;
  /** Step of the wizard on the screen (counted from 1, as its step ruler marks it) the hint belongs to; omitted means every step. */
  wizardStep?: number;
  /** Short heading of the step. */
  title: string;
  /** What is done here and why, in Russian. */
  description: string;
}

/** Tour steps by route pattern, the same pattern the feature's routes use. */
export type ScreenTours = Record<string, TourStep[]>;

/** What a feature's `routes.ts` may export. */
export interface FeatureRoutes {
  /** Routes rendered inside the shell, which requires a signed-in user. */
  routes?: RouteObject[];
  /** Routes rendered without the shell, such as the sign-in screen. */
  standaloneRoutes?: RouteObject[];
  /** Entries the shell adds to its navigation. */
  navItems?: NavItem[];
  /** Tour steps of the feature's screens that the help button walks through. */
  tours?: ScreenTours;
}
