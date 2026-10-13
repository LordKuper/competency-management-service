import type { NavItem } from "../../app/featureContract";
import { DeskCounter } from "./DeskCounter";
import { DeskPage } from "./DeskPage";
import { deskPaths } from "./deskTexts";

/** The desk is every user's first menu entry, with the count of their expected actions. */
export const navItems: NavItem[] = [
  {
    key: "desk",
    label: "Рабочий стол",
    path: deskPaths.desk,
    order: 0,
    badge: DeskCounter,
  },
];

/** The desk page; the scenario screens it links to join under the same prefix. */
export const routes = [{ path: deskPaths.desk, Component: DeskPage }];
