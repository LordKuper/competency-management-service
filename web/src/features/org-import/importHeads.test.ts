import { describe, expect, it } from "vitest";
import { headUnit, missingEmployee, planOf } from "../../test/orgImportPlan";
import { choicesStillListed, headDecisionsOf, headRows } from "./importHeads";

const NAMES = [
  { key: "ann", fullName: "Анна" },
  { key: "boris", fullName: "Борис" },
  { key: "e1", fullName: "Старый руководитель" },
];

describe("headRows", () => {
  it("takes the only candidate as the head of a unit with none now (AC-31)", () => {
    const plan = planOf({
      heads: {
        units: [
          headUnit("u", { candidates: ["ann"], employees: ["ann", "boris"] }),
        ],
        employees: NAMES,
      },
    });

    const [row] = headRows(plan, new Set(), new Map());

    expect(row?.chosen).toBe("ann");
    expect(row?.isUndecided).toBe(false);
    expect(row?.options.map((option) => option.fullName)).toEqual([
      "Анна",
      "Борис",
    ]);
  });

  it("leaves a unit with several candidates undecided and its head empty", () => {
    const plan = planOf({
      heads: {
        units: [
          headUnit("u", {
            candidates: ["ann", "boris"],
            employees: ["ann", "boris"],
          }),
        ],
        employees: NAMES,
      },
    });

    const [row] = headRows(plan, new Set(), new Map());

    expect(row?.chosen).toBeNull();
    expect(row?.isUndecided).toBe(true);
    expect(row?.needsAttention).toBe(true);
  });

  it("keeps the current head who stays over the only candidate", () => {
    const plan = planOf({
      heads: {
        units: [
          headUnit("u", {
            unitId: "unit-1",
            currentHead: "boris",
            currentHeadStays: true,
            candidates: ["ann"],
            employees: ["ann", "boris"],
          }),
        ],
        employees: NAMES,
      },
    });

    const [row] = headRows(plan, new Set(), new Map());

    expect(row?.chosen).toBe("boris");
    expect(row?.isCurrentHeadStaying).toBe(true);
    expect(row?.mismatch).toContain("Анна");
    expect(row?.needsAttention).toBe(true);
  });

  it("keeps a current head the file does not name only while the decisions keep them in the unit", () => {
    const plan = planOf({
      missingEmployees: [missingEmployee("e1", ["unit-1"])],
      heads: {
        units: [
          headUnit("u", {
            unitId: "unit-1",
            currentHead: "e1",
            currentHeadStays: false,
            candidates: ["ann"],
            employees: ["ann"],
          }),
        ],
        employees: NAMES,
      },
    });

    const [kept] = headRows(plan, new Set(["e1"]), new Map());
    const [dismissed] = headRows(plan, new Set(), new Map());

    expect(kept?.chosen).toBe("e1");
    expect(kept?.options.map((option) => option.key)).toContain("e1");
    expect(dismissed?.chosen).toBe("ann");
    expect(dismissed?.options.map((option) => option.key)).not.toContain("e1");
  });

  it("honours the choice of the user, null included, while it is still allowed", () => {
    const plan = planOf({
      heads: {
        units: [
          headUnit("u", {
            candidates: ["ann", "boris"],
            employees: ["ann", "boris"],
          }),
        ],
        employees: NAMES,
      },
    });

    expect(
      headRows(plan, new Set(), new Map([["u", "boris"]]))[0]?.chosen,
    ).toBe("boris");
    expect(
      headRows(plan, new Set(), new Map([["u", null]]))[0]?.chosen,
    ).toBeNull();
    expect(
      headRows(plan, new Set(), new Map([["u", "boris"]]))[0]?.isUndecided,
    ).toBe(false);
  });

  it("drops a choice that names an employee who no longer works in the unit", () => {
    const plan = planOf({
      heads: {
        units: [headUnit("u", { candidates: ["ann"], employees: ["ann"] })],
        employees: NAMES,
      },
    });

    expect(
      headRows(plan, new Set(), new Map([["u", "boris"]]))[0]?.chosen,
    ).toBe("ann");
  });

  it("names the current head by the missing employee's name when the file does not name them", () => {
    const plan = planOf({
      missingEmployees: [missingEmployee("e1", ["unit-1"])],
      heads: {
        units: [
          headUnit("u", { unitId: "unit-1", currentHead: "e1", employees: [] }),
        ],
        employees: [],
      },
    });

    expect(headRows(plan, new Set(), new Map())[0]?.currentHeadName).toBe(
      "Сотрудник e1",
    );
  });
});

describe("headDecisionsOf", () => {
  it("states a head for every unit of the head step (AC-31)", () => {
    const plan = planOf({
      heads: {
        units: [
          headUnit("one", { candidates: ["ann"], employees: ["ann"] }),
          headUnit("two", {
            candidates: ["ann", "boris"],
            employees: ["ann", "boris"],
          }),
        ],
        employees: NAMES,
      },
    });

    expect(headDecisionsOf(plan, new Set(), new Map())).toEqual([
      { unit: "one", employee: "ann" },
      { unit: "two", employee: null },
    ]);
  });
});

describe("choicesStillListed", () => {
  it("keeps the choices of the units the new plan still lists", () => {
    const plan = planOf({
      heads: { units: [headUnit("kept")], employees: [] },
    });

    const choices = choicesStillListed(
      plan,
      new Map([
        ["kept", "ann"],
        ["gone", null],
      ]),
    );

    expect([...choices]).toEqual([["kept", "ann"]]);
  });
});
