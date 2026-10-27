import { isEmployeeKept, type KeptIds } from "./importPlan";
import type {
  ImportHeadDecision,
  ImportHeadUnit,
  OrgImportPlan,
} from "./orgImportApi";

/** The heads the user chose, by the key of the unit; null is «Без руководителя». A unit without an entry takes the suggested head. */
export type HeadChoices = ReadonlyMap<string, string | null>;

/** An employee who works in a unit after the apply. */
export interface HeadOption {
  key: string;
  fullName: string;
}

/** One unit of the head step with the head it gets as things stand. */
export interface HeadRow {
  unit: ImportHeadUnit;
  /** Who heads the unit now, by name. */
  currentHeadName: string | null;
  /** Whether the current head works in the unit after the apply. */
  isCurrentHeadStaying: boolean;
  /** Employees who work in the unit after the apply: the file's, and the missing ones the decisions keep. */
  options: readonly HeadOption[];
  /** The head the unit gets; null leaves it without one. */
  chosen: string | null;
  /** Several candidates and nobody preselected or chosen yet. */
  isUndecided: boolean;
  /** The current head stays but is not among the recognised candidates; names the candidates. */
  mismatch: string | null;
  /** Several candidates, or a mismatch: the rows the filter «Требуют внимания» keeps. */
  needsAttention: boolean;
}

/** The heads a unit may have after the apply, as lists of employee keys, by unit. */
function keptWorkers(plan: OrgImportPlan, kept: KeptIds) {
  const byUnit = new Map<string, string[]>();
  for (const employee of plan.missingEmployees) {
    if (!isEmployeeKept(employee, kept)) continue;
    for (const unitId of new Set(employee.orgUnitIds)) {
      byUnit.set(unitId, [...(byUnit.get(unitId) ?? []), employee.id]);
    }
  }
  return byUnit;
}

/**
 * Every unit of the head step with its head: the user's choice while it is still allowed, else the current head when he
 * stays, else the only candidate, else nobody. Computed from the decisions about the missing entities, so a change to
 * them moves the defaults and drops a choice that is no longer allowed.
 */
export function headRows(
  plan: OrgImportPlan,
  kept: KeptIds,
  choices: HeadChoices,
): HeadRow[] {
  const names = new Map([
    ...plan.missingEmployees.map(({ id, fullName }) => [id, fullName] as const),
    ...plan.heads.employees.map(
      ({ key, fullName }) => [key, fullName] as const,
    ),
  ]);
  const nameOf = (key: string) => names.get(key) ?? key;
  const keptByUnit = keptWorkers(plan, kept);

  return plan.heads.units.map((unit) => {
    const stayingMissing = unit.unitId
      ? (keptByUnit.get(unit.unitId) ?? [])
      : [];
    const keys = [...new Set([...unit.employees, ...stayingMissing])];
    const { currentHead, candidates } = unit;
    const stayingHead =
      currentHead !== null &&
      (unit.currentHeadStays || keys.includes(currentHead))
        ? currentHead
        : null;
    const suggested =
      stayingHead ?? (candidates.length === 1 ? (candidates[0] ?? null) : null);
    const choice = choices.get(unit.key);
    const explicit =
      choice !== undefined && (choice === null || keys.includes(choice))
        ? choice
        : undefined;
    const mismatch =
      stayingHead !== null &&
      candidates.length > 0 &&
      !candidates.includes(stayingHead)
        ? `Действующий руководитель не распознан по должности. Распознаны: ${candidates.map(nameOf).join(", ")}.`
        : null;
    return {
      unit,
      currentHeadName: currentHead === null ? null : nameOf(currentHead),
      isCurrentHeadStaying: stayingHead !== null,
      options: keys.map((key) => ({ key, fullName: nameOf(key) })),
      chosen: explicit === undefined ? suggested : explicit,
      isUndecided:
        explicit === undefined && suggested === null && candidates.length > 1,
      mismatch,
      needsAttention: candidates.length > 1 || mismatch !== null,
    };
  });
}

/** The choices for the units a new plan still lists. */
export function choicesStillListed(
  plan: OrgImportPlan,
  choices: HeadChoices,
): HeadChoices {
  const listed = new Set(plan.heads.units.map(({ key }) => key));
  return new Map([...choices].filter(([unitKey]) => listed.has(unitKey)));
}

/** One explicit head for every unit of the head step, so the server's defaults never decide anything. */
export function headDecisionsOf(
  plan: OrgImportPlan,
  kept: KeptIds,
  choices: HeadChoices,
): ImportHeadDecision[] {
  return headRows(plan, kept, choices).map(({ unit, chosen }) => ({
    unit: unit.key,
    employee: chosen,
  }));
}
