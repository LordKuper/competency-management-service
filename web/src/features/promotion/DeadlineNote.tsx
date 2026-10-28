import { Flex, Typography } from "antd";
import type { SessionDates } from "./promotionApi";
import { formatDate } from "./promotionTexts";

interface DeadlineNoteProps {
  /** The session whose dates are told. */
  session: SessionDates;
  /** What the deadline is for, such as «Оценки можно менять до». */
  label: string;
  /** The stage whose end is the deadline, counted from 1. */
  stage: number;
}

/** The line above a screen that names its session and the day the screen stops taking changes. */
export function DeadlineNote({ session, label, stage }: DeadlineNoteProps) {
  const deadline = session.endsOn[stage - 1];
  return (
    <Flex justify="space-between" align="baseline" gap="middle" wrap>
      <Typography.Text type="secondary">
        Карьерная сессия с {formatDate(session.startsOn)}
      </Typography.Text>
      {deadline && (
        <Typography.Text type="secondary">
          {label}{" "}
          <Typography.Text strong>{formatDate(deadline)}</Typography.Text>
        </Typography.Text>
      )}
    </Flex>
  );
}
