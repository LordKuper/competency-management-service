import type { ScreenTours, TourStep } from "../../app/featureContract";
import { deskPaths } from "./deskTexts";

const PATH_STEP: TourStep = {
  target: "scenario-path",
  title: "Путь назад",
  description:
    "Отсюда можно вернуться на рабочий стол или на экран, с которого вы пришли.",
};

const QUEUE_STEPS: TourStep[] = [
  PATH_STEP,
  {
    target: "queue-list",
    title: "Очередь",
    description:
      "Записи, которые ждут вашего решения. Выберите запись в списке, при необходимости отфильтруйте по виду.",
  },
  {
    target: "queue-record",
    title: "Открытая запись",
    description:
      "Здесь видно, что за запись, где она используется и что произойдёт после решения.",
  },
];

const DRAFTS_STEPS: TourStep[] = [
  {
    title: "Утвердить черновые записи",
    description:
      "Новые записи каталога ждут утверждения: после него их можно использовать в матрицах.",
  },
  ...QUEUE_STEPS,
];

const BUILDER_STEPS: TourStep[] = [
  {
    title: "Построить матрицу компетенций",
    description:
      "Мастер ведёт от выбора трека до отправки матрицы на согласование: так подразделение получает требования к каждому уровню.",
  },
  PATH_STEP,
  {
    target: "wizard-steps",
    title: "Шаги мастера",
    description:
      "Пройденные шаги можно открыть снова, чтобы поправить введённое. Замечания к шагу отмечены рядом с его названием.",
  },
  {
    target: "scenario-form",
    title: "Поля шага",
    description: "Заполните поля текущего шага: от них зависит состав матрицы.",
  },
  {
    target: "matrix-grid",
    title: "Требования по уровням",
    description:
      "Для каждой компетенции выберите балл, который нужен на уровне. По этим баллам оценивают кандидатов.",
  },
  {
    target: "builder-footer",
    title: "Дальше",
    description:
      "Сохраните шаг и переходите к следующему. Если что-то мешает, рядом сказано, что исправить.",
  },
];

const TRACK_POSITIONS_STEPS: TourStep[] = [
  {
    title: "Связать должности с треком",
    description:
      "Каждому уровню трека нужна должность: по ней система определяет, на каком уровне сейчас сотрудник.",
  },
  PATH_STEP,
  {
    target: "scenario-context",
    title: "Выбор трека",
    description: "Выберите трек, уровни которого нужно связать с должностями.",
  },
  {
    target: "track-levels",
    title: "Уровни трека",
    description:
      "У каждого уровня показаны связанные должности. Уровню без должности нужно подобрать её из списка рядом.",
  },
  {
    target: "track-available",
    title: "Доступные должности",
    description: "Найдите должность и свяжите её с уровнем трека.",
  },
];

const CONNECT_UNIT_STEPS: TourStep[] = [
  {
    title: "Подключить подразделение",
    description:
      "Проверка, всё ли готово, чтобы подразделение участвовало в карьерных сессиях: руководитель, HR, должности и учётные записи.",
  },
  PATH_STEP,
  {
    target: "scenario-context",
    title: "Подразделение",
    description: "Подразделение, которое вы проверяете.",
  },
  {
    target: "connect-rows",
    title: "Что проверено",
    description:
      "Каждая строка — один пункт. Там, где чего-то не хватает, рядом есть действие, которое это исправит.",
  },
];

