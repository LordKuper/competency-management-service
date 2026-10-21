import type { MatrixProblem, MatrixVersion } from "../career/matrixApi";
import type { ApprovalStep } from "../career/matrixLabels";
import { BUILDER_STEPS } from "./deskTexts";

const PLURAL = new Intl.PluralRules("ru-RU");

const REMARK_FORMS: Record<string, string> = {
  one: "замечание",
  few: "замечания",
};

/** «1 замечание» / «2 замечания» / «5 замечаний». */
export function remarksText(count: number): string {
  return `${count} ${REMARK_FORMS[PLURAL.select(count)] ?? "замечаний"}`;
}

/** The wizard step where each problem of a version is put right; none for a problem put right outside the wizard. */
export const PROBLEM_STEP: Record<MatrixProblem["code"], number | null> = {
  NoLevels: 1,
  BlockWeights: 4,
  InactiveEntry: 2,
  ForeignCompetency: 2,
  ScoreOutOfScale: 3,
  LevelWithoutActivePosition: null,
  NoCompetencies: 2,
  DuplicateCompetency: 2,
  PositionUnavailable: null,
  SharedBlockUnavailable: 2,
};

/** «Шаг 3 «Требования»». */
export function stepText(step: number): string {
  return `Шаг ${step} «${BUILDER_STEPS[step - 1]}»`;
}

/** Whose turn it is on a version waiting for a decision, in words. */
export const WAITING_STEP_TEXTS: Record<NonNullable<ApprovalStep>, string> = {
  Superior: "шаг 1, вышестоящий руководитель",
  Hr: "шаг 2, HR",
};

/** What the toast says after a decision on a version: that it was published, with how many records, or `otherwise`. */
export function outcomeText(
  saved: MatrixVersion,
  publishedCount: number,
  otherwise: string,
): string {
  if (saved.status !== "Published") return otherwise;
  return publishedCount > 0
    ? `Версия опубликована; вместе с ней опубликовано записей: ${publishedCount}`
    : "Версия опубликована";
}
