import type { ScreenTours } from "../../app/featureContract";

/** Tour steps of the position catalogue screen. */
export const tours: ScreenTours = {
  "/positions": [
    {
      title: "Должности",
      description:
        "Каталог должностей компании. Должность выбирают в карточке сотрудника и связывают с уровнем карьерного трека.",
    },
    {
      target: "positions-add",
      title: "Добавить должность",
      description: "Создайте новую должность, если нужной ещё нет в каталоге.",
    },
    {
      target: "positions-search",
      title: "Поиск",
      description: "Найдите должность по названию.",
    },
    {
      target: "positions-table",
      title: "Список должностей",
      description:
        "Названия, состояние и действия с должностью. Столбцы можно отфильтровать.",
    },
  ],
};
