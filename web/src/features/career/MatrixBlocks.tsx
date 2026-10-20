import { EditOutlined } from "@ant-design/icons";
import {
  Button,
  Checkbox,
  Input,
  InputNumber,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { CatalogStatusTag } from "./CatalogParts";
import type {
  MatrixBlock,
  MatrixContent,
  MatrixSharedBlockEntry,
} from "./matrixApi";
import {
  addBlock,
  type ChangedPlaces,
  cellKey,
  hasCompetencies,
  removeBlock,
  setBlockWeight,
  setSharedBlockMandatory,
  updateBlock,
} from "./matrixContent";
import { OUTSIDE_LABEL } from "./matrixGroups";
import { CHANGED_NOTE } from "./matrixLabels";

const MAX_PERCENT = 100;
const NO_VALUE = "—";

interface PercentFieldProps {
  label: string;
  value: number | null;
  isEditable: boolean;
  isChanged: boolean;
  onChange: (value: number | null) => void;
}

function PercentField({
  label,
  value,
  isEditable,
  isChanged,
  onChange,
}: PercentFieldProps) {
  return (
    <Space size="small">
      {isEditable ? (
        <InputNumber
          aria-label={label}
          min={0}
          max={MAX_PERCENT}
          value={value}
          onChange={onChange}
        />
      ) : (
        (value ?? NO_VALUE)
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
}

/** A group of the table: an own block of the version, a shared block it includes, or the competencies outside blocks. */
type BlockRow =
  | { kind: "own"; key: string; blockId: string; block: MatrixBlock }
  | {
      kind: "shared";
      key: string;
      blockId: string;
      entry: MatrixSharedBlockEntry;
    }
  | { kind: "outside"; key: string; blockId: null };

interface MatrixBlocksProps {
  content: MatrixContent;
  /** The shared blocks the version includes, as their entries describe them. */
  sharedBlocks: readonly MatrixSharedBlockEntry[];
  /** The version has one column of requirements, so its coefficients and thresholds need no level heading. */
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

function rowName(row: BlockRow) {
  if (row.kind === "own") return row.block.name;
  if (row.kind === "shared") return row.entry.name;
  return OUTSIDE_LABEL;
}

/**
 * The groups of competencies with their coefficients and thresholds per level: own blocks of the version, shared blocks
 * (name and composition read-only, coefficient, mandatory flag and threshold set here) and, while some competency stands
 * outside every block, that group, which has a coefficient only. A control is disabled here only when it must be, and
 * otherwise follows the disabled state around it, so the editor can lock them all while it saves.
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
  const hasOutside = content.competencies.some((item) => item.blockId === null);
  const rows: BlockRow[] = [
    ...sharedBlocks.map((entry) => ({
      kind: "shared" as const,
      key: `shared:${entry.id}`,
      blockId: entry.id,
      entry,
    })),
    ...(content.blocks ?? []).map((block) => ({
      kind: "own" as const,
      key: `own:${block.id}`,
      blockId: block.id,
      block,
    })),
    ...(hasOutside
      ? [{ kind: "outside" as const, key: "outside", blockId: null }]
      : []),
  ];
  const levelColumns: TableColumnsType<BlockRow> = levelIds.flatMap(
    (levelId) => {
      const levelName = levelNames.get(levelId) ?? "";
      const weightOf = (row: BlockRow) =>
        content.blockWeights.find(
          (weight) =>
            weight.levelId === levelId && weight.blockId === row.blockId,
        );
      const isChanged = (row: BlockRow) =>
        row.blockId !== null &&
        changed.weightCells.has(cellKey(levelId, row.blockId));
      const percentColumn: TableColumnsType<BlockRow>[number] = {
        key: `${levelId}:percent`,
        title: "Коэффициент, %",
        onCell: (row) => ({
          className: isChanged(row) ? "matrix-changed" : undefined,
        }),
        render: (_, row) => (
          <PercentField
            label={`Коэффициент, %: ${levelName}, ${rowName(row)}`}
            value={weightOf(row)?.percent ?? null}
            isEditable={isEditable}
            isChanged={isChanged(row)}
            onChange={(value) =>
              onChange(
                setBlockWeight(content, levelId, row.blockId, "percent", value),
              )
            }
          />
        ),
      };
      const thresholdColumn: TableColumnsType<BlockRow>[number] = {
        key: `${levelId}:min`,
        title: "Минимальный порог блока",
        onCell: (row) => ({
          className: isChanged(row) ? "matrix-changed" : undefined,
        }),
        render: (_, row) =>
          row.kind === "outside" ? (
            NO_VALUE
          ) : (
            <PercentField
              label={`Минимальный порог блока: ${levelName}, ${rowName(row)}`}
              value={weightOf(row)?.minPercent ?? null}
              isEditable={isEditable}
              isChanged={false}
              onChange={(value) =>
                onChange(
                  setBlockWeight(
                    content,
                    levelId,
                    row.blockId,
                    "minPercent",
                    value,
                  ),
                )
              }
            />
          ),
      };
      return isSingleLevel
        ? [percentColumn, thresholdColumn]
        : [
            {
              key: levelId,
              title: levelName,
              children: [percentColumn, thresholdColumn],
            },
          ];
    },
  );
  const columns: TableColumnsType<BlockRow> = [
    {
      title: "Название блока",
      key: "name",
      render: (_, row) => {
        if (row.kind === "outside") return OUTSIDE_LABEL;
        if (row.kind === "shared") {
          return (
            <Space size="small" wrap>
              {row.entry.name}
              <Tag>Общий блок</Tag>
              {row.entry.status === "Draft" && (
                <CatalogStatusTag status="Draft" />
              )}
            </Space>
          );
        }
        const { block } = row;
        return (
          <Space orientation="vertical" size="small">
            {isEditable ? (
              <Input
                aria-label="Название блока"
                autoComplete="off"
                value={block.name}
                onChange={(event) =>
                  onChange(
                    updateBlock(content, block.id, {
                      name: event.target.value,
                    }),
                  )
                }
              />
            ) : (
              block.name
            )}
            {changed.blocks.has(block.id) && (
              <Tag icon={<EditOutlined />} color="warning">
                {CHANGED_NOTE}
              </Tag>
            )}
          </Space>
        );
      },
    },
    {
      title: "Обязательный блок",
      key: "isMandatory",
      render: (_, row) => {
        if (row.kind === "outside") return NO_VALUE;
        const isMandatory = row.kind === "own" ? row.block.isMandatory : false;
        return (
          <Checkbox
            disabled={!isEditable || undefined}
            checked={isMandatory}
            onChange={(event) =>
              onChange(
                row.kind === "own"
                  ? updateBlock(content, row.blockId, {
                      isMandatory: event.target.checked,
                    })
                  : setSharedBlockMandatory(
                      content,
                      row.blockId,
                      event.target.checked,
                    ),
              )
            }
          >
            Обязательный блок
          </Checkbox>
        );
      },
    },
    ...levelColumns,
  ];
  if (isEditable) {
    columns.push({
      title: "Действия",
      key: "actions",
      render: (_, row) => {
        if (row.kind === "shared") {
          return (
            <Typography.Text type="secondary">
              Убирается в мастере, шаг 2
            </Typography.Text>
          );
        }
        if (row.kind === "outside") {
          return (
            <Typography.Text type="secondary">
              Группа исчезает, когда в ней нет компетенций
            </Typography.Text>
          );
        }
        const isUsed = hasCompetencies(content, row.blockId);
        return (
          <Space size={0} wrap>
            <Button
              type="link"
              size="small"
              danger
              disabled={isUsed || undefined}
              onClick={() => onChange(removeBlock(content, row.blockId))}
            >
              Удалить
            </Button>
            {isUsed && (
              <Typography.Text type="secondary">
                Сначала уберите компетенции блока
              </Typography.Text>
            )}
          </Space>
        );
      },
    });
  }

  return (
    <section>
      {!isCompact && (
        <Typography.Title level={2}>Блоки компетенций</Typography.Title>
      )}
      <Space orientation="vertical" size="middle" style={{ display: "flex" }}>
        <Table<BlockRow>
          className="matrix-grid"
          rowKey="key"
          columns={columns}
          dataSource={rows}
          pagination={false}
          scroll={{ x: "max-content" }}
        />
        {isEditable && (
          <Button onClick={() => onChange(addBlock(content))}>
            Добавить блок
          </Button>
        )}
      </Space>
    </section>
  );
}
