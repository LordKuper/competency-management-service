import { EDUCATION_LABELS, type EducationLevel } from "../career/matrixLabels";

const EDUCATION_ORDER = Object.keys(EDUCATION_LABELS) as EducationLevel[];

/** Whether the stated education is lower than the required one; no requirement or no statement is never below. */
export function isEducationBelow(
  stated: EducationLevel | null | undefined,
  required: EducationLevel | null | undefined,
): boolean {
  if (!stated || !required) return false;
  return EDUCATION_ORDER.indexOf(stated) < EDUCATION_ORDER.indexOf(required);
}
