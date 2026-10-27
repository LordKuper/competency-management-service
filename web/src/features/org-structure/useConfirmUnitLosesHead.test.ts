import { describe, expect, it } from "vitest";
import type { OrgUnit } from "./orgStructureApi";
import { headedUnitsLeft } from "./useConfirmUnitLosesHead";

const unit = (id: string, headEmployeeId: string | null): OrgUnit => ({
  id,
  name: `Отдел ${id}`,
  parentId: null,
  headEmployeeId,
  isActive: true,
  version: 1,
});

describe("headedUnitsLeft (AC-3, AC-28, AC-29)", () => {
  const units = [
    unit("a", "e1"),
    unit("b", "e1"),
    unit("c", "e2"),
    unit("d", null),
  ];

  it("lists the units the employee heads and keeps no assignment in", () => {
    expect(headedUnitsLeft(units, "e1", ["b"]).map(({ id }) => id)).toEqual([
      "a",
    ]);
  });

  it("keeps a unit while any assignment, main or secondary, remains in it", () => {
    expect(headedUnitsLeft(units, "e1", ["a", "b"])).toEqual([]);
  });

  it("lists every headed unit when no assignment remains", () => {
    expect(headedUnitsLeft(units, "e1", []).map(({ id }) => id)).toEqual([
      "a",
      "b",
    ]);
  });

  it("ignores the units other people head and the units nobody heads", () => {
    expect(headedUnitsLeft(units, "e3", [])).toEqual([]);
  });

  it("lists nothing while the units are not loaded", () => {
    expect(headedUnitsLeft(undefined, "e1", [])).toEqual([]);
  });
});
