import type {
  ImportDecision,
  MissingEmployee,
  OrgImportPlan,
  OrgImportResult,
} from "./orgImportApi";

/** The groups of changes a plan lists and an applied import counts, in the order the wizard shows them. */
export const CHANGE_GROUPS: ReadonlyArray<{
  key: keyof OrgImportPlan & keyof OrgImportResult;
  title: string;
}> = [
  { key: "newUnits", title: "Новые подразделения" },
  { key: "activatedUnits", title: "Активируемые подразделения" },
  { key: "newPositions", title: "Новые должности" },
  { key: "reactivatedPositions", title: "Возвращаемые в работу должности" },
  { key: "newLegalEntities", title: "Новые юридические лица" },
  { key: "newEmployees", title: "Новые сотрудники" },
  { key: "rehiredEmployees", title: "Возврат на работу" },
  { key: "transfers", title: "Переводы: смена основного назначения" },
  { key: "addedAssignments", title: "Добавленные совмещения" },
  { key: "removedAssignments", title: "Снятые совмещения" },
  {
    key: "changedLegalEntities",
    title: "Изменения юридических лиц назначений",
  },
  {
    key: "unitPositionsAdded",
    title: "Должности, добавляемые в перечни подразделений",
  },
  {
    key: "unitPositionsRemoved",
    title: "Должности, исключаемые из перечней подразделений",
  },
];

/** The groups only an applied import counts: what the decisions about the missing units and employees did. */
export const DECISION_GROUPS: ReadonlyArray<{
  key: keyof OrgImportResult;
  title: string;
}> = [
  { key: "deactivatedUnits", title: "Деактивированные подразделения" },
  { key: "dismissedEmployees", title: "Уволенные сотрудники" },
];

/** The ids of the missing units and employees the user decided to keep; every other one is deactivated or dismissed. */
export type KeptIds = ReadonlySet<string>;

/** A last active administrator cannot be dismissed, so that employee stays whatever was chosen. */
export function isEmployeeKept(
  employee: MissingEmployee,
  kept: KeptIds,
): boolean {
  return employee.isLastActiveAdministrator || kept.has(employee.id);
}

/** The ids of the kept entities that the new plan still lists as missing. */
export function keptStillMissing(plan: OrgImportPlan, kept: KeptIds): KeptIds {
  const missing = new Set([
    ...plan.missingUnits.map((unit) => unit.id),
    ...plan.missingEmployees.map((employee) => employee.id),
  ]);
  return new Set([...kept].filter((id) => missing.has(id)));
}

/** One explicit decision for every missing unit and employee, so the server's defaults never decide anything. */
export function decisionsOf(
  plan: OrgImportPlan,
  kept: KeptIds,
): ImportDecision[] {
  return [
    ...plan.missingUnits.map(({ id }) => ({
      id,
      decision: kept.has(id) ? ("keep" as const) : ("deactivate" as const),
    })),
    ...plan.missingEmployees.map((employee) => ({
      id: employee.id,
      decision: isEmployeeKept(employee, kept)
        ? ("keep" as const)
        : ("dismiss" as const),
    })),
  ];
}

function count(counts: Map<string, number>, id: string) {
  counts.set(id, (counts.get(id) ?? 0) + 1);
}

/**
 * The deactivated units whose decision the server would refuse, with the reason: a working employee who stays in the
 * unit, or an active child unit that stays. Mirrors the server's check, so the wizard shows it before the apply.
 */
export function unitBlocks(
  plan: OrgImportPlan,
  kept: KeptIds,
): ReadonlyMap<string, string> {
  const stayingEmployees = new Map<string, number>();
  for (const employee of plan.missingEmployees) {
    if (!isEmployeeKept(employee, kept)) continue;
    for (const unitId of new Set(employee.orgUnitIds)) {
      count(stayingEmployees, unitId);
    }
  }
  const stayingChildren = new Map<string, number>();
  for (const unit of plan.missingUnits) {
    if (kept.has(unit.id) && unit.parentId)
      count(stayingChildren, unit.parentId);
  }

  const blocks = new Map<string, string>();
  for (const unit of plan.missingUnits) {
    if (kept.has(unit.id)) continue;
    const reasons = [
      stayingEmployees.has(unit.id)
        ? `В подразделении остаются работающие сотрудники (${stayingEmployees.get(unit.id)}): оставьте подразделение или уволите сотрудников.`
        : null,
      stayingChildren.has(unit.id)
        ? `Остаются активные дочерние подразделения (${stayingChildren.get(unit.id)}): оставьте подразделение или деактивируйте дочерние.`
        : null,
    ].filter((reason) => reason !== null);
    if (reasons.length > 0) blocks.set(unit.id, reasons.join(" "));
  }
  return blocks;
}
