import { HOME_PATH } from "../../app/featureContract";
import { paths as orgPaths } from "../org-structure/paths";
import type {
  DeskGroup,
  DeskItem,
  DeskKind,
  DeskObject,
  DeskReason,
} from "./deskApi";

/** A scenario or section a desk entry leads to. */
export interface DeskLink {
  /** Russian name of the scenario or section. */
  title: string;
  path: string;
}

/** Addresses of the desk and its scenario screens; the review, rework and drafts addresses are sent in e-mails. */
export const deskPaths = {
  desk: HOME_PATH,
  review: (versionId: string) => `/desk/review/${versionId}`,
  rework: (versionId: string) => `/desk/rework/${versionId}`,
  drafts: "/desk/drafts",
  draft: (kind: string, entryId: string) => `/desk/drafts/${kind}/${entryId}`,
  matrixBuilder: "/desk/matrix-builder",
  matrixBuilderOf: (versionId: string) => `/desk/matrix-builder/${versionId}`,
  connectUnit: "/desk/connect-unit",
  connectUnitOf: (unitId: string) => `/desk/connect-unit/${unitId}`,
  onboardEmployee: "/desk/onboard-employee",
  myPlace: "/desk/my-place",
  trackPositions: "/desk/track-positions",
  trackPositionsOf: (trackId: string) => `/desk/track-positions/${trackId}`,
  catalogCleanup: "/desk/catalog-cleanup",
  orgCleanup: "/desk/org-cleanup",
} as const;

/** Titles of the matrix builder steps, in order. */
export const BUILDER_STEPS = [
  "Трек и шкала",
  "Компетенции",
  "Требования",
  "Формула и пороги",
  "Проверка полноты",
  "Отправка",
] as const;

/** The step names for a matrix built on a track or, when `isPosition`, on a position, whose first step is named for it. */
export function builderSteps(isPosition: boolean): readonly string[] {
  return isPosition
    ? ["Должность и шкала", ...BUILDER_STEPS.slice(1)]
    : BUILDER_STEPS;
}

const CAREER_SECTION = "Карьерная модель";
const ORG_SECTION = "Оргструктура";

const DRAFTS_QUEUE: DeskLink = {
  title: "Утвердить черновые записи",
  path: deskPaths.drafts,
};
const ORG_QUEUE: DeskLink = {
  title: "Навести порядок в оргструктуре",
  path: deskPaths.orgCleanup,
};
const CATALOG_QUEUE: DeskLink = {
  title: "Привести каталог в порядок",
  path: deskPaths.catalogCleanup,
};
const LINK_QUEUE: DeskLink = {
  title: "Связать должности с треком",
  path: deskPaths.trackPositions,
};

/** How the desk shows one group. */
interface GroupMeta {
  title: string;
  /** Items come from a dated event, so the group shows how long ago it happened. */
  isDated?: boolean;
  /** Queue scenario that works through the whole group. */
  queue?: DeskLink;
  /** Section where the objects of a reference group without a queue can be found. */
  section?: string;
}

