import type { RouteObject } from "react-router";
import type { NavItem } from "../../app/featureContract";
import { OrgStructurePage } from "./OrgStructurePage";
import { paths } from "./paths";

/** Every signed-in user reads the structure. */
export const navItems: NavItem[] = [
  { key: "org-structure", label: "Оргструктура", path: paths.tree, order: 20 },
];

/** The card hierarchy, from which employees and units are opened and, for administrators, changed. */
export const routes: RouteObject[] = [
  { path: paths.tree, Component: OrgStructurePage },
];
