import dayjs, { type Dayjs } from "dayjs";
import { STAGE_COUNT } from "./promotionTexts";

/** The end date of every stage, each stage lasting its default number of days from the day after the previous one. */
export function endDatesFrom(
  start: Dayjs,
  durations: readonly number[],
): Dayjs[] {
  const ends: Dayjs[] = [];
  let previous = start.subtract(1, "day");
  for (const days of durations.slice(0, STAGE_COUNT)) {
    previous = previous.add(days, "day");
    ends.push(previous);
  }
  return ends;
}

/** The end dates of a wave sent back to a stage: the stages before it keep theirs, it and the later ones last their default days from today. */
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
