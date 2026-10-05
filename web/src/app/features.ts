import type { FeatureRoutes, NavItem, UserRole } from "./featureContract";

/** Feature modules found by convention: every `features/<name>/routes.ts` joins the app without a shared file being edited. */
export const features = Object.values(
  import.meta.glob<FeatureRoutes>("../features/*/routes.ts", { eager: true }),
);

const navItems = features
  .flatMap((feature) => feature.navItems ?? [])
  .sort((first, second) => first.order - second.order);

/** Navigation entries a user of the given role may see, in menu order. */
export function navItemsFor(role: UserRole): NavItem[] {
  return navItems.filter((item) => item.roles?.includes(role) ?? true);
}
