import type { RouteObject } from "react-router";
import { AdminOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";
import { EmployeeCardPage } from "./EmployeeCardPage";
import { EmployeeCreatePage } from "./EmployeeCreatePage";
import { EmployeeListPage } from "./EmployeeListPage";
import { OrgStructurePage } from "./OrgStructurePage";
import { paths } from "./paths";

/** Every signed-in user reads the structure. */
export const navItems: NavItem[] = [
  { key: "org-structure", label: "Оргструктура", path: paths.tree, order: 20 },
];

/** The card hierarchy, the employee list and card, and the creation page, which only administrators open. */
export const routes: RouteObject[] = [
  { path: paths.tree, Component: OrgStructurePage },
  { path: paths.employees, Component: EmployeeListPage },
  { path: paths.employee(":id"), Component: EmployeeCardPage },
  {
    Component: AdminOnly,
    children: [{ path: paths.newEmployee, Component: EmployeeCreatePage }],
  },
];
