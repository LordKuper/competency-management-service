import type {
  ImportHeadUnit,
  MissingEmployee,
  MissingUnit,
  OrgImportPlan,
} from "../features/org-import/orgImportApi";

/** A plan with no changes, no missing units or employees and no head step; a test overrides what it is about. */
export function planOf(overrides: Partial<OrgImportPlan> = {}): OrgImportPlan {
  return {
    fingerprint: "fingerprint-1",
    newUnits: [],
    activatedUnits: [],
    newPositions: [],
    reactivatedPositions: [],
    newLegalEntities: [],
    newEmployees: [],
    rehiredEmployees: [],
    transfers: [],
    addedAssignments: [],
    removedAssignments: [],
    changedLegalEntities: [],
    unitPositionsAdded: [],
    unitPositionsRemoved: [],
    missingUnits: [],
    missingEmployees: [],
    heads: { units: [], employees: [] },
    ...overrides,
  };
}

export function missingUnit(
  id: string,
  parentId: string | null = null,
): MissingUnit {
  return { id, path: `Путь ${id}`, parentId };
}

export function missingEmployee(
  id: string,
  orgUnitIds: string[] = [],
  isLastActiveAdministrator = false,
): MissingEmployee {
  return {
    id,
    fullName: `Сотрудник ${id}`,
    orgUnitIds,
    isLastActiveAdministrator,
  };
}

export function headUnit(
  key: string,
  overrides: Partial<ImportHeadUnit> = {},
): ImportHeadUnit {
  return {
    key,
    path: `Отдел ${key}`,
    unitId: null,
    currentHead: null,
    currentHeadStays: false,
    candidates: [],
    employees: [],
    suggested: null,
    ...overrides,
  };
}
