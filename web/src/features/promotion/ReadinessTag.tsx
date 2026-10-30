import { Tag } from "antd";
import { NO_VALUE } from "./promotionTexts";

/** Whether the rating of a candidate makes them ready for the level; nothing is claimed before the chair has scored them. */
export function ReadinessTag({ isReady }: { isReady: boolean | null }) {
  if (isReady === null) return NO_VALUE;
  return isReady ? (
    <Tag color="success">Готов</Tag>
  ) : (
    <Tag color="warning">Не готов</Tag>
  );
}
