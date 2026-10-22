import { Table, type TableColumnsType } from "antd";
import type { PublicationEntry } from "../career/catalogApi";
import {
  hiddenDraftsText,
  KIND_LABELS,
  type PublishedDrafts,
} from "../career/usePublishEntry";

const COLUMNS: TableColumnsType<PublicationEntry> = [
  {
    title: "Вид",
    dataIndex: "kind",
    render: (kind: PublicationEntry["kind"]) => KIND_LABELS[kind],
  },
  {
    title: "Название",
    dataIndex: "name",
    render: (name: string, entry) =>
      entry.context ? `${name} (${entry.context})` : name,
  },
  { title: "Статус", key: "status", render: () => "Черновик" },
];

interface CascadeTableProps {
  /** The draft records that become active together with the action. */
  drafts: PublishedDrafts;
}

/**
 * The records an action publishes with its object, one row each, always with the word for their state; the ones the
 * user may not read only by number under the table.
 */
export function CascadeTable({ drafts }: CascadeTableProps) {
  return (
    <>
      {drafts.entries.length > 0 && (
        <Table<PublicationEntry>
          size="small"
          rowKey={(entry) => `${entry.kind}:${entry.id}`}
          columns={COLUMNS}
          dataSource={[...drafts.entries]}
          pagination={false}
          className="desk-table"
        />
      )}
      {drafts.hiddenEntryCount > 0 && (
        <p>{hiddenDraftsText(drafts.hiddenEntryCount)}</p>
      )}
    </>
  );
}