/** Title and layout of every desk group, words built from the action rather than from a role. */
export const GROUPS: Record<DeskGroup, GroupMeta> = {
  ReturnedVersions: { title: "Версии возвращены вам", isDated: true },
  VersionsToApprove: {
    title: "Версии ждут вашего согласования",
    isDated: true,
  },
  UnsubmittedVersions: {
    title: "Продолжить построение матрицы",
    isDated: true,
  },
  DraftsToApprove: {
    title: "Черновые записи ждут утверждения",
    isDated: true,
    queue: DRAFTS_QUEUE,
  },
  MatricesToBuild: { title: "Построить или обновить матрицу" },
  PositionsToLink: { title: "Связать должности с треками" },
  UnitsToConnect: { title: "Подключить подразделения" },
  Invitations: { title: "Приглашения не приняты" },
  OrgCleanup: { title: "Навести порядок в оргструктуре", queue: ORG_QUEUE },
  UnitsWithoutMatrix: {
    title: "Подразделения без опубликованной матрицы",
    section: ORG_SECTION,
  },
  VersionsInApproval: {
    title: "Ваши версии на согласовании",
    isDated: true,
    section: CAREER_SECTION,
  },
  StuckVersions: {
    title: "Версии, задержавшиеся на шаге 1",
    isDated: true,
    section: CAREER_SECTION,
  },
  ChangedCompetencies: {
    title: "Компетенции изменились после публикации матрицы",
    section: CAREER_SECTION,
  },
  OwnDrafts: {
    title: "Ваши черновые записи ждут утверждения",
    isDated: true,
    section: CAREER_SECTION,
  },
  PositionLinksInfo: {
    title: "Треки и должности без связей",
    queue: LINK_QUEUE,
  },
  UnusedCatalogEntries: {
    title: "Неиспользуемые записи каталога",
    queue: CATALOG_QUEUE,
  },
  InactivePositions: {
    title: "Неактивные должности заняты сотрудниками",
    queue: ORG_QUEUE,
  },
  UnusedPositions: { title: "Неиспользуемые должности", queue: ORG_QUEUE },
  EmptyUnits: { title: "Пустые подразделения", queue: ORG_QUEUE },
  NoTrack: {
    title: "Нет карьерного трека для вашей должности",
    section: CAREER_SECTION,
  },
};

const TYPE_LABELS: Record<DeskObject["type"], string> = {
  Matrix: "Матрица",
  MatrixVersion: "Версия матрицы",
  OrgUnit: "Подразделение",
  Track: "Трек",
  Level: "Уровень",
  Direction: "Область",
  Category: "Категория",
  Competency: "Компетенция",
  Scale: "Шкала",
  Block: "Общий блок",
  Position: "Должность",
  Employee: "Сотрудник",
  UserAccount: "Учётная запись",
};

const DRAFT_KINDS: Partial<Record<DeskObject["type"], string>> = {
  Direction: "direction",
  Track: "track",
  Category: "category",
  Competency: "competency",
  Scale: "scale",
  Block: "block",
};

const DATE = new Intl.DateTimeFormat("ru-RU");

const RELATIVE = new Intl.RelativeTimeFormat("ru", { numeric: "always" });
const MINUTE = 60_000;
const MINUTES_PER_HOUR = 60;
const HOURS_PER_DAY = 24;

/** How far a moment is from now in words, such as "3 дня назад" or "через 2 дня". */
export function relativeTime(value: string): string {
  const minutes = Math.trunc((new Date(value).getTime() - Date.now()) / MINUTE);
  if (Math.abs(minutes) < MINUTES_PER_HOUR)
    return RELATIVE.format(minutes, "minute");
  const hours = Math.trunc(minutes / MINUTES_PER_HOUR);
  if (Math.abs(hours) < HOURS_PER_DAY) return RELATIVE.format(hours, "hour");
  return RELATIVE.format(Math.trunc(hours / HOURS_PER_DAY), "day");
}

/** A date as the desk writes it, such as 09.10.2026. */
export function formatDate(value: string): string {
  return DATE.format(new Date(value));
}

/** "Фамилия И. О." from a full name written surname first; an e-mail or a single word stays as it is. */
export function shortName(fullName: string): string {
  const [surname, ...given] = fullName.trim().split(/\s+/);
  if (!surname || given.length === 0) return fullName;
  return [surname, given.map((part) => `${part[0]}.`).join(" ")].join(" ");
}

const quoted = (objects: ReadonlyArray<DeskObject>) =>
  objects.map((object) => `«${object.name}»`).join(", ");

/** The type tag shown before an object's name; matrices and units carry the type in the name, invitations need none. */
export function typeLabelOf(item: DeskItem): string | null {
  const { type } = item.object;
  if (type === "Matrix" || type === "OrgUnit" || type === "MatrixVersion")
    return null;
  if (type === "UserAccount" && item.group === "Invitations") return null;
  return TYPE_LABELS[type];
}

