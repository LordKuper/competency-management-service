import { type TableColumnsType, Typography } from "antd";
import type { ColumnType } from "antd/es/table";
import type { ReactNode } from "react";

/** A block of competencies with its place among the blocks. */
export interface BlockInfo {
  name: string;
  order: number;
}

/** A row of a table whose competencies are set under the heading of their block: the heading or one competency. */
export type BlockRow<TItem> =
  | { key: string; group: string; item?: undefined }
  | { key: string; group?: undefined; item: TItem };

const NO_BLOCK = "Прочие";

/** The competencies in the order of their blocks, each block opening with its heading row; without any block there are no headings. */
export function blockRows<TItem>(
  items: readonly TItem[],
  blockOf: (item: TItem) => BlockInfo | null,
  keyOf: (item: TItem) => string,
): BlockRow<TItem>[] {
  if (items.every((item) => blockOf(item) === null)) {
    return items.map((item) => ({ key: keyOf(item), item }));
  }
  const groups = new Map<string, { order: number; items: TItem[] }>();
  for (const item of items) {
    const block = blockOf(item);
    const name = block?.name ?? NO_BLOCK;
    const group = groups.get(name) ?? {
      order: block?.order ?? Number.MAX_SAFE_INTEGER,
      items: [],
    };
    group.items.push(item);
    groups.set(name, group);
  }
  return [...groups]
    .sort(([, left], [, right]) => left.order - right.order)
    .flatMap(([name, group]) => [
      { key: `block:${name}`, group: name },
      ...group.items.map((item) => ({ key: keyOf(item), item })),
    ]);
}

/** The columns of a table of competencies made to fit its rows with block headings: a heading spans the whole row. */
export function blockColumns<TItem extends object>(
  columns: TableColumnsType<TItem>,
): TableColumnsType<BlockRow<TItem>> {
  return (columns as ColumnType<TItem>[]).map((column, index) => ({
    ...column,
    dataIndex: undefined,
    onCell: (row: BlockRow<TItem>) =>
      row.group === undefined
        ? {}
        : { colSpan: index === 0 ? columns.length : 0 },
    render: (_: unknown, row: BlockRow<TItem>, rowIndex: number): ReactNode => {
      if (row.group !== undefined) {
        return index === 0 ? (
          <Typography.Text strong>{row.group}</Typography.Text>
        ) : null;
      }
      const value =
        typeof column.dataIndex === "string"
          ? (row.item as Record<string, unknown>)[column.dataIndex]
          : undefined;
      return column.render
        ? (column.render(value, row.item, rowIndex) as ReactNode)
        : (value as ReactNode);
    },
  })) as TableColumnsType<BlockRow<TItem>>;
}
