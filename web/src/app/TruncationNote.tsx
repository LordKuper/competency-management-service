import { Typography } from "antd";

interface TruncationNoteProps {
  /** How many records the list shows. */
  shown: number;
  /** How many records match in all. */
  total: number | undefined;
}

/** The line under a list read in one request: nothing when all records are shown, else how many are and that the search narrows it. */
export function TruncationNote({ shown, total }: TruncationNoteProps) {
  if (total === undefined || total <= shown) return null;
  return (
    <Typography.Text type="secondary">
      {`Показаны первые ${shown} из ${total} — уточните поиск.`}
    </Typography.Text>
  );
}
