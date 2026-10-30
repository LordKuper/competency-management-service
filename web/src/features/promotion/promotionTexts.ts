import dayjs from "dayjs";
import type { components } from "../../api/schema";
import type {
  FormalFailure,
  RollbackEffectKind,
  RollbackPreview,
} from "./promotionApi";

/** Number of stages of a wave. */
export const STAGE_COUNT = 6;

/** Numbers of the stages the screens act on. */
export const STAGE = {
  submission: 1,
  formalCheck: 2,
  rating: 3,
  artifacts: 4,
  review: 5,
  feedback: 6,
} as const;

/** Names of the stages, in order. */
export const STAGE_TITLES = [
  "Подача заявок",
  "Формальная проверка",
  "Круговая оценка",
  "Сбор артефактов",
  "Рассмотрение комиссией",
  "Обратная связь",
] as const;

/** Where a wave stands for its computed stage: 0 before it starts, 1 to 6 a running stage, above that over. */
export function stageLabel(stage: number): string {
  if (stage < 1) return "Ещё не началась";
  return STAGE_TITLES[stage - 1] ?? "Завершена";
}

/** Day shown as the people of the organization write it. */
export function formatDate(isoDate: string): string {
  return dayjs(isoDate).format("DD.MM.YYYY");
}

/** The failure of the formal check in a word, for a table cell. */
export const FORMAL_FAILURE_SHORT: Record<FormalFailure, string> = {
  MissingDate: "нет даты",
  IntervalNotPassed: "интервал",
  EducationBelowRequired: "образование",
};

/** Months between two days, whole months only. */
export function monthsBetween(from: string, to: string): number {
  return dayjs(to).diff(dayjs(from), "month");
}

/** What a condition of the formal check that the application fails means to the HR. */
export const FORMAL_FAILURE_TEXTS: Record<FormalFailure, string> = {
  MissingDate: "Не внесена дата приёма на работу или последнего перехода",
  IntervalNotPassed:
    "С приёма на работу или последнего перехода прошло мало времени",
  EducationBelowRequired: "Образование кандидата ниже требуемого для цели",
};

type ApplicationStatus = components["schemas"]["ApplicationStatus"];

/** Label and tag colour of an application status. */
export const APPLICATION_STATUS_TAGS: Record<
  ApplicationStatus,
  { label: string; color: string }
> = {
  Draft: { label: "Черновик", color: "default" },
  Submitted: { label: "Подана", color: "processing" },
  Admitted: { label: "Допущена", color: "processing" },
  FormalRejected: {
    label: "Отклонена по формальным признакам",
    color: "error",
  },
  UnderReview: { label: "На рассмотрении комиссии", color: "processing" },
  Decided: { label: "Решение принято", color: "success" },
  NoDecision: { label: "Решение не принято", color: "default" },
  Withdrawn: { label: "Отозвана", color: "default" },
};

type PromotionDecision = NonNullable<
  components["schemas"]["PromotionDecision"]
>;

/** Label and tag colour of the decision of the commission. */
export const DECISION_TAGS: Record<
  PromotionDecision,
  { label: string; color: string }
> = {
  Meets: { label: "Соответствует критериям для перехода", color: "success" },
  DoesNotMeet: {
    label: "Не соответствует критериям для перехода",
    color: "warning",
  },
};

/** What a candidate is told when the education they state is below the one the target asks for. */
export const EDUCATION_WARNING =
  "Образование ниже требуемого для этого уровня: заявка будет отклонена по формальным признакам. Отправить её всё равно можно.";

/** Where the education a draft starts with comes from and what changing it does. */
export const EDUCATION_NOTE =
  "Из вашего профиля. Если измените, профиль обновится при отправке заявки.";

/** The mark of a person the administrator forbids to name as a rater. */
export const RATER_FORBIDDEN = "Указание этого сотрудника запрещено";

/** What a candidate is asked for when listing the people who rate them. */
export const RATERS_HINT =
  "Укажите не меньше 5 человек, с которыми вы работали последние полгода; не меньше 2 из них должны быть из других подразделений. Для сотрудника без учётной записи укажите электронную почту: на неё придёт приглашение в систему.";

/** What a head is asked for when correcting the people who rate a candidate. */
export const HEAD_RATERS_HINT =
  "Не меньше 5 оценивающих, из них не меньше 2 из других подразделений, чем подразделение кандидата. Для оценивающего без учётной записи подтвердите почту.";

/** What a candidate owes for a competency that needs an artifact. */
export const ARTIFACT_MANDATORY_EXPLANATION =
  "Без артефакта эта компетенция, скорее всего, не будет подтверждена.";

/** Where the results of a wave are published, which is no stage of its own. */
export const PUBLICATION_NOTE = "В конце этапа публикуются результаты";

/** What the stage that publishes the results shows once they are published. */
export const PUBLISHED_NOTE = "Результаты опубликованы";

/** The title of the moment the results are published. */
export const PUBLICATION_TITLE = "Публикация результатов";

