import { describe, expect, it } from "vitest";
import { ApiError } from "../../api/ApiError";
import {
  describeEmailFailure,
  isSameEmail,
  normalizedEmail,
} from "./raterEmail";

describe("the address of a rater without an account (AC-62)", () => {
  it("trims the address and treats an empty one as none", () => {
    expect(normalizedEmail("  a@b.ru ")).toBe("a@b.ru");
    expect(normalizedEmail("   ")).toBeNull();
    expect(normalizedEmail(undefined)).toBeNull();
  });

  it("does not tell addresses apart by the case of letters or the blanks around them", () => {
    expect(isSameEmail("A@B.ru", " a@b.RU")).toBe(true);
    expect(isSameEmail("a@b.ru", "c@b.ru")).toBe(false);
    expect(isSameEmail("", null)).toBe(true);
  });

  it("gives the reason the server states for the address itself, otherwise the usual description", () => {
    const refused = new ApiError(400, {
      errors: { email: ["Адрес уже занят", "Укажите другой"] },
    });

    expect(describeEmailFailure(refused)).toBe(
      "Адрес уже занят Укажите другой",
    );
    expect(describeEmailFailure(new ApiError(403, {}))).not.toBe("");
    expect(describeEmailFailure(new Error("offline"))).not.toBe("");
  });
});
