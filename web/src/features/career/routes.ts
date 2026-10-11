import type { RouteObject } from "react-router";
import type { NavItem } from "../../app/featureContract";
import { CareerPage } from "./CareerPage";
import { MatrixPage } from "./MatrixPage";

/** Every signed-in user opens the career model, after the organisation structure. */
export const navItems: NavItem[] = [
  { key: "career", label: "Карьерная модель", path: "/career", order: 20 },
];

/** The section page with its tabs, and the page of one matrix. */
export const routes: RouteObject[] = [
  { path: "/career", Component: CareerPage },
  { path: "/career/matrices/:id", Component: MatrixPage },
];
