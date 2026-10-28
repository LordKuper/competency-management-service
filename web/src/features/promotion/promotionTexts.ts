import dayjs from "dayjs";
import type { components } from "../../api/schema";
import type { FormalFailure } from "./promotionApi";

/** Number of stages of a wave. */
export const STAGE_COUNT = 7;

/** Numbers of the stages the screens act on. */
export const STAGE = {
  submission: 1,
  formalCheck: 2,
  rating: 3,
  artifacts: 4,
  feedback: 7,
} as const;

/** Names of the stages, in order. */
export const STAGE_TITLES = [
  "Подача заявок",
  "Формальная проверка",
  "Круговая оценка",
  "Сбор артефактов",
  "Рассмотрение комиссией",
  "Публикация результатов",
  "Обратная связь",
] as const;

/** Where a wave stands for its computed stage: 0 before it starts, 1 to 7 a running stage, above that over. */
export function stageLabel(stage: number): string {
  if (stage < 1) return "Ещё не началась";
  return STAGE_TITLES[stage - 1] ?? "Завершена";
}

/** Day shown as the people of the organization write it. */
export function formatDate(isoDate: string): string {
  return dayjs(isoDate).format("DD.MM.YYYY");
}

/** What a condition of the formal check that the application fails means to the HR. */
export const FORMAL_FAILURE_TEXTS: Record<FormalFailure, string> = {
  MissingDate: "Не внесена дата приёма на работу или последнего повышения",
  IntervalNotPassed:
    "С приёма на работу или последнего повышения прошло мало времени",
  EducationBelowRequired: "Образование кандидата ниже требуемого для цели",
};

type ApplicationStatus = components["schemas"]["ApplicationStatus"];

/** Label and tag colour of an application status. */
export const APPLICATION_STATUS_TAGS: Record<
  ApplicationStatus,
  { label: string; color: string }
> = {
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
  Meets: { label: "Соответствует критериям для повышения", color: "success" },
  DoesNotMeet: {
    label: "Не соответствует критериям для повышения",
    color: "warning",
  },
};

/** What a candidate is told when the education they state is below the one the target asks for. */
export const EDUCATION_WARNING =
  "Образование ниже требуемого для этого уровня: заявка будет отклонена по формальным признакам. Отправить её всё равно можно.";

/** What a candidate is asked for when listing the people who rate them. */
export const RATERS_HINT =
  "Укажите не меньше 5 человек, с которыми вы работали последние полгода; не меньше 2 из них должны быть из других подразделений.";

/** What a head is asked for when correcting the people who rate a candidate. */
export const HEAD_RATERS_HINT =
  "Не меньше 5 оценивающих, из них не меньше 2 из других подразделений, чем подразделение кандидата.";

/** A scale point as a choice: its score and its meaning. */
export function scalePointLabel(point: { score: number; label: string }) {
  return `${point.score} — ${point.label}`;
}
