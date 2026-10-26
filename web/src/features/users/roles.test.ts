import { describe, expect, it } from "vitest";
import { formatRecordedRoles, formatRoles, PLAIN_USER_LABEL } from "./roles";

describe("the roles of an account in words (user-management AC-50)", () => {
  it("lists the roles by name, in the order the form offers them, and an account with none as a plain user", () => {
    expect(formatRoles(["Hr", "GlobalAdmin"])).toBe(
      "Глобальный администратор, HR",
    );
    expect(formatRoles(["Hr"])).toBe("HR");
    expect(formatRoles([])).toBe(PLAIN_USER_LABEL);
  });

  it("reads the roles of an audit record as the account list does", () => {
    expect(formatRecordedRoles("GlobalAdmin,Hr")).toBe(
      formatRoles(["GlobalAdmin", "Hr"]),
    );
    expect(formatRecordedRoles("Hr")).toBe("HR");
    expect(formatRecordedRoles("User")).toBe(PLAIN_USER_LABEL);
    expect(formatRecordedRoles("")).toBe(PLAIN_USER_LABEL);
  });

  it("leaves out a role name it does not know instead of printing it", () => {
    expect(formatRecordedRoles("Hr,Removed")).toBe("HR");
    expect(formatRecordedRoles("Removed")).toBe(PLAIN_USER_LABEL);
  });
});