/** The object's name as the row shows it. */
export function objectTitle(object: DeskObject): string {
  switch (object.type) {
    case "Matrix":
      return [`Матрица «${object.name}»`, object.context?.at(-1)]
        .filter(Boolean)
        .join(" — ");
    case "OrgUnit":
      return `Подразделение «${object.name}»`;
    default:
      return object.name;
  }
}

/** The line under the object's name: where it sits, its version or its author. */
export function objectContext(item: DeskItem): string | null {
  const { object } = item;
  const [first] = item.reasons;
  const details = first?.details;
  const author = details?.person
    ? `автор ${shortName(details.person.name)}`
    : null;
  switch (object.type) {
    case "Matrix":
      return matrixContext(first);
    case "OrgUnit":
      return object.context?.length ? object.context.join(" → ") : null;
    case "Track":
      return (
        [
          object.context?.at(-1),
          first?.kind === "DraftAwaitsApproval" ? author : null,
        ]
          .filter(Boolean)
          .join(" · ") || null
      );
    default:
      if (first?.kind === "DraftAwaitsApproval") {
        return ["Общая", author].filter(Boolean).join(" · ");
      }
      return object.context?.join(" · ") || null;
  }
}

function matrixContext(reason: DeskReason | undefined): string | null {
  const number = reason?.details?.number;
  if (!reason || number == null) return null;
  switch (reason.kind) {
    case "VersionReturned":
      return `Версия ${number} · Возвращена${reason.details?.hasChanges ? " с правками" : ""}`;
    case "VersionNotSubmitted":
      return `Версия ${number} · Черновик`;
    case "VersionAwaitsHeadApproval":
    case "VersionAwaitsHrApproval":
      return [
        `Версия ${number}`,
        reason.details?.person &&
          `автор ${shortName(reason.details.person.name)}`,
      ]
        .filter(Boolean)
        .join(" · ");
    case "MatrixUsesInactive":
    case "MatrixLevelsChanged":
    case "MatrixSharedBlockChanged":
    case "MatrixCompetencyChanged":
      return `Опубликована, версия ${number}`;
    default:
      return `Версия ${number}`;
  }
}

/** One line of an item's reasons; `isEarly` marks a draft track that is not ready to approve yet. */
export interface ReasonLine {
  text: string;
  isEarly?: boolean;
}

/** The reasons of an item in words, the most important first. */
export function reasonLines(item: DeskItem): ReasonLine[] {
  return item.reasons.flatMap((reason) => reasonOf(reason, item));
}

