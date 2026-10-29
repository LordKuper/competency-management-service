import { CloseCircleOutlined, EditOutlined } from "@ant-design/icons";
import {
  Button,
  Checkbox,
  Collapse,
  Flex,
  InputNumber,
  Select,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useEffect, useRef } from "react";
import { CatalogStatusTag } from "./CatalogParts";
import type { MatrixContent, MatrixSharedBlockEntry } from "./matrixApi";
import {
  areAllCritical,
  type ChangedPlaces,
  cellKey,
  competencyWeightsOf,
  distributeBlockWeights,
  hasOwnWeights,
  ownWeightSum,
  setBlockWeight,
  setCompetencyWeight,
  setOverallMin,
  setOwnWeights,
  setRequiredEducation,
  withEvenBlockWeights,
  withoutOverallMins,
} from "./matrixContent";
import { competenciesText, OUTSIDE_LABEL } from "./matrixGroups";
import {
  CHANGED_NOTE,
  EDUCATION_LABELS,
  type EducationLevel,
} from "./matrixLabels";

const MAX_PERCENT = 100;
const PERCENT_PRECISION = 100;
const NO_VALUE = "—";
const CRITICAL_ONLY_NOTE = "Не задаётся: все компетенции критические";
const EDUCATION_OPTIONS = (
  Object.keys(EDUCATION_LABELS) as EducationLevel[]
).map((value) => ({ value, label: EDUCATION_LABELS[value] }));

/**
 * A row of the table: a shared block, the weight of one of its competencies or the sum of those weights when it has its own,
 * the competencies outside blocks, the sum of a level, its overall threshold or its required education.
 */
type Row =
  | {
      kind: "shared";
      key: string;
      blockId: string;
      entry: MatrixSharedBlockEntry;
    }
  | {
      kind: "weight";
      key: string;
      blockId: string;
      competencyId: string;
      memberIds: readonly string[];
    }
  | {
      kind: "weightSum";
      key: string;
      blockId: string;
      memberIds: readonly string[];
    }
  | { kind: "outside"; key: string; blockId: null; count: number }
  | { kind: "sum"; key: string }
  | { kind: "min"; key: string }
  | { kind: "education"; key: string };

interface MatrixBlocksProps {
  content: MatrixContent;
  /** The shared blocks the version includes, as their entries describe them. */
  sharedBlocks: readonly MatrixSharedBlockEntry[];
  /** The version has one column of requirements, so its coefficients need no level heading. */
  isSingleLevel?: boolean;
  /** The levels of the content in track order. */
  levelIds: readonly string[];
  levelNames: ReadonlyMap<string, string>;
  /** The names of the competencies of the version, for the rows of own weights. */
  competencyNames: ReadonlyMap<string, string>;
  changed: ChangedPlaces;
  isEditable: boolean;
  /** Inside a wizard step, where the step's name already stands in the ruler and the table needs no heading. */
  isCompact?: boolean;
  onChange: (content: MatrixContent) => void;
}

const FORMULA_KEY = "formula";

