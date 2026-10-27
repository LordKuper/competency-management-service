import { describe, expect, it } from "vitest";
import { missingEmployee, missingUnit, planOf } from "../../test/orgImportPlan";
import {
  decisionsOf,
  isEmployeeKept,
  keptStillMissing,
  unitBlocks,
} from "./importPlan";

describe("isEmployeeKept", () => {
  it("keeps an employee the user kept", () => {
    expect(isEmployeeKept(missingEmployee("e1"), new Set(["e1"]))).toBe(true);
  });

  it("dismisses an employee the user did not keep", () => {
    expect(isEmployeeKept(missingEmployee("e1"), new Set())).toBe(false);
  });

  it("keeps the last active administrator whatever was chosen (AC-17)", () => {
    expect(isEmployeeKept(missingEmployee("e1", [], true), new Set())).toBe(
      true,
    );
  });
});

describe("keptStillMissing", () => {
  it("drops the kept ids the new plan no longer lists", () => {
    const plan = planOf({
      missingUnits: [missingUnit("u1")],
      missingEmployees: [missingEmployee("e1")],
    });

    expect(keptStillMissing(plan, new Set(["u1", "e1", "gone"]))).toEqual(
      new Set(["u1", "e1"]),
    );
  });

  it("keeps nothing when the new plan lists nothing missing", () => {
    expect(keptStillMissing(planOf(), new Set(["u1"]))).toEqual(new Set());
  });
});

describe("decisionsOf", () => {
  it("states one decision for every missing unit and employee (AC-18)", () => {
    const plan = planOf({
      missingUnits: [missingUnit("u1"), missingUnit("u2")],
      missingEmployees: [
        missingEmployee("e1"),
        missingEmployee("e2"),
        missingEmployee("e3", [], true),
      ],
    });

    expect(decisionsOf(plan, new Set(["u2", "e2"]))).toEqual([
      { id: "u1", decision: "deactivate" },
      { id: "u2", decision: "keep" },
      { id: "e1", decision: "dismiss" },
      { id: "e2", decision: "keep" },
      { id: "e3", decision: "keep" },
    ]);
  });

  it("states nothing for a plan with nothing missing", () => {
    expect(decisionsOf(planOf(), new Set())).toEqual([]);
  });
});

describe("unitBlocks", () => {
  it("blocks deactivating a unit whose employee stays (AC-18)", () => {
    const plan = planOf({
      missingUnits: [missingUnit("u1")],
      missingEmployees: [missingEmployee("e1", ["u1"])],
    });

    const blocks = unitBlocks(plan, new Set(["e1"]));

    expect(blocks.get("u1")).toContain("работающие сотрудники (1)");
  });

  it("counts an employee once however many assignments they keep in the unit", () => {
    const plan = planOf({
      missingUnits: [missingUnit("u1")],
      missingEmployees: [missingEmployee("e1", ["u1", "u1"])],
    });

    expect(unitBlocks(plan, new Set(["e1"])).get("u1")).toContain("(1)");
  });

  it("does not block when the employee of the unit is dismissed", () => {
    const plan = planOf({
      missingUnits: [missingUnit("u1")],
      missingEmployees: [missingEmployee("e1", ["u1"])],
    });

    expect(unitBlocks(plan, new Set()).size).toBe(0);
  });

  it("blocks for the last active administrator, who stays whatever was chosen", () => {
    const plan = planOf({
      missingUnits: [missingUnit("u1")],
      missingEmployees: [missingEmployee("e1", ["u1"], true)],
    });

    expect(unitBlocks(plan, new Set()).has("u1")).toBe(true);
  });

  it("blocks deactivating a unit whose child stays active", () => {
    const plan = planOf({
      missingUnits: [missingUnit("parent"), missingUnit("child", "parent")],
    });

    const blocks = unitBlocks(plan, new Set(["child"]));

    expect(blocks.get("parent")).toContain("дочерние подразделения (1)");
    expect(blocks.has("child")).toBe(false);
  });

  it("does not block a unit that is kept, nor a unit whose child goes too", () => {
    const plan = planOf({
      missingUnits: [missingUnit("parent"), missingUnit("child", "parent")],
      missingEmployees: [missingEmployee("e1", ["parent"])],
    });

    expect(unitBlocks(plan, new Set(["parent", "e1"])).has("parent")).toBe(
      false,
    );
    expect(
      unitBlocks(planOf({ missingUnits: plan.missingUnits }), new Set()).size,
    ).toBe(0);
  });

  it("gives both reasons when an employee and a child stay", () => {
    const plan = planOf({
      missingUnits: [missingUnit("parent"), missingUnit("child", "parent")],
      missingEmployees: [missingEmployee("e1", ["parent"])],
    });

    const reason = unitBlocks(plan, new Set(["child", "e1"])).get("parent");

    expect(reason).toContain("работающие сотрудники");
    expect(reason).toContain("дочерние подразделения");
  });
});
