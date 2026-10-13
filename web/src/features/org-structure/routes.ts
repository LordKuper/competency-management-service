import type { RouteObject } from "react-router";
import type { NavItem } from "../../app/featureContract";
import { OrgStructurePage } from "./OrgStructurePage";
import { paths } from "./paths";

/** Every signed-in user reads the structure, a reference next to the desk. */
export const navItems: NavItem[] = [
  {
    key: "org-structure",
    label: "Оргструктура",
    path: paths.tree,
    order: 10,
    group: "reference",
  },
];

/** The card hierarchy, from which employees and units are opened and, for administrators, changed. */
export const routes: RouteObject[] = [
  { path: paths.tree, Component: OrgStructurePage },
];