/** Tour steps of the desk and of the scenario screens it opens. */
export const tours: ScreenTours = {
  [deskPaths.desk]: [
    {
      title: "Рабочий стол",
      description:
        "Здесь собрано всё, что ждёт вашего действия, и сценарии, доступные вам. С него удобно начинать работу в системе.",
    },
    {
      target: "desk-actions",
      title: "Ожидают вашего действия",
      description:
        "Задачи, которые система просит выполнить именно вас. Откройте задачу, чтобы перейти к нужному экрану; число в заголовке показывает, сколько их осталось.",
    },
    {
      target: "desk-waves",
      title: "Карьерные сессии",
      description:
        "Открытые карьерные сессии. Отсюда можно подать заявку на карьерный переход или вернуться к начатой.",
    },
    {
      target: "desk-scenarios",
      title: "Что можно сделать",
      description:
        "Готовые сценарии для ваших ролей: каждый ведёт к нужному экрану и подсказывает порядок действий.",
    },
  ],
  "/desk/review/:versionId": [
    {
      title: "Согласовать версию",
      description:
        "Вы видите версию матрицы, присланную на согласование. Согласуйте её, верните автору с комментарием или внесите правки сами.",
    },
    PATH_STEP,
    {
      target: "scenario-context",
      title: "О версии",
      description:
        "Подразделение, автор и состояние версии. Меню рядом открывает другие версии, которые ждут вас.",
    },
    {
      target: "review-changes",
      title: "Что изменилось",
      description:
        "Отличия от действующей версии: по ним удобно решать, согласовывать или нет.",
    },
    {
      target: "scenario-actions",
      title: "Решение",
      description:
        "Согласуйте версию, верните её с правками или внесите правки сами. Если действие недоступно, рядом написано почему.",
    },
  ],
  "/desk/rework/:versionId": [
    {
      title: "Доработать возвращённую версию",
      description:
        "Версию вернули с комментарием. Прочитайте его и решите: принять правки согласующего или переделать версию по-своему.",
    },
    PATH_STEP,
    {
      target: "rework-comment",
      title: "Комментарий согласующего",
      description: "Что именно просили исправить.",
    },
    {
      target: "rework-decision",
      title: "Ваше решение",
      description:
        "Принять правки, принять и доработать или вернуться к своему варианту. Под каждым вариантом сказано, что будет дальше.",
    },
  ],
  [deskPaths.drafts]: DRAFTS_STEPS,
  "/desk/drafts/:kind/:entryId": DRAFTS_STEPS,
  [deskPaths.matrixBuilder]: BUILDER_STEPS,
  "/desk/matrix-builder/:versionId": BUILDER_STEPS,
  [deskPaths.trackPositions]: TRACK_POSITIONS_STEPS,
  "/desk/track-positions/:trackId": TRACK_POSITIONS_STEPS,
  [deskPaths.catalogCleanup]: [
    {
      title: "Привести каталог в порядок",
      description:
        "Записи каталога, которые нигде не используются. Уберите лишнее, чтобы каталог оставался понятным.",
    },
    ...QUEUE_STEPS,
  ],
  [deskPaths.myPlace]: [
    {
      title: "Моё место в компании",
      description:
        "Ваше подразделение, должность и карьерные треки: так видно, на каком вы уровне и что нужно для следующего.",
    },
    PATH_STEP,
    {
      target: "place-unit",
      title: "Подразделение и должность",
      description:
        "Где вы работаете, кто ваш руководитель и ответственный HR: к ним можно обратиться с вопросами.",
    },
    {
      target: "place-tracks",
      title: "Мои карьерные треки",
      description:
        "Треки вашей должности и требования к следующим уровням: по ним видно, что подтянуть.",
    },
  ],
  [deskPaths.connectUnit]: CONNECT_UNIT_STEPS,
  "/desk/connect-unit/:unitId": CONNECT_UNIT_STEPS,
  [deskPaths.onboardEmployee]: [
    {
      title: "Принять сотрудника",
      description:
        "Два шага: сначала карточка сотрудника в оргструктуре, затем учётная запись, чтобы он мог войти в систему.",
    },
    PATH_STEP,
    {
      target: "wizard-steps",
      title: "Шаги",
      description: "Выполненные шаги можно открыть снова и поправить.",
    },
    {
      target: "scenario-form",
      title: "Данные шага",
      description: "Заполните поля текущего шага и сохраните.",
    },
    {
      target: "scenario-actions",
      title: "Действия",
      description: "Вернитесь на шаг назад или завершите приём.",
    },
  ],
  [deskPaths.orgCleanup]: [
    {
      title: "Навести порядок в оргструктуре",
      description:
        "Подразделения и сотрудники, у которых чего-то не хватает или данные расходятся. Исправьте их, чтобы оргструктура была полной.",
    },
    ...QUEUE_STEPS,
  ],
  [deskPaths.orgImport]: [
    {
      title: "Импорт оргструктуры",
      description:
        "Оргструктуру можно загрузить из файла .xlsx. Перед записью вы увидите, что изменится, и сможете отказаться.",
    },
    PATH_STEP,
    {
      target: "wizard-steps",
      title: "Шаги импорта",
      description:
        "Загрузка файла, предпросмотр, отсутствующие подразделения, руководители и итог.",
    },
    {
      target: "import-flow",
      title: "Текущий шаг",
      description:
        "Следуйте подсказкам на экране: в конце система покажет, что загружено, а что пропущено.",
    },
  ],
};
