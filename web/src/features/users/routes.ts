import { AdminOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";
import { UserListPage } from "./UserListPage";

/** Account management is for global administrators only. */
export const navItems: NavItem[] = [
  {
    key: "users",
    label: "Пользователи",
    path: "/users",
    order: 10,
    roles: ["GlobalAdmin"],
  },
];

/** The list, from which every account action opens. */
export const routes = [
  {
    Component: AdminOnly,
    children: [{ path: "/users", Component: UserListPage }],
  },
];
