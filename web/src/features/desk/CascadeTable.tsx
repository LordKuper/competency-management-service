import { Table, type TableColumnsType } from "antd";
import type { PublicationEntry } from "../career/catalogApi";
import { KIND_LABELS } from "../career/usePublishEntry";

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
  entries: readonly PublicationEntry[];
}

/** The records an action publishes with its object, one row each, always with the word for their state. */
export function CascadeTable({ entries }: CascadeTableProps) {
  return (
    <Table<PublicationEntry>
      size="small"
      rowKey={(entry) => `${entry.kind}:${entry.id}`}
      columns={COLUMNS}
      dataSource={[...entries]}
      pagination={false}
      className="desk-table"
    />
  );
}
