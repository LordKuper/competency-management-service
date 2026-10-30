import type { ScreenTours } from "../../app/featureContract";

/** Tour steps of the user accounts screen. */
export const tours: ScreenTours = {
  "/users": [
    {
      title: "Пользователи",
      description:
        "Учётные записи, с которыми сотрудники входят в систему. Здесь их создают, блокируют и назначают роли.",
    },
    {
      target: "users-add",
      title: "Создать пользователя",
      description: "Заведите учётную запись сотрудника и назначьте ей роли.",
    },
    {
      target: "users-filters",
      title: "Поиск и фильтры",
      description: "Найдите учётную запись по e-mail, роли или состоянию.",
    },
    {
      target: "users-table",
      title: "Список",
      description:
        "Роли и состояние каждой записи. Действия в строке позволяют изменить запись или заблокировать её.",
    },
  ],
};