/** What the table's numbers mean: the share of each block in the rating, how the rating is counted and when a level is reached. */
function FormulaExplanation({
  isSingleLevel,
  isOpen,
}: {
  isSingleLevel: boolean;
  isOpen: boolean;
}) {
  return (
    <Flex vertical gap="small">
      <Typography.Text type="secondary">
        {`Коэффициент блока — доля блока в рейтинге кандидата, сумма коэффициентов ${isSingleLevel ? "" : "каждого уровня "}— ${MAX_PERCENT} %. «${OUTSIDE_LABEL}» — компетенции, не входящие ни в один блок.`}
      </Typography.Text>
      <Collapse
        size="small"
        defaultActiveKey={isOpen ? [FORMULA_KEY] : []}
        items={[
          {
            key: FORMULA_KEY,
            label: "Как считается рейтинг и готовность к уровню",
            children: (
              <Flex vertical gap="small">
                <Typography.Text>
                  Рейтинг — сумма по блокам: коэффициент × средняя доля баллов
                  компетенций блока от максимума их шкал (взвешенная, если у
                  блока свои веса компетенций).
                </Typography.Text>
                <Typography.Text>
                  Компетенции блока весят одинаково. «Свои веса компетенций»
                  задают вес каждой компетенции на уровне в процентах, сумма
                  весов блока на уровне — {MAX_PERCENT} %.
                </Typography.Text>
                <Typography.Text>
                  Минимальный общий порог — рейтинг, нужный для уровня (не задан
                  — порога нет). Если на уровне все компетенции критические,
                  порог не задаётся: готовность определяют только критические
                  компетенции.
                </Typography.Text>
                <Typography.Text>
                  Кандидат готов к уровню, когда рейтинг не ниже порога и каждая
                  критическая компетенция набрала требуемый балл.
                </Typography.Text>
              </Flex>
            ),
          },
        ]}
      />
    </Flex>
  );
}

