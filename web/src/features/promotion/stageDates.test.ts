import dayjs from "dayjs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { earliestStart, endDatesFrom, rollbackEndDates } from "./stageDates";

const day = (value: dayjs.Dayjs) => value.format("YYYY-MM-DD");

describe("the dates offered for the stages of a wave (AC-52, AC-56)", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-11-09T10:00:00"));
  });
  afterEach(() => vi.useRealTimers());

  it("puts the earliest start a week after the announcement, today by default", () => {
    expect(day(earliestStart())).toBe("2026-11-16");
    expect(day(earliestStart("2026-10-30"))).toBe("2026-11-06");
  });

  it("counts the stages in working days and moves a weekend start to the Monday", () => {
    const ends = endDatesFrom(dayjs("2026-11-07"), [3, 2, 1, 1, 1, 1]);

    expect(ends.map(day)).toEqual([
      "2026-11-11",
      "2026-11-13",
      "2026-11-16",
      "2026-11-17",
      "2026-11-18",
      "2026-11-19",
    ]);
  });

  it("lets a stage of one day end on the day it starts, and skips the weekend between stages", () => {
    const ends = endDatesFrom(dayjs("2026-11-13"), [1, 1]);

    expect(ends.map(day)).toEqual(["2026-11-13", "2026-11-16"]);
  });

  it("offers no more stages than a wave has", () => {
    expect(
      endDatesFrom(dayjs("2026-11-09"), [1, 1, 1, 1, 1, 1, 1, 1]),
    ).toHaveLength(6);
  });

  it("keeps the ends of the stages before the one a wave goes back to, and renews the rest from today", () => {
    const kept = ["2026-11-02", "2026-11-04"];

    const ends = rollbackEndDates(
      3,
      [...kept, "2026-11-06", "2026-11-09", "2026-11-12", "2026-11-16"],
      [10, 2, 5, 2, 5, 5],
    );

    expect(ends.map(day)).toEqual([
      ...kept,
      "2026-11-13",
      "2026-11-17",
      "2026-11-24",
      "2026-12-01",
    ]);
  });
});
