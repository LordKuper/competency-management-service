import { describe, expect, it } from "vitest";
import { effectiveHr, type OrgUnitHrPerson } from "./careerApi";

const person = (name: string, isActive?: boolean): OrgUnitHrPerson => ({
  userId: name,
  name,
  orgUnitId: "unit",
  orgUnitName: "Отдел",
  ...(isActive === undefined ? {} : { isActive }),
});

describe("the HR who answers for a unit (AC-8)", () => {
  it("is the one assigned to the unit, before the one inherited from above", () => {
    const hr = effectiveHr({
      assigned: person("Назначенный"),
      inherited: person("Унаследованный"),
    });

    expect(hr?.name).toBe("Назначенный");
  });

  it("is the inherited one when the unit has none of its own", () => {
    const hr = effectiveHr({
      assigned: null,
      inherited: person("Унаследованный"),
    });

    expect(hr?.name).toBe("Унаследованный");
  });

  it("is nobody when the assigned HR cannot act, the inherited one does not step in, as the server does not route to one either", () => {
    const hr = effectiveHr({
      assigned: person("Заблокированный", false),
      inherited: person("Унаследованный", true),
    });

    expect(hr).toBeNull();
  });

  it("takes an HR whose status the server did not send as one who can act, since it leaves out those who cannot", () => {
    expect(
      effectiveHr({ assigned: person("Обычный"), inherited: null })?.name,
    ).toBe("Обычный");
  });

  it("is nobody when there is no HR at all or the only one cannot act", () => {
    expect(effectiveHr({ assigned: null, inherited: null })).toBeNull();
    expect(
      effectiveHr({
        assigned: null,
        inherited: person("Заблокированный", false),
      }),
    ).toBeNull();
  });
});
