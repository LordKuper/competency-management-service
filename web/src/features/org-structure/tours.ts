import type { ScreenTours } from "../../app/featureContract";
import { paths } from "./paths";

/** Tour steps of the organisation structure screen. */
export const tours: ScreenTours = {
  [paths.tree]: [
    {
      title: "Оргструктура",
      description:
        "Подразделения компании и их сотрудники. По этой структуре система определяет, кто кому подчиняется и кто участвует в карьерных сессиях.",
    },
    {
      target: "org-add",
      title: "Добавить",
      description: "Создайте подразделение или карточку сотрудника.",
    },
    {
      target: "org-search",
      title: "Поиск",
      description:
        "Найдите подразделение, сотрудника по ФИО или должности: дерево раскроется до найденного.",
    },
    {
      target: "org-tree",
      title: "Иерархия",
      description:
        "Раскрывайте подразделения, чтобы увидеть вложенные и сотрудников. Карточка открывает действия с подразделением или сотрудником.",
    },
  ],
};
