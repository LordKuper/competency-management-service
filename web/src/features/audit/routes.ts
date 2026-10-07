import { AdminOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";
import { AuditPage } from "./AuditPage";

/** The journal is for global administrators only. */
export const navItems: NavItem[] = [
  {
    key: "audit",
    label: "Журнал",
    path: "/audit",
    order: 30,
    roles: ["GlobalAdmin"],
  },
];

/** The read-only list of audit events. */
export const routes = [
  {
    Component: AdminOnly,
    children: [{ path: "/audit", Component: AuditPage }],
  },
];
