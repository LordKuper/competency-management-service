import { EditOutlined, WarningOutlined } from "@ant-design/icons";
import {
  useMutation,
  useQueries,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  Button,
  Card,
  ConfigProvider,
  Flex,
  InputNumber,
  Select,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useEffect, useState } from "react";
import { useEditBase } from "../../app/useEditBase";
import { BuilderFooter, type BuilderStepControls } from "./BuilderFooter";
import { CatalogStatusTag } from "./CatalogParts";
import { IN_WORK } from "./CatalogSelect";
import { COMPETENCY_TYPES } from "./CompetencyModal";
import {
  type CompetencyType,
  competencyQuery,
  levelsQuery,
} from "./catalogApi";
import { MatrixBlocks } from "./MatrixBlocks";
import { MatrixChanges } from "./MatrixChanges";
import { MatrixErrorAlert } from "./MatrixErrorAlert";
import { MatrixFormulaTotals } from "./MatrixFormulaTotals";
import {
  AddCompetencyModal,
  AddLevelModal,
  AddOwnBlockModal,
} from "./MatrixPickers";
import {
  invalidateMatrices,
  type MatrixBlock,
  type MatrixContent,
  type MatrixDiff,
  type MatrixSharedBlockEntry,
  type MatrixVersion,
  matrixChangesQuery,
  matrixVersionQuery,
  saveMatrixContent,
} from "./matrixApi";
import {
  addBlock,
  addCompetency,
  addLevel,
  cellKey,
  changedPlaces,
  chipLevel,
  NO_CHANGES,
  removeCompetency,
  removeLevel,
  requirementOf,
  setCompetencyBlock,
  setOverallMin,
  updateBlock,
  withoutRequirement,
  withRequirement,
} from "./matrixContent";
import {
  compositionChanges,
  OUTSIDE_LABEL,
  POSITION_COLUMN_LABEL,
  scaleNameOf,
  scaleOfCompetency,
  sharedBlockOf,
  sharedEntriesOf,
  versionCompetencyIds,
} from "./matrixGroups";
import { CHANGED_NOTE } from "./matrixLabels";
import { RequirementModal } from "./RequirementModal";
import "./matrix.css";

const NO_VALUE = "—";
const MAX_PERCENT = 100;
const UNNAMED_BLOCK = "Название блока";
const OUTSIDE_VALUE = "__outside__";

interface BuilderSave {
  /** The step the wizard records as the one to open next time. */
  step: number;
  then: "next" | "exit";
}

type Dialog =
  | { kind: "level" }
  | { kind: "competency" }
  | { kind: "block" }
  | { kind: "requirement"; levelId: string; competencyId: string };

/** A row that opens a group of competencies; its cell spans the table. */
type GroupHeader =
  | { kind: "shared"; block: MatrixSharedBlockEntry }
  | { kind: "own"; block: MatrixBlock }
  | { kind: "outside" };

interface GridRow {
  key: string;
  /** The competency the row is for; null for the row of the minimum overall threshold and for a group header. */
  competencyId: string | null;
  group?: GroupHeader;
}

/** What a grid shows of a competency. */
export interface CompetencyInfo {
  name: string;
  type: CompetencyType;
  categoryName?: string;
  scaleId?: string | null;
  scaleName?: string;
}

interface MatrixEditorProps {
  /** The version as the cache holds it now; a version without the edit action is shown read-only. */
  version: MatrixVersion;
  /** What the approver changed against the variant before; omitted when there is nothing to show. */
  diff?: MatrixDiff;
  /** Called after a save, once the cache holds the saved version; omitted for a read-only view. */
  onSaved?: () => void;
  /** Told whether the form holds edits that are not saved yet or are being saved, so the page holds back the transitions; omitted for a read-only view. */
  onUnsavedChange?: (isUnsaved: boolean) => void;
  /** Shows one step of the matrix wizard instead of the whole editor: its part of the version and its own buttons. */
  builder?: BuilderStepControls;
  /** Lays the editor out for a scenario screen: closer blocks and headings of the fourth level; a wizard step is always compact. */
  isCompact?: boolean;
}