/** What the responsible HR sees of the check the entered data would pass now. */
export const FORMAL_CHECK_PASSED =
  "По внесённым данным заявка пройдёт формальную проверку";

/** What the responsible HR sees when the entered data would not pass the check. */
export const FORMAL_CHECK_FAILED =
  "По внесённым данным заявка не пройдёт формальную проверку";

/** What is shown where a value is not there yet. */
export const NO_VALUE = "—";

/** The statuses of applications that reached the commission. */
export const REVIEWED_STATUSES: readonly ApplicationStatus[] = [
  "UnderReview",
  "Decided",
  "NoDecision",
];

const ONE_DECIMAL = new Intl.NumberFormat("ru-RU", {
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});

/** A mean of scores, rounded to a tenth only here, for the eye. */
export function formatAverage(value: number): string {
  return ONE_DECIMAL.format(value);
}

const PERCENT = new Intl.NumberFormat("ru-RU", { maximumFractionDigits: 1 });

/** A rating or threshold in percent, rounded to a tenth only here, for the eye. */
export function formatPercent(value: number | null): string {
  return value === null ? NO_VALUE : `${PERCENT.format(value)} %`;
}

const SIGNED_DELTA = new Intl.NumberFormat("ru-RU", {
  signDisplay: "exceptZero",
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});

/** A change of an average or of a rating as the server gave it, with its sign. */
export function formatDelta(value: number): string {
  return SIGNED_DELTA.format(value);
}

/** Day and month of a date, as the stage dates of a stepper are written. */
export function formatShortDate(isoDate: string): string {
  return dayjs(isoDate).format("DD.MM");
}

/** A scale point as a choice: its score and its meaning. */
export function scalePointLabel(point: { score: number; label: string }) {
  return `${point.score} — ${point.label}`;
}

const PLURAL = new Intl.PluralRules("ru-RU");

const COMPETENCY_FORMS: Record<string, string> = {
  one: "компетенция",
  few: "компетенции",
};

/** «1 компетенция» / «2 компетенции» / «5 компетенций». */
export function competenciesText(count: number): string {
  return `${count} ${COMPETENCY_FORMS[PLURAL.select(count)] ?? "компетенций"}`;
}

/** The tag over the competencies a candidate left without a score. */
export function unscoredText(count: number): string {
  return `${count === 1 ? "Не оценена" : "Не оценены"} ${competenciesText(count)} — считаются как 0`;
}

const RATER_FORMS: Record<string, string> = {
  one: "оценивающий",
  few: "оценивающих",
};

/** «1 оценивающий» / «2 оценивающих» / «5 оценивающих». */
export function raterCountText(count: number): string {
  return `${count} ${RATER_FORMS[PLURAL.select(count)] ?? "оценивающих"}`;
}

const ARTIFACT_FORMS: Record<string, string> = {
  one: "артефакт",
  few: "артефакта",
};

/** «1 артефакт» / «2 артефакта» / «5 артефактов». */
export function artifactsText(count: number): string {
  return `${count} ${ARTIFACT_FORMS[PLURAL.select(count)] ?? "артефактов"}`;
}

const APPLICATION_FORMS: Record<string, string> = {
  one: "заявка",
  few: "заявки",
};

/** «1 заявка» / «2 заявки» / «5 заявок». */
function applicationsText(count: number): string {
  return `${count} ${APPLICATION_FORMS[PLURAL.select(count)] ?? "заявок"}`;
}

/** What sending a wave back to a stage does to the applications, one line per kind of change, with the number of applications. */
const ROLLBACK_EFFECT_TEXTS: Record<RollbackEffectKind, string> = {
  FormalAdmittedReset:
    "Допущенные заявки вернутся в статус «Подана», формальная проверка пройдёт заново",
  FormalRejectedReset:
    "Отклонённые по формальным признакам заявки вернутся в статус «Подана», формальная проверка пройдёт заново",
  NoDecisionCleared: "Отметка «Решение не принято» будет снята",
  RatingRecalculated: "Итог круговой оценки будет пересчитан",
  ArtifactsRecalculated:
    "Отметка о недостающих обязательных артефактах будет пересчитана",
  ResultsHidden:
    "Кандидаты перестанут видеть опубликованные результаты до новой публикации",
};

/** The consequences of sending a wave back to a stage as lines for the warning: what resets or is computed again, then the letters. */
export function rollbackConsequences(preview: RollbackPreview): string[] {
  return [
    ...preview.effects.map(
      ({ kind, count }) =>
        `${ROLLBACK_EFFECT_TEXTS[kind]} — ${applicationsText(count)}`,
    ),
    ...(preview.letterStages.length > 0
      ? [
          `Письма о завершении этапов будут отправлены заново: ${preview.letterStages
            .map((stage) => `«${STAGE_TITLES[stage - 1]}»`)
            .join(", ")}`,
        ]
      : []),
    ...(preview.raterReminderAgain
      ? ["Оценивающим снова придёт напоминание об оценке"]
      : []),
  ];
}
