import type { RouteObject } from "react-router";
import type { NavItem } from "../../app/featureContract";
import { CareerPage } from "./CareerPage";

/** Every signed-in user opens the career model, right after the position catalogue. */
export const navItems: NavItem[] = [
  { key: "career", label: "Карьерная модель", path: "/career", order: 26 },
];

/** The section page; its tabs are filled by the reference and matrix screens. */
export const routes: RouteObject[] = [
  { path: "/career", Component: CareerPage },
];
