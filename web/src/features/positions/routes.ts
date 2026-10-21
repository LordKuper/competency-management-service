import { AdminOrHrOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";
import { PositionsPage } from "./PositionsPage";

/** The position catalogue is kept by global administrators and HR; an account with both roles finds it under administration. */
export const navItems: NavItem[] = [
  {
    key: "positions",
    label: "Должности",
    path: "/positions",
    order: 40,
    group: "admin",
    roles: ["GlobalAdmin"],
  },
  {
    key: "positions-hr",
    label: "Должности",
    path: "/positions",
    order: 40,
    group: "reference",
    roles: ["Hr"],
    hiddenFromRoles: ["GlobalAdmin"],
  },
];

/** The catalogue list, from which every position action opens. */
export const routes = [
  {
    Component: AdminOrHrOnly,
    children: [{ path: "/positions", Component: PositionsPage }],
  },
];
