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
import { AddCompetencyModal, AddLevelModal } from "./MatrixPickers";
import {
  invalidateMatrices,
  type MatrixContent,
  type MatrixDiff,
  type MatrixVersion,
  matrixVersionQuery,
  saveMatrixContent,
} from "./matrixApi";
import {
  addCompetency,
  addLevel,
  cellKey,
  changedPlaces,
  NO_CHANGES,
  removeCompetency,
  removeLevel,
  requirementOf,
  setCompetencyBlock,
  setOverallMin,
  withoutRequirement,
  withRequirement,
} from "./matrixContent";
import { CHANGED_NOTE } from "./matrixLabels";
import { RequirementModal } from "./RequirementModal";
import "./matrix.css";

const NO_VALUE = "—";
const MAX_CHIP_LEVEL = 4;
const MAX_PERCENT = 100;
const UNNAMED_BLOCK = "Название блока";

interface BuilderSave {
  /** The step the wizard records as the one to open next time. */
  step: number;
  then: "next" | "exit";
}

type Dialog =
  | { kind: "level" }
  | { kind: "competency" }
  | { kind: "requirement"; levelId: string; competencyId: string };

interface GridRow {
  key: string;
  competencyId: string | null;
}

/** What a grid shows of a competency. */
export interface CompetencyInfo {
  name: string;
  type: CompetencyType;
  categoryName?: string;
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

function chipLevel(score: number) {
  return Math.min(Math.max(score, 0), MAX_CHIP_LEVEL);
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
  const isFrozen =
    version.status === "Published" || version.status === "Archived";
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
  const competencies = useCompetencies(entries, [
    ...shown.competencies.map((item) => item.competencyId),
    ...(diff?.before.competencies ?? []).map((item) => item.competencyId),
  ]);
  const blockOptions = shown.blocks.map((block) => ({
    value: block.id,
    label: block.name || UNNAMED_BLOCK,
  }));
  const blockNames = new Map(
    blockOptions.map((option) => [option.value, option.label]),
  );
  const isOverallMinimumShown = builder === undefined;
  const rows: GridRow[] = [
    ...shown.competencies.map(({ competencyId }) => ({
      key: competencyId,
      competencyId,
    })),
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
    scaleId: version.scaleId,
    directionId: entries.direction?.id,
    withGeneral: true,
  };

  function renderName(row: GridRow) {
    if (row.competencyId === null) return "Минимальный общий порог";
    const competency = competencies.get(row.competencyId);
    const block = shown.competencies.find(
      (item) => item.competencyId === row.competencyId,
    );
    const competencyId = row.competencyId;
    return (
      <Space orientation="vertical" size={0}>
        <Typography.Text strong>{competency?.name}</Typography.Text>
        {competency?.categoryName && (
          <Typography.Text type="secondary">
            {competency.categoryName}
          </Typography.Text>
        )}
        {competency?.type === "Critical" && (
          <Tag icon={<WarningOutlined />} color="error">
            {COMPETENCY_TYPES.Critical}
          </Tag>
        )}
        {isEditable ? (
          <Select
            size="small"
            aria-label="Блок компетенций"
            value={block?.blockId}
            options={blockOptions}
            onChange={(blockId) =>
              setEdited(setCompetencyBlock(edited, competencyId, blockId))
            }
          />
        ) : (
          <Typography.Text type="secondary">
            {block?.blockId && blockNames.get(block.blockId)}
          </Typography.Text>
        )}
        {changed.competencies.has(competencyId) && (
          <Tag icon={<EditOutlined />} color="warning">
            {CHANGED_NOTE}
          </Tag>
        )}
        {isEditable && (
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
    const label = entries.scale.points.find(
      (point) => point.score === score,
    )?.label;
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
            className={`rating-chip rating-chip--${chipLevel(score)}`}
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
          {isEditable && !builder && (
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

  const legend = (
    <ul className={`matrix-legend${builder ? " matrix-legend--vertical" : ""}`}>
      {entries.scale.points.map((point) => (
        <li key={point.score}>
          <span
            className={`rating-chip rating-chip--${chipLevel(point.score)}`}
          >
            {point.score}
          </span>
          {point.label}
        </li>
      ))}
    </ul>
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
        row.competencyId && changed.competencies.has(row.competencyId)
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
            <Button onClick={() => setDialog({ kind: "level" })}>
              Добавить уровень
            </Button>
            <Button
              type="primary"
              loading={save.isPending}
              onClick={() => save.mutate(undefined)}
            >
              Сохранить
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
            scalePoints={entries.scale.points}
          />
        )}
        {isGridShown &&
          (builder ? (
            <div className="matrix-step-columns">
              {grid}
              <Card size="small" title="Требуемый уровень">
                <Space orientation="vertical" size="small">
                  <Typography.Text type="secondary">
                    Нажмите ячейку, чтобы задать уровень. Блок выбирается в
                    строке компетенции.
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
        {dialog?.kind === "competency" && (
          <AddCompetencyModal
            filters={competencyFilters}
            usedIds={shown.competencies.map((item) => item.competencyId)}
            blocks={blockOptions}
            onPick={(competencyId, blockId) => {
              setEdited(addCompetency(edited, competencyId, blockId));
              setDialog(null);
            }}
            onClose={() => setDialog(null)}
          />
        )}
        {requested && (
          <RequirementModal
            title={`${competencies.get(requested.competencyId)?.name}, ${levelNames.get(requested.levelId)}`}
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
            scalePoints={entries.scale.points}
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