function reasonOf(reason: DeskReason, item: DeskItem): ReasonLine[] {
  const details = reason.details ?? {};
  const person = details.person ? shortName(details.person.name) : null;
  const entries = details.entries ?? [];
  const hrHint =
    item.isReference && person
      ? `. Править трек может ответственный HR: ${person}`
      : "";
  switch (reason.kind) {
    case "VersionReturned":
      return [{ text: `Согласующий вернул версию${returnedWith(details)}` }];
    case "VersionNotSubmitted": {
      const step = details.step ? BUILDER_STEPS[details.step - 1] : undefined;
      return [
        {
          text: `Черновик версии не отправлен на согласование${step ? ` (остановились на шаге ${details.step} «${step}»)` : ""}`,
        },
      ];
    }
    case "VersionAwaitsHeadApproval":
      return [
        {
          text: "Версия ждёт вашего согласования (шаг 1: вышестоящий руководитель)",
        },
      ];
    case "VersionAwaitsHrApproval":
      return [{ text: "Версия ждёт вашего согласования (шаг 2: HR)" }];
    case "MatrixUsesInactive":
      return [{ text: `Опирается на неактивное: ${inactiveEntries(entries)}` }];
    case "MatrixLevelsChanged":
      return [{ text: `Не покрывает уровни трека: ${levelChanges(details)}` }];
    case "MatrixSharedBlockChanged":
      return entries.map((block) => ({
        text: `Общий блок изменился: «${block.name}» — состав отличается от опубликованной версии`,
      }));
    case "MatrixCompetencyChanged":
      return [
        {
          text:
            entries.length > 1
              ? `Компетенции ${quoted(entries)} изменились после публикации матрицы`
              : `Компетенция ${quoted(entries)} изменилась после публикации матрицы`,
        },
      ];
    case "OwnVersionInApproval":
      return [
        {
          text: `Ваша версия на согласовании: ход у ${person ?? "согласующего"} (шаг ${details.step ?? 1})`,
        },
      ];
    case "VersionStuckAtHead":
      return [
        {
          text: `Версия на шаге 1 дольше 7 дней${person ? `: ход у ${person}` : ""}`,
        },
      ];
    case "UnitWithoutMatrix":
      return [
        {
          text:
            entries.length > 1
              ? `Нет опубликованной матрицы, хотя есть действующие треки ${quoted(entries)}`
              : `Нет опубликованной матрицы, хотя есть действующий трек ${quoted(entries)}`,
        },
      ];
    case "ResponsibleUnitWithoutMatrix":
      return [
        {
          text: `Нет опубликованной матрицы${details.person ? `. Руководитель: ${details.person.name}` : ""}`,
        },
      ];
    case "UnitWithoutHead":
      return [{ text: "Нет руководителя" }];
    case "ResponsibleUnitWithoutHead":
      return [
        {
          text: "Нет руководителя. Назначить руководителя вправе глобальный администратор",
        },
      ];
    case "UnitWithoutHr":
      return [{ text: `Нет ответственного HR${assignedHrState(details)}` }];
    case "UnitWithoutPositions":
      return [
        {
          text: `Перечень должностей пуст, а сотрудников: ${details.employeeCount ?? 0}`,
        },
      ];
    case "HeadWithoutAccount":
    case "HeadAccountInvited":
    case "HeadAccountBlocked":
      return [
        {
          text: `Руководитель${person ? ` ${person}` : ""} без действующей учётной записи: ${HEAD_ACCOUNT_STATES[reason.kind]}`,
        },
      ];
    case "EmptyUnit":
      return [{ text: "Подразделение пусто" }];
    case "DraftAwaitsApproval":
      return [
        { text: "Черновая запись ждёт утверждения" },
        ...earlyApproval(item, details),
      ];
    case "TrackWithoutLevels":
      return [{ text: `В треке нет уровней${hrHint}` }];
    case "TrackWithOneLevel":
      return [{ text: `У трека один уровень${hrHint}` }];
    case "TrackWithoutPositions":
      return [
        { text: `У трека нет связанных действующих должностей${hrHint}` },
      ];
    case "LevelWithoutPosition":
      return [{ text: `${levelsWithoutPosition(entries)}${hrHint}` }];
    case "PositionWithoutTrack":
      return [
        {
          text: `Должность не связана с карьерным треком; сотрудников: ${details.employeeCount ?? 0}${hrHint}`,
        },
      ];
    case "OwnDraftAwaitsApproval":
      return [{ text: "Ваша черновая запись ждёт утверждения HR" }];
    case "UnusedCompetency":
      return [{ text: "Компетенция нигде не используется" }];
    case "UnusedScale":
      return [{ text: "Шкала нигде не используется" }];
    case "UnusedCategory":
      return [{ text: "Категория нигде не используется" }];
    case "DirectionWithoutTracks":
      return [{ text: "У области нет треков" }];
    case "InactivePositionInUse":
      return [
        {
          text: `Неактивная должность занята сотрудниками: ${details.employeeCount ?? 0}`,
        },
      ];
    case "UnusedPosition":
      return [{ text: "Должность нигде не используется" }];
    case "PositionOutsideList": {
      const [position, unit] = entries;
      return [
        {
          text: `Должность «${position?.name ?? ""}» не входит в перечень подразделения «${unit?.name ?? ""}»`,
        },
      ];
    }
    case "NoTrackForPosition":
      return [
        {
          text: "Для вашей должности нет карьерного трека — обратитесь к руководителю",
        },
      ];
    case "AccountWithoutEmployee":
      return [{ text: "Учётная запись не привязана к сотруднику" }];
    case "InvitationNotAccepted":
      return [
        {
          text: `Приглашение не принято: ${linkState(reason, item.group !== "Invitations")}`,
        },
      ];
  }
}

