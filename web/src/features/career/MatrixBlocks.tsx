import { CloseCircleOutlined, EditOutlined } from "@ant-design/icons";
import {
  InputNumber,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { CatalogStatusTag } from "./CatalogParts";
import type { MatrixContent, MatrixSharedBlockEntry } from "./matrixApi";
import {
  type ChangedPlaces,
  cellKey,
  setBlockWeight,
  setOverallMin,
} from "./matrixContent";
import { competenciesText, OUTSIDE_LABEL } from "./matrixGroups";
import { CHANGED_NOTE } from "./matrixLabels";

const MAX_PERCENT = 100;
const PERCENT_PRECISION = 100;
const NO_VALUE = "—";

/** A row of the table: a shared block, the competencies outside blocks, the sum of a level or its overall threshold. */
type Row =
  | {
      kind: "shared";
      key: string;
      blockId: string;
      entry: MatrixSharedBlockEntry;
    }
  | { kind: "outside"; key: string; blockId: null; count: number }
  | { kind: "sum"; key: string }
  | { kind: "min"; key: string };

interface MatrixBlocksProps {
  content: MatrixContent;
  /** The shared blocks the version includes, as their entries describe them. */
  sharedBlocks: readonly MatrixSharedBlockEntry[];
  /** The version has one column of requirements, so its coefficients need no level heading. */
  isSingleLevel?: boolean;
  /** The levels of the content in track order. */
  levelIds: readonly string[];
  levelNames: ReadonlyMap<string, string>;
  changed: ChangedPlaces;
  isEditable: boolean;
  /** Inside a wizard step, where the step's name already stands in the ruler and the table needs no heading. */
  isCompact?: boolean;
  onChange: (content: MatrixContent) => void;
}

function coefficientSum(content: MatrixContent, levelId: string) {
  const sum = content.blockWeights
    .filter((weight) => weight.levelId === levelId)
    .reduce((total, weight) => total + weight.percent, 0);
  return Math.round(sum * PERCENT_PRECISION) / PERCENT_PRECISION;
}

/**
 * The coefficients of the groups of a version per level — its shared blocks and the competencies outside blocks —
 * with the check that each level sums to 100 and the overall threshold of each level under them. Names are read-only:
 * blocks are assigned on the composition step. A control follows the disabled state around it, so the editor can
 * lock them all while it saves.
 */
export function MatrixBlocks({
  content,
  sharedBlocks,
  isSingleLevel = false,
  levelIds,
  levelNames,
  changed,
  isEditable,
  isCompact = false,
  onChange,
}: MatrixBlocksProps) {
  const outsideCount = content.competencies.filter(
    (item) => item.blockId === null,
  ).length;
  const rows: Row[] = [
    ...sharedBlocks.map((entry) => ({
      kind: "shared" as const,
      key: `shared:${entry.id}`,
      blockId: entry.id,
      entry,
    })),
    ...(outsideCount > 0
      ? [
          {
            kind: "outside" as const,
            key: "outside",
            blockId: null,
            count: outsideCount,
          },
        ]
      : []),
    { kind: "sum", key: "sum" },
    { kind: "min", key: "min" },
  ];
  const levelColumns: TableColumnsType<Row> = levelIds.map((levelId) => {
    const levelName = levelNames.get(levelId) ?? "";
    const sum = coefficientSum(content, levelId);
    return {
      key: levelId,
      title: isSingleLevel ? "Коэф., %" : `${levelName}: коэф., %`,
      render: (_, row) => {
        if (row.kind === "sum") {
          return sum === MAX_PERCENT ? (
            sum
          ) : (
            <Typography.Text type="danger">
              <CloseCircleOutlined /> {`${sum} — нужно ${MAX_PERCENT}`}
            </Typography.Text>
          );
        }
        if (row.kind === "min") {
          const level = content.levels.find((item) => item.levelId === levelId);
          return isEditable ? (
            <InputNumber
              aria-label={`Минимальный общий порог, %: ${levelName}`}
              min={0}
              max={MAX_PERCENT}
              value={level?.minOverallPercent ?? null}
              onChange={(value) =>
                onChange(setOverallMin(content, levelId, value))
              }
            />
          ) : (
            (level?.minOverallPercent ?? NO_VALUE)
          );
        }
        const percent =
          content.blockWeights.find(
            (weight) =>
              weight.levelId === levelId && weight.blockId === row.blockId,
          )?.percent ?? null;
        const name = row.kind === "shared" ? row.entry.name : OUTSIDE_LABEL;
        const isChanged =
          row.blockId !== null &&
          changed.weightCells.has(cellKey(levelId, row.blockId));
        return (
          <Space size="small">
            {isEditable ? (
              <InputNumber
                aria-label={`Коэффициент, %: ${levelName}, ${name}`}
                min={0}
                max={MAX_PERCENT}
                value={percent}
                onChange={(value) =>
                  onChange(setBlockWeight(content, levelId, row.blockId, value))
                }
              />
            ) : (
              (percent ?? NO_VALUE)
            )}
            {isChanged && (
              <EditOutlined
                role="img"
                aria-label={CHANGED_NOTE}
                title={CHANGED_NOTE}
              />
            )}
          </Space>
        );
      },
    };
  });
  const columns: TableColumnsType<Row> = [
    {
      title: "Блок",
      key: "name",
      render: (_, row) => {
        if (row.kind === "sum") return <strong>Сумма коэффициентов</strong>;
        if (row.kind === "min") {
          return <strong>Минимальный общий порог, %</strong>;
        }
        if (row.kind === "outside") {
          return (
            <Space size="small" wrap>
              {OUTSIDE_LABEL}
              <Typography.Text type="secondary">
                {competenciesText(row.count)}
              </Typography.Text>
            </Space>
          );
        }
        return (
          <Space size="small" wrap>
            {row.entry.name}
            <Tag>Общий блок</Tag>
            {row.entry.status === "Draft" && (
              <CatalogStatusTag status="Draft" />
            )}
            <Typography.Text type="secondary">
              {competenciesText(row.entry.competencyIds.length)}
            </Typography.Text>
          </Space>
        );
      },
    },
    ...levelColumns,
  ];

  return (
    <section>
      {!isCompact && (
        <Typography.Title level={2}>Коэффициенты блоков</Typography.Title>
      )}
      <Table<Row>
        className="matrix-grid"
        rowKey="key"
        columns={columns}
        dataSource={rows}
        pagination={false}
        scroll={{ x: "max-content" }}
      />
    </section>
  );
}
