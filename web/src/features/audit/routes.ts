import { AdminOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";
import { AuditPage } from "./AuditPage";

export { tours } from "./tours";

/** The journal is for global administrators only. */
export const navItems: NavItem[] = [
  {
    key: "audit",
    label: "Журнал",
    path: "/audit",
    order: 50,
    group: "admin",
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
