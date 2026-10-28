import { matchPath } from "react-router";
import type {
  FeatureRoutes,
  NavItem,
  ScreenTours,
  TourStep,
  UserRole,
} from "./featureContract";

/** Feature modules found by convention: every `features/<name>/routes.ts` joins the app without a shared file being edited. */
export const features = Object.values(
  import.meta.glob<FeatureRoutes>("../features/*/routes.ts", { eager: true }),
);

const navItems = features
  .flatMap((feature) => feature.navItems ?? [])
  .sort((first, second) => first.order - second.order);

/** Navigation entries a user with the given roles may see, in menu order. */
export function navItemsFor(roles: ReadonlyArray<UserRole>): NavItem[] {
  return navItems.filter(
    (item) =>
      (item.roles?.some((role) => roles.includes(role)) ?? true) &&
      !item.hiddenFromRoles?.some((role) => roles.includes(role)),
  );
}

const tours: ScreenTours = Object.assign(
  {},
  ...features.map((feature) => feature.tours),
);

/** Tour steps of the screen at the address; an address equal to a pattern wins over a pattern with parameters, so `/waves/new` is not read as `/waves/:id`. */
export function tourFor(pathname: string): TourStep[] | undefined {
  return (
    tours[pathname] ??
    Object.entries(tours).find(([pattern]) => matchPath(pattern, pathname))?.[1]
  );
}
