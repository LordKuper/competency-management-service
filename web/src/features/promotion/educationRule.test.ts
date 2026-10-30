import { describe, expect, it } from "vitest";
import { isEducationBelow } from "./educationRule";

describe("stated education against the required one (AC-10, AC-15)", () => {
  it("is below when the stated level comes earlier in the order of levels", () => {
    expect(isEducationBelow("SecondaryGeneral", "Bachelor")).toBe(true);
    expect(isEducationBelow("Bachelor", "SpecialistOrMaster")).toBe(true);
  });

  it("is not below at the same or a higher level", () => {
    expect(isEducationBelow("Bachelor", "Bachelor")).toBe(false);
    expect(isEducationBelow("HigherQualification", "Bachelor")).toBe(false);
  });

  it("is never below without a requirement or without a statement", () => {
    expect(isEducationBelow("SecondaryGeneral", null)).toBe(false);
    expect(isEducationBelow(undefined, "Bachelor")).toBe(false);
    expect(isEducationBelow(null, undefined)).toBe(false);
  });
});