const HEAD_ACCOUNT_STATES: Record<
  Extract<
    DeskKind,
    "HeadWithoutAccount" | "HeadAccountInvited" | "HeadAccountBlocked"
  >,
  string
> = {
  HeadWithoutAccount: "нет учётной записи",
  HeadAccountInvited: "приглашение не принято",
  HeadAccountBlocked: "учётная запись заблокирована",
};

function returnedWith(details: NonNullable<DeskReason["details"]>): string {
  if (details.hasChanges && details.hasComment)
    return " с правками и комментарием";
  if (details.hasChanges) return " с правками";
  if (details.hasComment) return " с комментарием";
  return "";
}

const INACTIVE_FORMS: Partial<Record<DeskObject["type"], string>> = {
  Track: "неактивен",
  Direction: "неактивна",
  Competency: "неактивна",
  Block: "неактивен",
  Position: "неактивна",
};

function inactiveEntries(entries: ReadonlyArray<DeskObject>): string {
  const named = entries
    .map((entry) => `${TYPE_LABELS[entry.type].toLowerCase()} «${entry.name}»`)
    .join(", ");
  const [only] = entries;
  const form =
    entries.length === 1 && only ? INACTIVE_FORMS[only.type] : "неактивны";
  return `${named} ${form ?? "неактивны"}`;
}

function levelChanges(details: NonNullable<DeskReason["details"]>): string {
  const change = (
    levels: ReadonlyArray<DeskObject> | null | undefined,
    one: string,
    many: string,
  ) =>
    levels?.length
      ? `${levels.length > 1 ? many : one} ${quoted(levels)}`
      : null;
  return [
    change(details.addedLevels, "добавлен", "добавлены"),
    change(details.removedLevels, "удалён", "удалены"),
    change(details.movedLevels, "переставлен", "переставлены"),
  ]
    .filter(Boolean)
    .join(", ");
}

function levelsWithoutPosition(levels: ReadonlyArray<DeskObject>): string {
  return levels.length > 1
    ? `У уровней ${quoted(levels)} нет действующих должностей`
    : `У уровня ${quoted(levels)} нет действующей должности`;
}

function earlyApproval(
  item: DeskItem,
  details: NonNullable<DeskReason["details"]>,
): ReasonLine[] {
  if (item.object.type !== "Track") return [];
  if (details.levelCount === 0)
    return [{ text: "У трека нет уровней", isEarly: true }];
  if (details.entries?.length)
    return [{ text: levelsWithoutPosition(details.entries), isEarly: true }];
  return [];
}

function assignedHrState(details: NonNullable<DeskReason["details"]>): string {
  switch (details.accountState) {
    case "Blocked":
      return ": закреплённая учётная запись заблокирована";
    case "Invited":
      return ": закреплённая учётная запись не приняла приглашение";
    case "None":
      return ": закреплённая учётная запись не может действовать";
    default:
      return "";
  }
}

function linkState(reason: DeskReason, withDate: boolean): string {
  const date =
    withDate && reason.occurredAt ? ` ${formatDate(reason.occurredAt)}` : "";
  switch (reason.details?.linkState) {
    case "ExpiringSoon":
      return withDate ? `ссылка истечёт${date}` : "ссылка скоро истечёт";
    case "Revoked":
      return "ссылка отменена";
    default:
      return `ссылка истекла${date}`;
  }
}

