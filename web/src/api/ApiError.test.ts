import { describe, expect, it } from "vitest";
import { ApiError } from "./ApiError";

describe("ApiError (AC-25)", () => {
  it("reads the blocking records and the count of the others from a refusal", () => {
    const blockers = [
      { kind: "Track", id: "t", name: "Бэкенд", context: null },
    ];

    const error = new ApiError(409, {
      title: "Операция отклонена",
      detail: "Трек используют матрицы — удалить нельзя",
      blockers,
      moreCount: 4,
    });

    expect(error.status).toBe(409);
    expect(error.detail).toBe("Трек используют матрицы — удалить нельзя");
    expect(error.blockers).toEqual(blockers);
    expect(error.moreCount).toBe(4);
  });

  it("has no blockers and a zero count when the body carries none, and tolerates a body that is not an object", () => {
    expect(new ApiError(409, { detail: "Занято" })).toMatchObject({
      blockers: [],
      moreCount: 0,
    });
    expect(new ApiError(500, "boom")).toMatchObject({
      blockers: [],
      moreCount: 0,
      status: 500,
    });
    expect(new ApiError(409, { blockers: "x", moreCount: "3" })).toMatchObject({
      blockers: [],
      moreCount: 0,
    });
  });
});
