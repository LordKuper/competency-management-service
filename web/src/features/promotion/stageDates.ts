import dayjs, { type Dayjs } from "dayjs";
import { STAGE_COUNT } from "./promotionTexts";

/** Calendar days between the announcement of a wave and the first day it takes applications, at the least. */
const MIN_LEAD_DAYS = 7;

const SATURDAY = 6;
const SUNDAY = 0;

function isWorkingDay(day: Dayjs): boolean {
  return day.day() !== SATURDAY && day.day() !== SUNDAY;
}

/** The day itself when it is a working day, otherwise the next working day. */
function workingDayFrom(day: Dayjs): Dayjs {
  return isWorkingDay(day) ? day : workingDayFrom(day.add(1, "day"));
}

/** The first day applications may start for a wave announced on `announcedOn`, today by default. */
export function earliestStart(announcedOn?: string): Dayjs {
  return dayjs(announcedOn).add(MIN_LEAD_DAYS, "day");
}

/**
 * The end date of every stage in working days, Monday to Friday without holidays: the first stage starts on `start`,
 * or the next working day when that is a weekend, each next stage on the working day after the previous end, and a
 * stage of `n` days ends on its `n`-th working day counting the first.
 */
export function endDatesFrom(
  start: Dayjs,
  durations: readonly number[],
): Dayjs[] {
  const ends: Dayjs[] = [];
  let first = workingDayFrom(start);
  for (const days of durations.slice(0, STAGE_COUNT)) {
    let end = first;
    for (let counted = 1; counted < days; counted += 1) {
      end = workingDayFrom(end.add(1, "day"));
    }
    ends.push(end);
    first = workingDayFrom(end.add(1, "day"));
  }
  return ends;
}

/** The end dates of a wave sent back to a stage: the stages before it keep theirs, it and the later ones last their default working days from today. */
export function rollbackEndDates(
  stage: number,
  endsOn: readonly string[],
  durations: readonly number[],
): Dayjs[] {
  return [
    ...endsOn.slice(0, stage - 1).map((date) => dayjs(date)),
    ...endDatesFrom(dayjs(), durations.slice(stage - 1)),
  ];
}