function SumCheck({ sum }: { sum: number }) {
  return sum === MAX_PERCENT ? (
    sum
  ) : (
    <Typography.Text type="danger">
      <CloseCircleOutlined /> {`${sum} — нужно ${MAX_PERCENT}`}
    </Typography.Text>
  );
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
  competencyNames,
  changed,
  isEditable,
  isCompact = false,
  onChange,
}: MatrixBlocksProps) {
  const outsideCount = content.competencies.filter(
    (item) => item.blockId === null,
  ).length;
  const groupIds = [
    ...sharedBlocks.map((entry) => entry.id),
    ...(outsideCount > 0 ? [null] : []),
  ];
  const isCriticalOnly = areAllCritical(content, [
    ...content.competencies.map((item) => item.competencyId),
    ...sharedBlocks.flatMap((entry) => entry.competencyIds),
  ]);
  const isPrepared = useRef(false);
  useEffect(() => {
    if (isPrepared.current || !isEditable) return;
    isPrepared.current = true;
    const prepared = withEvenBlockWeights(content, levelIds, groupIds);
    const result = isCriticalOnly ? withoutOverallMins(prepared) : prepared;
    if (result !== content) onChange(result);
  });
  const rows: Row[] = [
    ...sharedBlocks.flatMap((entry) => [
      {
        kind: "shared" as const,
        key: `shared:${entry.id}`,
        blockId: entry.id,
        entry,
      },
      ...(hasOwnWeights(content, entry.competencyIds)
        ? [
            ...entry.competencyIds.map((competencyId) => ({
              kind: "weight" as const,
              key: `weight:${entry.id}:${competencyId}`,
              blockId: entry.id,
              competencyId,
              memberIds: entry.competencyIds,
            })),
            {
              kind: "weightSum" as const,
              key: `weightSum:${entry.id}`,
              blockId: entry.id,
              memberIds: entry.competencyIds,
            },
          ]
        : []),
    ]),
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
    ...(isSingleLevel
      ? []
      : [{ kind: "education" as const, key: "education" }]),
  ];
  const levelColumns: TableColumnsType<Row> = levelIds.map((levelId) => {
    const levelName = levelNames.get(levelId) ?? "";
    const sum = coefficientSum(content, levelId);
    return {
      key: levelId,
      title: isSingleLevel ? "Коэф., %" : `${levelName}: коэф., %`,
      render: (_, row) => {
        if (row.kind === "sum") {
          return (
            <Space size="small" wrap>
              <SumCheck sum={sum} />
              {isEditable && (
                <Button
                  size="small"
                  disabled={groupIds.length === 0}
                  aria-label={
                    isSingleLevel
                      ? undefined
                      : `Распределить поровну: ${levelName}`
                  }
                  onClick={() =>
                    onChange(distributeBlockWeights(content, levelId, groupIds))
                  }
                >
                  Распределить поровну
                </Button>
              )}
            </Space>
          );
        }
        if (row.kind === "weightSum") {
          return (
            <SumCheck sum={ownWeightSum(content, levelId, row.memberIds)} />
          );
        }
        if (row.kind === "weight") {
          const weight = competencyWeightsOf(content).find(
            (item) =>
              item.levelId === levelId &&
              item.competencyId === row.competencyId,
          )?.percent;
          const name = competencyNames.get(row.competencyId) ?? NO_VALUE;
          return (
            <Space size="small">
              {isEditable ? (
                <InputNumber
                  aria-label={`Вес, %: ${levelName}, ${name}`}
                  min={0}
                  max={MAX_PERCENT}
                  value={weight ?? null}
                  onChange={(value) =>
                    onChange(
                      setCompetencyWeight(
                        content,
                        levelId,
                        row.competencyId,
                        value,
                      ),
                    )
                  }
                />
              ) : (
                (weight ?? NO_VALUE)
              )}
              {changed.weightCells.has(cellKey(levelId, row.competencyId)) && (
                <EditOutlined
                  role="img"
                  aria-label={CHANGED_NOTE}
                  title={CHANGED_NOTE}
                />
              )}
            </Space>
          );
        }
        if (row.kind === "min") {
          const level = content.levels.find((item) => item.levelId === levelId);
          if (isCriticalOnly) {
            return (
              <Flex vertical gap="small">
                {isEditable && (
                  <InputNumber
                    aria-label={`Минимальный общий порог, %: ${levelName}`}
                    disabled
                    value={null}
                  />
                )}
                <Typography.Text type="secondary">
                  {CRITICAL_ONLY_NOTE}
                </Typography.Text>
              </Flex>
            );
          }
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
        if (row.kind === "education") {
          const education = content.levels.find(
            (item) => item.levelId === levelId,
          )?.requiredEducation;
          return isEditable ? (
            <Select
              className="matrix-education-select"
              aria-label={`Требуемое образование: ${levelName}`}
              allowClear
              options={EDUCATION_OPTIONS}
              value={education ?? null}
              onChange={(value) =>
                onChange(setRequiredEducation(content, levelId, value ?? null))
              }
            />
          ) : (
            (education && EDUCATION_LABELS[education]) || NO_VALUE
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
        if (row.kind === "weightSum") return <strong>Сумма весов</strong>;
        if (row.kind === "weight") {
          return (
            <Typography.Text type="secondary">
              {`Вес: ${competencyNames.get(row.competencyId) ?? NO_VALUE}`}
            </Typography.Text>
          );
        }
        if (row.kind === "min") {
          return <strong>Минимальный общий порог, %</strong>;
        }
        if (row.kind === "education") {
          return <strong>Требуемое образование</strong>;
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
            <Checkbox
              disabled={!isEditable || row.entry.competencyIds.length === 0}
              checked={hasOwnWeights(content, row.entry.competencyIds)}
              aria-label={`Свои веса компетенций: ${row.entry.name}`}
              onChange={(event) =>
                onChange(
                  setOwnWeights(
                    content,
                    levelIds,
                    row.entry.competencyIds,
                    event.target.checked,
                  ),
                )
              }
            >
              Свои веса компетенций
            </Checkbox>
          </Space>
        );
      },
    },
    ...levelColumns,
  ];

  return (
    <section data-tour="matrix-blocks">
      {!isCompact && (
        <Typography.Title level={2}>Коэффициенты блоков</Typography.Title>
      )}
      <Flex vertical gap="middle">
        <FormulaExplanation isSingleLevel={isSingleLevel} isOpen={isCompact} />
        <Table<Row>
          className="matrix-grid"
          rowKey="key"
          columns={columns}
          dataSource={rows}
          pagination={false}
          scroll={{ x: "max-content" }}
        />
      </Flex>
    </section>
  );
}