/**
 * The competency-by-level grid with the blocks and thresholds of a version. It is editable when the version's
 * actions allow, and then keeps the edits in the form until they are saved. While a save is in flight its controls are
 * disabled, so nothing typed then is lost when the saved version replaces the form. Levels go in the track order: a
 * published or archived version keeps the order its entries were frozen in, and puts after them the levels only an
 * earlier variant has; a version in work follows the catalogue.
 */
export function MatrixEditor(props: MatrixEditorProps) {
  const { base, noteSaveFailure } = useEditBase(props.version);
  return (
    <MatrixEditorBody
      key={base?.version}
      {...props}
      base={base ?? props.version}
      noteSaveFailure={noteSaveFailure}
    />
  );
}

interface MatrixEditorBodyProps extends MatrixEditorProps {
  base: MatrixVersion;
  noteSaveFailure: (error: unknown) => void;
}

/**
 * Names and kinds of the competencies the grid refers to: the version's entries, and for any other, such as one just
 * added or one the approver removed, the catalogue entry read by its id.
 */
export function useCompetencies(
  entries: MatrixVersion["entries"],
  ids: readonly string[],
): Map<string, CompetencyInfo> {
  const known = new Map<string, CompetencyInfo>(
    entries.competencies.map((competency) => [competency.id, competency]),
  );
  const missing = [...new Set(ids)].filter((id) => !known.has(id));
  const reads = useQueries({ queries: missing.map(competencyQuery) });
  for (const read of reads) {
    if (read.data && !known.has(read.data.id))
      known.set(read.data.id, read.data);
  }
  return known;
}

