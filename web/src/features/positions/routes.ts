import { AdminOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";
import { PositionsPage } from "./PositionsPage";

/** The position catalogue is kept by global administrators only. */
export const navItems: NavItem[] = [
  {
    key: "positions",
    label: "Должности",
    path: "/positions",
    order: 40,
    group: "admin",
    roles: ["GlobalAdmin"],
  },
];

/** The catalogue list, from which every position action opens. */
export const routes = [
  {
    Component: AdminOnly,
    children: [{ path: "/positions", Component: PositionsPage }],
  },
];
