import { Empty, Table, type TableColumnsType, Tag } from "antd";
import { CHANGE_TYPES, describeChanges } from "../career/MatrixChanges";
import type { MatrixChange, MatrixDiff } from "../career/matrixApi";
import "./scenario.css";

const NO_VALUE = "—";

const TAG_COLORS: Record<MatrixChange["type"], string> = {
  Added: "success",
  Removed: "error",
  Changed: "warning",
};

interface ChangesBlockProps {
  diff: MatrixDiff;
  levelNames: ReadonlyMap<string, string>;
  competencyNames: ReadonlyMap<string, string>;
  blockNames: ReadonlyMap<string, string>;
  pointsOf: (
    competencyId: string,
  ) => readonly { score: number; label: string }[];
  /** Heads of the columns with the values before and after. */
  columns: { before: string; after: string };
  /** What the table says when nothing changed. */
  emptyText: string;
}

interface Row {
  key: string;
  change: MatrixChange;
  place: string;
  before: string | undefined;
  after: string | undefined;
}

/** The changes of a diff as a table: each row names its kind in words and is tinted, so colour is never the only sign. */
export function ChangesBlock({
  diff,
  levelNames,
  competencyNames,
  blockNames,
  pointsOf,
  columns: heads,
  emptyText,
}: ChangesBlockProps) {
  const describe = describeChanges(
    diff,
    levelNames,
    competencyNames,
    blockNames,
    pointsOf,
  );
  const rows: Row[] = diff.changes.map((change) => ({
    key: `${change.kind}:${change.levelId}:${change.blockId}:${change.competencyId}`,
    change,
    ...describe(change),
  }));
  const columns: TableColumnsType<Row> = [
    {
      title: "Что",
      key: "type",
      render: (_, { change }) => (
        <Tag color={TAG_COLORS[change.type]}>{CHANGE_TYPES[change.type]}</Tag>
      ),
    },
    { title: "Где", dataIndex: "place" },
    {
      title: heads.before,
      dataIndex: "before",
      render: (value?: string) => value ?? NO_VALUE,
    },
    {
      title: heads.after,
      dataIndex: "after",
      render: (value?: string) => value ?? NO_VALUE,
    },
  ];
  return (
    <Table<Row>
      size="small"
      rowKey="key"
      columns={columns}
      dataSource={rows}
      pagination={false}
      className="desk-table"
      rowClassName={({ change }) => `change-row-${change.type.toLowerCase()}`}
      locale={{
        emptyText: (
          <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={emptyText} />
        ),
      }}
    />
  );
}