/** The scenario screen an action item opens, with the label of its row action; null when the action is on the desk itself. */
export function itemAction(item: DeskItem): DeskLink | null {
  const [first] = item.reasons;
  const { object, target } = item;
  const details = first?.details;
  const versionLink = (title: string, path: (versionId: string) => string) =>
    target ? { title, path: path(target.id) } : null;
  switch (first?.kind) {
    case "VersionReturned":
      return versionLink("Доработать", deskPaths.rework);
    case "VersionNotSubmitted":
      return versionLink("Продолжить", deskPaths.matrixBuilderOf);
    case "VersionAwaitsHeadApproval":
    case "VersionAwaitsHrApproval":
      return versionLink("Рассмотреть", deskPaths.review);
    case "DraftAwaitsApproval": {
      const kind = DRAFT_KINDS[object.type];
      return kind
        ? { title: "Утвердить", path: deskPaths.draft(kind, object.id) }
        : { title: "Утвердить", path: deskPaths.drafts };
    }
    case "UnitWithoutMatrix":
      return {
        title: "Построить матрицу",
        path: `${deskPaths.matrixBuilder}?${builderPreselection(object, details?.entries)}`,
      };
    case "MatrixUsesInactive":
    case "MatrixLevelsChanged":
      return {
        title: "Создать новую версию",
        path: `/career/matrices/${object.id}`,
      };
    case "MatrixSharedBlockChanged":
      return {
        title: "Создать новую версию",
        path: `${deskPaths.matrixBuilder}?${new URLSearchParams({ matrixId: object.id })}`,
      };
    case "TrackWithoutLevels":
    case "TrackWithOneLevel":
      return { title: "Править трек", path: "/career" };
    case "TrackWithoutPositions":
    case "LevelWithoutPosition":
      return {
        title: "Связать должности",
        path: deskPaths.trackPositionsOf(object.id),
      };
    case "PositionWithoutTrack":
      return { title: "Связать должности", path: deskPaths.trackPositions };
    case "UnitWithoutHead":
    case "HeadWithoutAccount":
    case "HeadAccountInvited":
    case "HeadAccountBlocked":
    case "UnitWithoutHr":
    case "UnitWithoutPositions":
      return { title: "Подключить", path: deskPaths.connectUnitOf(object.id) };
    case "PositionOutsideList":
    case "AccountWithoutEmployee":
      return { title: "Привести в порядок", path: deskPaths.orgCleanup };
    default:
      return null;
  }
}

function builderPreselection(
  unit: DeskObject,
  tracks: ReadonlyArray<DeskObject> | null | undefined,
): URLSearchParams {
  const [track] = tracks ?? [];
  return new URLSearchParams(
    tracks?.length === 1 && track
      ? { unitId: unit.id, trackId: track.id }
      : { unitId: unit.id },
  );
}

/** Where an item's object name leads: the scenario of an action, or a view of the object for reference. */
export function itemLink(item: DeskItem): string | null {
  if (!item.isReference) return itemAction(item)?.path ?? null;
  const { object } = item;
  switch (item.reasons[0]?.kind) {
    case "ResponsibleUnitWithoutMatrix":
    case "ResponsibleUnitWithoutHead":
      return `${orgPaths.tree}?${new URLSearchParams({ unit: object.id })}`;
    case "OwnVersionInApproval":
    case "VersionStuckAtHead":
    case "MatrixCompetencyChanged":
      return `/career/matrices/${object.id}`;
    case "OwnDraftAwaitsApproval":
      return "/career";
    case "NoTrackForPosition":
      return deskPaths.myPlace;
    case "TrackWithoutLevels":
    case "TrackWithOneLevel":
    case "TrackWithoutPositions":
    case "LevelWithoutPosition":
      return deskPaths.trackPositionsOf(object.id);
    default:
      return GROUPS[item.group].queue?.path ?? null;
  }
}
