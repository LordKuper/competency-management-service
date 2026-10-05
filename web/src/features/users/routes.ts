import { AdminOnly } from "../../app/AdminOnly";
import type { NavItem } from "../../app/featureContract";
import { UserCardPage } from "./UserCardPage";
import { UserCreatePage } from "./UserCreatePage";
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

/** The list, the creation page and the account card. */
export const routes = [
  {
    Component: AdminOnly,
    children: [
      { path: "/users", Component: UserListPage },
      { path: "/users/new", Component: UserCreatePage },
      { path: "/users/:id", Component: UserCardPage },
    ],
  },
];