function MatrixEditorBody({
  version,
  base,
  diff,
  noteSaveFailure,
  onSaved,
  onUnsavedChange,
  builder,
  isCompact: isCompactRequested = false,
}: MatrixEditorBodyProps) {
  const isCompact = isCompactRequested || builder !== undefined;
  const queryClient = useQueryClient();
  const isEditable = version.actions.includes("Edit");
  const [edited, setEdited] = useState<MatrixContent>(base.content);
  const [dialog, setDialog] = useState<Dialog | null>(null);
  const shown = isEditable ? edited : version.content;
  const { entries } = version;
  const changed = diff ? changedPlaces(diff.changes) : NO_CHANGES;
  const isPosition = entries.position != null;
  const sharedEntries = sharedEntriesOf(version);
  const isFrozen =
    version.status === "Published" || version.status === "Archived";
  const publishedChanges = useQuery({
    ...matrixChangesQuery(version.id),
    enabled: sharedEntries.length > 0 && !isFrozen,
  });
  const compositionChange = compositionChanges(publishedChanges.data);

  const catalogLevels = useQuery({
    ...levelsQuery(entries.track?.id ?? ""),
    enabled: entries.track !== null,
  });
  const save = useMutation({
    mutationFn: (request: BuilderSave | undefined) =>
      saveMatrixContent(
        base,
        edited,
        request && Math.max(base.builderStep ?? 0, request.step),
      ),
    onSuccess: (saved, request) => {
      queryClient.setQueryData(matrixVersionQuery(saved.id).queryKey, saved);
      onSaved?.();
      if (request) builder?.onSaved(request.then);
    },
    onError: noteSaveFailure,
    onSettled: () => invalidateMatrices(queryClient),
  });
  const isUnsaved =
    isEditable &&
    (save.isPending || JSON.stringify(edited) !== JSON.stringify(base.content));
  useEffect(() => onUnsavedChange?.(isUnsaved), [isUnsaved, onUnsavedChange]);

  const catalogOrder = catalogLevels.data?.items ?? [];
  const levelNames = new Map<string, string>([
    ...catalogOrder.map((level) => [level.id, level.name] as const),
    ...entries.levels.map((level) => [level.id, level.name] as const),
  ]);
  if (isPosition) {
    for (const level of shown.levels) {
      levelNames.set(level.levelId, POSITION_COLUMN_LABEL);
    }
  }
  const trackOrder = (
    isFrozen
      ? [...entries.levels, ...catalogOrder]
      : [...catalogOrder, ...entries.levels]
  ).map((level) => level.id);
  const rank = (levelId: string) => {
    const index = trackOrder.indexOf(levelId);
    return index === -1 ? trackOrder.length : index;
  };
  const shownLevelIds = shown.levels
    .map((level) => level.levelId)
    .sort((left, right) => rank(left) - rank(right));
  const shownCompetencyIds = versionCompetencyIds(version, shown);
  const competencies = useCompetencies(entries, [
    ...shownCompetencyIds,
    ...(diff?.before.competencies ?? []).map((item) => item.competencyId),
  ]);
  const ownBlockOptions = shown.blocks.map((block) => ({
    value: block.id,
    label: block.name || UNNAMED_BLOCK,
  }));
  const blockOptions = [
    ...ownBlockOptions,
    { value: OUTSIDE_VALUE, label: OUTSIDE_LABEL },
  ];
  const hasRequirement = (competencyId: string) =>
    shown.requirements.some((item) => item.competencyId === competencyId);
  const outsideIds = shown.competencies
    .filter((item) => item.blockId === null)
    .map((item) => item.competencyId);
  const rowOf = (competencyId: string): GridRow => ({
    key: competencyId,
    competencyId,
  });
  const groupedRows = (): GridRow[] => [
    ...sharedEntries.flatMap((block) => [
      {
        key: `group:${block.id}`,
        competencyId: null,
        group: { kind: "shared", block } as const,
      },
      ...block.competencyIds.map(rowOf),
    ]),
    ...shown.blocks.flatMap((block) => {
      const ids = shown.competencies
        .filter((item) => item.blockId === block.id)
        .map((item) => item.competencyId);
      return ids.length === 0
        ? []
        : [
            {
              key: `group:${block.id}`,
              competencyId: null,
              group: { kind: "own", block } as const,
            },
            ...ids.map(rowOf),
          ];
    }),
    ...(outsideIds.length === 0
      ? []
      : [
          {
            key: "group:outside",
            competencyId: null,
            group: { kind: "outside" } as const,
          },
          ...outsideIds.map(rowOf),
        ]),
  ];
  const isOverallMinimumShown = builder === undefined;
  const rows: GridRow[] = [
    ...(builder ? shownCompetencyIds.map(rowOf) : groupedRows()),
    ...(isOverallMinimumShown
      ? [{ key: "overall-minimum", competencyId: null }]
      : []),
  ];
  const levelOptions = (catalogLevels.data?.items ?? [])
    .filter(
      (level) =>
        level.status !== "Inactive" &&
        !shown.levels.some((used) => used.levelId === level.id),
    )
    .map((level) => ({ value: level.id, label: level.name }));
  const competencyFilters = {
    ...IN_WORK,
    directionId: entries.direction?.id,
    withGeneral: true,
  };

  function renderGroupHeader(group: GroupHeader) {
    if (group.kind === "own") {
      return (
        <Space size="small" wrap>
          <Typography.Text strong>
            {group.block.name || UNNAMED_BLOCK}
          </Typography.Text>
          <Typography.Text type="secondary">
            собственный блок версии · шкала версии
          </Typography.Text>
        </Space>
      );
    }
    if (group.kind === "outside") {
      return (
        <Space size="small" wrap>
          <Typography.Text strong>{OUTSIDE_LABEL}</Typography.Text>
          <Typography.Text type="secondary">шкала версии</Typography.Text>
        </Space>
      );
    }
    const { block } = group;
    const change = compositionChange.get(block.id);
    return (
      <Space size="small" wrap>
        <Typography.Text strong>{block.name}</Typography.Text>
        <Tag>Общий блок</Tag>
        {block.status === "Draft" && <CatalogStatusTag status="Draft" />}
        {change && (
          <Tag icon={<WarningOutlined />} color="warning">
            Состав изменился
          </Tag>
        )}
        {change && (
          <Typography.Text type="secondary">
            {[
              change.added.length > 0 &&
                `Добавлено: ${change.added.map((id) => `«${competencies.get(id)?.name}»`).join(", ")}`,
              change.removed.length > 0 &&
                `Убрано: ${change.removed.map((id) => `«${competencies.get(id)?.name}»`).join(", ")}`,
            ]
              .filter(Boolean)
              .join("; ")}
            ; состав правится в справочнике
          </Typography.Text>
        )}
      </Space>
    );
  }

  function renderBlockControl(competencyId: string) {
    const shared = sharedBlockOf(version, competencyId);
    if (shared) {
      return (
        <Space size="small" wrap>
          <Tag>{`Общий блок «${shared.name}»`}</Tag>
        </Space>
      );
    }
    const block = shown.competencies.find(
      (item) => item.competencyId === competencyId,
    );
    const value = block?.blockId ?? OUTSIDE_VALUE;
    return isEditable ? (
      <Select
        size="small"
        aria-label="Блок компетенций"
        value={value}
        options={blockOptions}
        onChange={(next) =>
          setEdited(
            setCompetencyBlock(
              edited,
              competencyId,
              next === OUTSIDE_VALUE ? null : next,
            ),
          )
        }
      />
    ) : (
      <Typography.Text type="secondary">
        {value === OUTSIDE_VALUE
          ? OUTSIDE_LABEL
          : ownBlockOptions.find((option) => option.value === value)?.label}
      </Typography.Text>
    );
  }

  function renderName(row: GridRow) {
    if (row.group) return renderGroupHeader(row.group);
    if (row.competencyId === null) return "Минимальный общий порог";
    const competency = competencies.get(row.competencyId);
    const competencyId = row.competencyId;
    const isShared = sharedBlockOf(version, competencyId) !== undefined;
    const isAdded = [...compositionChange.values()].some((change) =>
      change.added.includes(competencyId),
    );
    return (
      <Space orientation="vertical" size={0}>
        <Typography.Text strong>{competency?.name}</Typography.Text>
        {competency?.categoryName && (
          <Typography.Text type="secondary">
            {[
              competency.categoryName,
              scaleNameOf(version, competencyId, competency),
            ]
              .filter(Boolean)
              .join(" · ")}
          </Typography.Text>
        )}
        {competency?.type === "Critical" && (
          <Tag icon={<WarningOutlined />} color="error">
            {COMPETENCY_TYPES.Critical}
          </Tag>
        )}
        {(builder || (isEditable && !isShared)) &&
          renderBlockControl(competencyId)}
        {isAdded && (
          <Tag icon={<WarningOutlined />} color="warning">
            Добавлена в блок
          </Tag>
        )}
        {!isFrozen && !hasRequirement(competencyId) && (
          <Tag icon={<WarningOutlined />} color="warning">
            Нет требований
          </Tag>
        )}
        {changed.competencies.has(competencyId) && (
          <Tag icon={<EditOutlined />} color="warning">
            {CHANGED_NOTE}
          </Tag>
        )}
        {isEditable && !isShared && (
          <Button
            type="link"
            size="small"
            danger
            onClick={() => setEdited(removeCompetency(edited, competencyId))}
          >
            {builder ? "Убрать" : "Удалить"}
          </Button>
        )}
      </Space>
    );
  }

  function renderCell(levelId: string, row: GridRow) {
    const levelName = levelNames.get(levelId) ?? "";
    if (row.group) return null;
    if (row.competencyId === null) {
      const level = shown.levels.find((item) => item.levelId === levelId);
      return isEditable ? (
        <InputNumber
          aria-label={`Минимальный общий порог: ${levelName}`}
          min={0}
          max={MAX_PERCENT}
          value={level?.minOverallPercent ?? null}
          onChange={(value) => setEdited(setOverallMin(edited, levelId, value))}
        />
      ) : (
        (level?.minOverallPercent ?? NO_VALUE)
      );
    }
    const competencyId = row.competencyId;
    const requirement = requirementOf(shown, levelId, competencyId);
    const changedMark = changed.requirementCells.has(
      cellKey(levelId, competencyId),
    ) && (
      <EditOutlined role="img" aria-label={CHANGED_NOTE} title={CHANGED_NOTE} />
    );
    if (!requirement && !isEditable) {
      return (
        <>
          {NO_VALUE}
          {changedMark}
        </>
      );
    }
    const score = requirement?.requiredScore;
    const { points } = scaleOfCompetency(version, competencyId);
    const label = points.find((point) => point.score === score)?.label;
    return (
      <button
        type="button"
        className="matrix-cell-button"
        disabled={save.isPending}
        aria-label={`${competencies.get(competencyId)?.name}, ${levelName}`}
        onClick={() =>
          setDialog({ kind: "requirement", levelId, competencyId })
        }
      >
        {score == null ? (
          NO_VALUE
        ) : (
          <span
            className={`rating-chip rating-chip--${chipLevel(score, points)}`}
            title={label}
          >
            {score}
          </span>
        )}
        {changedMark}
      </button>
    );
  }

  const columns: TableColumnsType<GridRow> = [
    {
      title: "Компетенции",
      key: "competency",
      fixed: "left",
      className: "matrix-name-cell",
      render: (_, row) => renderName(row),
    },
    ...shownLevelIds.map((levelId) => ({
      key: levelId,
      className: "matrix-cell",
      title: (
        <Space orientation="vertical" size={0}>
          {levelNames.get(levelId)}
          {changed.levels.has(levelId) && (
            <Tag icon={<EditOutlined />} color="warning">
              {CHANGED_NOTE}
            </Tag>
          )}
          {isEditable && !builder && !isPosition && (
            <Button
              type="link"
              size="small"
              danger
              onClick={() => setEdited(removeLevel(edited, levelId))}
            >
              Удалить
            </Button>
          )}
        </Space>
      ),
      onCell: (row: GridRow) => ({
        className:
          row.competencyId &&
          changed.requirementCells.has(cellKey(levelId, row.competencyId))
            ? "matrix-changed"
            : undefined,
      }),
      render: (_: unknown, row: GridRow) => renderCell(levelId, row),
    })),
  ];
  const requested = dialog?.kind === "requirement" ? dialog : null;
  const isGridShown = builder === undefined || builder.part === "requirements";
  const isBlocksShown = builder === undefined || builder.part === "formula";

  const usedScales = [
    ...(entries.scales ?? []),
    ...(entries.scale ? [entries.scale] : []),
  ].filter(
    (scale, index, all) =>
      all.findIndex((other) => other.id === scale.id) === index,
  );
  const legend = (
    <Space orientation="vertical" size="small">
      {usedScales.map((scale) => (
        <Space key={scale.id} orientation="vertical" size={0}>
          {usedScales.length > 1 && (
            <Typography.Text strong>{scale.name}</Typography.Text>
          )}
          <ul
            className={`matrix-legend${builder ? " matrix-legend--vertical" : ""}`}
          >
            {scale.points.map((point) => (
              <li key={point.score}>
                <span
                  className={`rating-chip rating-chip--${chipLevel(point.score, scale.points)}`}
                >
                  {point.score}
                </span>
                {point.label}
              </li>
            ))}
          </ul>
        </Space>
      ))}
    </Space>
  );
  const grid = (
    <Table<GridRow>
      className="matrix-grid"
      rowKey="key"
      columns={columns}
      dataSource={rows}
      pagination={false}
      scroll={{ x: "max-content" }}
      rowClassName={(row) =>
        row.group
          ? "matrix-group-row"
          : row.competencyId && changed.competencies.has(row.competencyId)
            ? "matrix-changed-row"
            : ""
      }
    />
  );

  return (
    <ConfigProvider componentDisabled={save.isPending}>
      <Space
        orientation="vertical"
        size={isCompact ? "middle" : "large"}
        style={{ display: "flex" }}
      >
        {isEditable && !builder && (
          <Flex gap="small" wrap>
            <Button onClick={() => setDialog({ kind: "competency" })}>
              Добавить компетенцию
            </Button>
            {!isPosition && (
              <Button onClick={() => setDialog({ kind: "level" })}>
                Добавить уровень
              </Button>
            )}
            <Button
              type="primary"
              loading={save.isPending}
              onClick={() => save.mutate(undefined)}
            >
              Сохранить
            </Button>
          </Flex>
        )}
        {isEditable && builder?.part === "requirements" && (
          <Flex>
            <Button onClick={() => setDialog({ kind: "block" })}>
              Добавить блок
            </Button>
          </Flex>
        )}
        {save.error != null && (
          <MatrixErrorAlert
            title="Не удалось сохранить запись"
            error={save.error}
          />
        )}
        {diff && (
          <MatrixChanges
            diff={diff}
            levelNames={levelNames}
            competencyNames={
              new Map(
                [...competencies].map(([id, competency]) => [
                  id,
                  competency.name,
                ]),
              )
            }
            pointsOf={(id) => scaleOfCompetency(version, id).points}
          />
        )}
        {isGridShown &&
          (builder ? (
            <div className="matrix-step-columns">
              {grid}
              <Card
                size="small"
                title={isPosition ? "Требуемый балл" : "Требуемый уровень"}
              >
                <Space orientation="vertical" size="small">
                  <Typography.Text type="secondary">
                    {`Нажмите ячейку, чтобы задать ${isPosition ? "балл" : "уровень"}. Блок выбирается в строке компетенции; у компетенции общего блока блок и шкала не меняются.`}
                  </Typography.Text>
                  {legend}
                </Space>
              </Card>
            </div>
          ) : (
            <>
              {legend}
              {grid}
            </>
          ))}
        {isBlocksShown && (
          <MatrixBlocks
            content={shown}
            sharedBlocks={sharedEntries}
            isSingleLevel={isPosition}
            levelIds={shownLevelIds}
            levelNames={levelNames}
            changed={changed}
            isEditable={isEditable}
            isCompact={isCompact}
            onChange={setEdited}
          />
        )}
        {builder?.part === "formula" && (
          <MatrixFormulaTotals
            content={shown}
            isSingleLevel={isPosition}
            levelIds={shownLevelIds}
            levelNames={levelNames}
            isEditable={isEditable}
            isCompact={isCompact}
            onChange={setEdited}
          />
        )}
        {builder && (
          <BuilderFooter
            step={builder.step}
            onBack={builder.onBack}
            onSkip={builder.onSkip}
            isEditable={isEditable}
            isUnsaved={isUnsaved}
            isSaving={save.isPending}
            onSave={(step, then) => save.mutate({ step, then })}
            onCancelEdits={() => setEdited(base.content)}
          />
        )}
        {dialog?.kind === "level" && (
          <AddLevelModal
            options={levelOptions}
            isLoading={catalogLevels.isFetching}
            onPick={(levelId) => {
              setEdited(addLevel(edited, levelId));
              setDialog(null);
            }}
            onClose={() => setDialog(null)}
          />
        )}
        {dialog?.kind === "block" && (
          <AddOwnBlockModal
            onPick={(name) => {
              const added = addBlock(edited);
              const created = added.blocks.at(-1);
              setEdited(
                created ? updateBlock(added, created.id, { name }) : added,
              );
              setDialog(null);
            }}
            onClose={() => setDialog(null)}
          />
        )}
        {dialog?.kind === "competency" && (
          <AddCompetencyModal
            filters={competencyFilters}
            usedIds={shownCompetencyIds}
            blocks={blockOptions}
            defaultBlock={OUTSIDE_VALUE}
            onPick={(competencyId, blockId) => {
              setEdited(
                addCompetency(
                  edited,
                  competencyId,
                  blockId === OUTSIDE_VALUE ? null : blockId,
                ),
              );
              setDialog(null);
            }}
            onClose={() => setDialog(null)}
          />
        )}
        {requested && (
          <RequirementModal
            title={
              isPosition
                ? (competencies.get(requested.competencyId)?.name ?? "")
                : `${competencies.get(requested.competencyId)?.name}, ${levelNames.get(requested.levelId)}`
            }
            levelId={requested.levelId}
            competencyId={requested.competencyId}
            requirement={requirementOf(
              shown,
              requested.levelId,
              requested.competencyId,
            )}
            defaultType={
              competencies.get(requested.competencyId)?.type ?? "Profile"
            }
            scalePoints={
              scaleOfCompetency(version, requested.competencyId).points
            }
            isEditable={isEditable}
            onSave={(requirement) => {
              setEdited(withRequirement(edited, requirement));
              setDialog(null);
            }}
            onRemove={() => {
              setEdited(
                withoutRequirement(
                  edited,
                  requested.levelId,
                  requested.competencyId,
                ),
              );
              setDialog(null);
            }}
            onClose={() => setDialog(null)}
          />
        )}
      </Space>
    </ConfigProvider>
  );
}
