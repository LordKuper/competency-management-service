import { describe, expect, it } from "vitest";
import type { Employee, OrgUnit, OrgUnitTreeNode } from "./orgStructureApi";
import { ancestorIds, buildUnitTree, searchUnitTree } from "./orgTree";

function unit(
  id: string,
  name: string,
  parentId: string | null,
): OrgUnitTreeNode {
  return {
    id,
    name,
    parentId,
    headEmployeeId: null,
    isActive: true,
    version: 1,
    headName: null,
    employeeCount: 0,
  };
}

function employeeIn(orgUnitId: string): Employee {
  return {
    id: `employee-of-${orgUnitId}`,
    lastName: "Иванов",
    firstName: "Иван",
    middleName: null,
    fullName: "Иванов Иван",
    email: null,
    orgUnitId,
    orgUnitName: "",
    position: "Инженер",
  };
}

const company = unit("company", "Компания", null);
const sales = unit("sales", "Продажи", "company");
const north = unit("north", "Север", "sales");
const support = unit("support", "Поддержка", "company");
const orphan = unit("orphan", "Без родителя", "missing");

describe("buildUnitTree (AC-17)", () => {
  it("nests units by their parent links keeping the order of siblings, and makes a unit with an unknown parent a root", () => {
    const roots = buildUnitTree([company, support, sales, north, orphan]);

    expect(roots.map((root) => root.unit.id)).toEqual(["company", "orphan"]);
    expect(roots[0]?.children.map((child) => child.unit.id)).toEqual([
      "support",
      "sales",
    ]);
    expect(
      roots[0]?.children[1]?.children.map((child) => child.unit.id),
    ).toEqual(["north"]);
  });
});

describe("ancestorIds", () => {
  const units: OrgUnit[] = [company, sales, north];

  it("lists the units above one, nearest first, and none for a root or an unknown unit", () => {
    expect(ancestorIds(units, "north")).toEqual(["sales", "company"]);
    expect(ancestorIds(units, "company")).toEqual([]);
    expect(ancestorIds(units, "nowhere")).toEqual([]);
  });

  it("stops at a repeated unit instead of looping when the links form a cycle", () => {
    const cyclic: OrgUnit[] = [unit("a", "A", "b"), unit("b", "B", "a")];

    expect(ancestorIds(cyclic, "a")).toEqual(["b", "a"]);
  });
});

describe("searchUnitTree (AC-17)", () => {
  const roots = buildUnitTree([company, sales, north, support]);

  it("shows a matching unit with its path and everything below it, ignoring letter case and surrounding spaces", () => {
    const search = searchUnitTree(roots, "  ПРОДАЖИ ", []);

    expect(search.needle).toBe("продажи");
    expect([...search.visibleIds].sort()).toEqual([
      "company",
      "north",
      "sales",
    ]);
    expect([...search.pathIds]).toEqual(["company"]);
  });

  it("opens the path down to a unit holding matched employees and lists them by unit", () => {
    const matched = employeeIn("north");

    const search = searchUnitTree(roots, "иванов", [matched]);

    expect([...search.visibleIds].sort()).toEqual([
      "company",
      "north",
      "sales",
    ]);
    expect([...search.pathIds].sort()).toEqual(["company", "north", "sales"]);
    expect(search.employeesByUnit.get("north")).toEqual([matched]);
    expect(search.visibleIds.has("support")).toBe(false);
  });

  it("shows nothing when neither a unit nor an employee matches", () => {
    expect(searchUnitTree(roots, "ничего", []).visibleIds.size).toBe(0);
  });
});
