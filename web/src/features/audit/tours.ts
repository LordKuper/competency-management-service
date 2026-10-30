import type { ScreenTours } from "../../app/featureContract";

/** Tour steps of the audit journal screen. */
export const tours: ScreenTours = {
  "/audit": [
    {
      title: "Журнал аудита",
      description:
        "Список действий пользователей в системе, только для чтения: кто, когда и что сделал.",
    },
    {
      target: "audit-filters",
      title: "Фильтры",
      description:
        "Сузьте список по периоду, исполнителю, действию или объекту и нажмите «Применить».",
    },
    {
      target: "audit-table",
      title: "События",
      description:
        "Раскройте строку, чтобы увидеть, какие значения были до и после действия.",
    },
  ],
};
