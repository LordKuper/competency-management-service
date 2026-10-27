import type { components } from "../../api/schema";
import type { MatrixDecision, MatrixVersionStatus } from "./matrixApi";

/** Step of the route a version is at; `null` means no approval step (the author's or the end). */
export type ApprovalStep = components["schemas"]["ApprovalStep"];

/** What a matrix is called with its unit: «Матрица «трек»» or, for a position matrix, «Матрица должности «должность»». */
export function matrixName(subject: {
  trackName?: string | null;
  positionName?: string | null;
}): string {
  return subject.trackName
    ? `Матрица «${subject.trackName}»`
    : `Матрица должности «${subject.positionName ?? ""}»`;
}

export const SKIPPED_NOTE = "Пропущен — нет руководителя с учётной записью";
export const AUTO_APPROVED_NOTE =
  "Согласован автоматически — HR является автором";
export const CHANGED_NOTE = "Изменено согласующим";

export const STEP_TITLES = [
  "Автор",
  "Вышестоящий руководитель",
  "HR",
  "Публикация",
] as const;

/** Names of the approval steps a decision or an approval row can carry. */
export const APPROVAL_STEP_LABELS: Record<NonNullable<ApprovalStep>, string> = {
  Superior: STEP_TITLES[1],
  Hr: STEP_TITLES[2],
};

export const VERSION_STATUS_LABELS: Record<MatrixVersionStatus, string> = {
  Draft: "Черновик",
  OnReview: "На согласовании",
  Returned: "Возвращена с правками",
  Approved: "Согласована",
  Published: "Опубликована",
  Archived: "В архиве",
};

export const DECISION_TEXTS: Record<MatrixDecision["kind"], string> = {
  Submitted: "отправил на согласование",
  Skipped: SKIPPED_NOTE,
  Approved: "согласовал",
  AutoApproved: AUTO_APPROVED_NOTE,
  Returned: "вернул с правками",
  AcceptedChanges: "принял правки",
  AcceptedAndRevising: "принял и дорабатывает",
  RolledBack: "откатил правки",
  Published: "опубликовано",
};

/** A level of education a matrix level can require. */
export type EducationLevel = NonNullable<
  components["schemas"]["EducationLevel"]
>;

/** Names of the education levels, lowest first. */
export const EDUCATION_LABELS: Record<EducationLevel, string> = {
  SecondaryGeneral: "Среднее общее",
  SecondaryVocational: "Среднее профессиональное",
  Bachelor: "Высшее — бакалавриат",
  SpecialistOrMaster: "Высшее — специалитет, магистратура",
  HigherQualification: "Высшее — подготовка кадров высшей квалификации",
};
