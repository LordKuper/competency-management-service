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
import type { MatrixBlock, MatrixContent } from "./matrixApi";
import {
  addBlock,
  type ChangedPlaces,
  cellKey,
  hasCompetencies,
  removeBlock,
  setBlockWeight,
  updateBlock,
} from "./matrixContent";
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

interface MatrixBlocksProps {
  content: MatrixContent;
  /** The levels of the content in track order. */
  levelIds: readonly string[];
  levelNames: ReadonlyMap<string, string>;
  changed: ChangedPlaces;
  isEditable: boolean;
  /** Inside a wizard step, where the step's name already stands in the ruler and the table needs no heading. */
  isCompact?: boolean;
  onChange: (content: MatrixContent) => void;
}

/**
 * The blocks of competencies with their coefficients and thresholds per level. A control is disabled here only when it
 * must be, and otherwise follows the disabled state around it, so the editor can lock them all while it saves.
 */
export function MatrixBlocks({
  content,
  levelIds,
  levelNames,
  changed,
  isEditable,
  isCompact = false,
  onChange,
}: MatrixBlocksProps) {
  const levelColumns: TableColumnsType<MatrixBlock> = levelIds.map(
    (levelId) => {
      const levelName = levelNames.get(levelId) ?? "";
      const weightOf = (block: MatrixBlock) =>
        content.blockWeights.find(
          (weight) => weight.levelId === levelId && weight.blockId === block.id,
        );
      const isChanged = (block: MatrixBlock) =>
        changed.weightCells.has(cellKey(levelId, block.id));
      return {
        key: levelId,
        title: levelName,
        children: [
          {
            key: `${levelId}:percent`,
            title: "Коэффициент, %",
            onCell: (block) => ({
              className: isChanged(block) ? "matrix-changed" : undefined,
            }),
            render: (_, block) => (
              <PercentField
                label={`Коэффициент, %: ${levelName}, ${block.name}`}
                value={weightOf(block)?.percent ?? null}
                isEditable={isEditable}
                isChanged={isChanged(block)}
                onChange={(value) =>
                  onChange(
                    setBlockWeight(
                      content,
                      levelId,
                      block.id,
                      "percent",
                      value,
                    ),
                  )
                }
              />
            ),
          },
          {
            key: `${levelId}:min`,
            title: "Минимальный порог блока",
            onCell: (block) => ({
              className: isChanged(block) ? "matrix-changed" : undefined,
            }),
            render: (_, block) => (
              <PercentField
                label={`Минимальный порог блока: ${levelName}, ${block.name}`}
                value={weightOf(block)?.minPercent ?? null}
                isEditable={isEditable}
                isChanged={false}
                onChange={(value) =>
                  onChange(
                    setBlockWeight(
                      content,
                      levelId,
                      block.id,
                      "minPercent",
                      value,
                    ),
                  )
                }
              />
            ),
          },
        ],
      };
    },
  );
  const columns: TableColumnsType<MatrixBlock> = [
    {
      title: "Название блока",
      key: "name",
      render: (_, block) => (
        <Space orientation="vertical" size="small">
          {isEditable ? (
            <Input
              aria-label="Название блока"
              autoComplete="off"
              value={block.name}
              onChange={(event) =>
                onChange(
                  updateBlock(content, block.id, { name: event.target.value }),
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
      ),
    },
    {
      title: "Обязательный блок",
      key: "isMandatory",
      render: (_, block) => (
        <Checkbox
          disabled={!isEditable || undefined}
          checked={block.isMandatory}
          onChange={(event) =>
            onChange(
              updateBlock(content, block.id, {
                isMandatory: event.target.checked,
              }),
            )
          }
        >
          Обязательный блок
        </Checkbox>
      ),
    },
    ...levelColumns,
  ];
  if (isEditable) {
    columns.push({
      title: "Действия",
      key: "actions",
      render: (_, block) => {
        const isUsed = hasCompetencies(content, block.id);
        return (
          <Space size={0} wrap>
            <Button
              type="link"
              size="small"
              danger
              disabled={isUsed || undefined}
              onClick={() => onChange(removeBlock(content, block.id))}
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
        <Table<MatrixBlock>
          className="matrix-grid"
          rowKey="id"
          columns={columns}
          dataSource={[...content.blocks]}
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
