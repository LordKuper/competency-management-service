import {
  EditOutlined,
  PlusCircleOutlined,
  WarningOutlined,
} from "@ant-design/icons";
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
  Grid,
  Select,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  Tooltip,
  Typography,
  theme,
} from "antd";
import { useEffect, useState } from "react";
import { Link } from "react-router";
import type { LeaveEdits } from "../../app/leaveGuard";
import { useEditBase } from "../../app/useEditBase";
import { BuilderFooter, type BuilderStepControls } from "./BuilderFooter";
import { CatalogStatusTag } from "./CatalogParts";
import { type CatalogStatus, competencyQuery, levelsQuery } from "./catalogApi";
import { COMPETENCY_TYPES, CompetencyTypeSelect } from "./competencyType";
import { MatrixBlocks } from "./MatrixBlocks";
import { MatrixChanges } from "./MatrixChanges";
import { MatrixErrorAlert } from "./MatrixErrorAlert";
import { AddLevelModal } from "./MatrixPickers";
import {
  invalidateMatrices,
  type MatrixContent,
  type MatrixDiff,
  type MatrixSharedBlockEntry,
  type MatrixVersion,
  matrixChangesQuery,
  saveMatrixContent,
  storeSavedVersion,
} from "./matrixApi";
import {
  addLevel,
  cellKey,
  changedPlaces,
  chipLevel,
  NO_CHANGES,
  removeLevel,
  requirementOf,
  setCompetencyType,
  typeOfCompetency,
  withRequirement,
} from "./matrixContent";
import {
  compositionChanges,
  compositionsOf,
  minimumScoreOf,
  OUTSIDE_LABEL,
  POSITION_COLUMN_LABEL,
  scaleNameOf,
  scaleOfCompetency,
  sharedEntriesOf,
  versionCompetencyIds,
} from "./matrixGroups";
import { CHANGED_NOTE } from "./matrixLabels";
import { useBlockNames } from "./useBlockNames";
import "./matrix.css";

const NO_VALUE = "—";
const TYPE_COLUMN_WIDTH_IN_CONTROLS = 6;
const NAME_COLUMN_WIDTH_IN_CONTROLS = 9;
const LEVEL_COLUMN_WIDTH_IN_CONTROLS = 4;

interface BuilderSave {
  /** The step the wizard records as the one to open next time. */
  step: number;
  then: "next" | "exit";
}

type Dialog = { kind: "level" };

/** A row that opens a group of competencies; its cell spans the table. */
type GroupHeader =
  | { kind: "shared"; block: MatrixSharedBlockEntry }
  | { kind: "outside" };

interface GridRow {
  key: string;
  /** The competency the row is for; null for a group header. */
  competencyId: string | null;
  group?: GroupHeader;
}

/** What a grid shows of a competency. */
export interface CompetencyInfo {
  name: string;
  status?: CatalogStatus;
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
  /** Told whether the form holds edits that are not saved yet and whether a save is in flight, so the page holds back the transitions; omitted for a read-only view. */
  onEditsChange?: (edits: LeaveEdits) => void;
  /** Shows one step of the matrix wizard instead of the whole editor: its part of the version and its own buttons. */
  builder?: BuilderStepControls;
  /** Lays the editor out for a scenario screen: closer blocks and headings of the fourth level; a wizard step is always compact. */
  isCompact?: boolean;
}

/**
 * The competency-by-level grid of a version with the type and score of each competency, and the coefficients of its
 * groups. It is editable when the version's actions allow, and then keeps the edits in the form until they are saved.
 * While a save is in flight its controls are disabled, so nothing typed then is lost when the saved version replaces the
 * form. Levels go in the track order: a published or archived version keeps the order its entries were frozen in, and
 * puts after them the levels only an earlier variant has; a version in work follows the catalogue.
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
  onEditsChange,
  builder,
  isCompact: isCompactRequested = false,
}: MatrixEditorBodyProps) {
  const { token } = theme.useToken();
  const isWide = Grid.useBreakpoint().lg === true;
  const isCompact = isCompactRequested || builder !== undefined;
  const queryClient = useQueryClient();
  const isEditable = version.actions.includes("Edit");
  const [edited, setEdited] = useState<MatrixContent>(base.content);
  const [dialog, setDialog] = useState<Dialog | null>(null);
  const shown = isEditable ? edited : version.content;
  const { entries } = version;
  const changed = diff ? changedPlaces(diff.changes) : NO_CHANGES;
  const blockNames = useBlockNames(version, diff);
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
    onSuccess: async (saved, request) => {
      await storeSavedVersion(queryClient, saved);
      onSaved?.();
      if (request) builder?.onSaved(request.then);
    },
    onError: noteSaveFailure,
    onSettled: () => invalidateMatrices(queryClient),
  });
  const isUnsaved =
    isEditable && JSON.stringify(edited) !== JSON.stringify(base.content);
  const isSaving = save.isPending;
  useEffect(
    () => onEditsChange?.({ isUnsaved, isSaving }),
    [isUnsaved, isSaving, onEditsChange],
  );

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
  const outsideIds = shown.competencies
    .filter((item) => item.blockId === null)
    .map((item) => item.competencyId);
  const rowOf = (competencyId: string): GridRow => ({
    key: competencyId,
    competencyId,
  });
  const rows: GridRow[] = [
    ...sharedEntries.flatMap((block) => [
      {
        key: `group:${block.id}`,
        competencyId: null,
        group: { kind: "shared", block } as const,
      },
      ...block.competencyIds.map(rowOf),
    ]),
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
  const levelOptions = (catalogLevels.data?.items ?? [])
    .filter(
      (level) =>
        level.status !== "Inactive" &&
        !shown.levels.some((used) => used.levelId === level.id),
    )
    .map((level) => ({ value: level.id, label: level.name }));

  function renderGroupHeader(group: GroupHeader) {
    if (group.kind === "outside") {
      return <Typography.Text strong>{OUTSIDE_LABEL}</Typography.Text>;
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

  function renderName(row: GridRow) {
    if (row.group) return renderGroupHeader(row.group);
    const competencyId = row.competencyId ?? "";
    const competency = competencies.get(competencyId);
    const isAdded = [...compositionChange.values()].some((change) =>
      change.added.includes(competencyId),
    );
    return (
      <Space orientation="vertical" size={0}>
        <Space size="small" wrap>
          <Typography.Text strong>{competency?.name}</Typography.Text>
          {competency?.status === "Draft" && (
            <CatalogStatusTag status="Draft" />
          )}
        </Space>
        <Space size="small">
          <Typography.Text type="secondary">
            {scaleNameOf(version, competencyId, competency)}
          </Typography.Text>
          {isAdded && (
            <Tooltip title="Добавлена в блок">
              <PlusCircleOutlined
                role="img"
                aria-label="Добавлена в блок"
                className="matrix-mark"
              />
            </Tooltip>
          )}
          {changed.competencies.has(competencyId) && (
            <Tooltip title={CHANGED_NOTE}>
              <EditOutlined
                role="img"
                aria-label={CHANGED_NOTE}
                className="matrix-mark"
              />
            </Tooltip>
          )}
        </Space>
      </Space>
    );
  }

  function renderType(row: GridRow) {
    if (row.group) return null;
    const competencyId = row.competencyId ?? "";
    const type = typeOfCompetency(shown, competencyId);
    return (
      <Space orientation="vertical" size={0}>
        {isEditable ? (
          <CompetencyTypeSelect
            label={`Тип компетенции «${competencies.get(competencyId)?.name ?? ""}»`}
            value={type}
            onChange={(next) =>
              setEdited(
                setCompetencyType(
                  edited,
                  competencyId,
                  next,
                  compositionsOf(version),
                ),
              )
            }
          />
        ) : (
          COMPETENCY_TYPES[type]
        )}
      </Space>
    );
  }

  function renderCell(levelId: string, row: GridRow) {
    if (row.group || row.competencyId === null) return null;
    const competencyId = row.competencyId;
    const levelName = levelNames.get(levelId) ?? "";
    const { points } = scaleOfCompetency(version, competencyId);
    const score =
      requirementOf(shown, levelId, competencyId)?.requiredScore ??
      minimumScoreOf(points);
    const changedMark = changed.requirementCells.has(
      cellKey(levelId, competencyId),
    ) && (
      <EditOutlined role="img" aria-label={CHANGED_NOTE} title={CHANGED_NOTE} />
    );
    const chip =
      score == null ? (
        NO_VALUE
      ) : (
        <span
          className={`rating-chip rating-chip--${chipLevel(score, points)}`}
          title={points.find((point) => point.score === score)?.label}
        >
          {score}
        </span>
      );
    if (!isEditable) {
      return (
        <>
          {chip}
          {changedMark}
        </>
      );
    }
    return (
      <Space size="small">
        <Select<number>
          size="small"
          className="matrix-score-select"
          aria-label={`${competencies.get(competencyId)?.name}, ${levelName}`}
          value={score}
          labelRender={() => chip}
          options={[...points]
            .sort((left, right) => left.score - right.score)
            .map((point) => ({
              value: point.score,
              label: `${point.score} — ${point.label}`,
            }))}
          onChange={(next) =>
            setEdited(
              withRequirement(edited, {
                levelId,
                competencyId,
                requiredScore: next,
              }),
            )
          }
        />
        {changedMark}
      </Space>
    );
  }

  const columns: TableColumnsType<GridRow> = [
    {
      title: "Компетенция",
      key: "competency",
      fixed: "left",
      width: token.controlHeight * NAME_COLUMN_WIDTH_IN_CONTROLS,
      className: "matrix-name-cell",
      render: (_, row) => renderName(row),
    },
    {
      title: "Тип",
      key: "type",
      fixed: "left",
      width: token.controlHeight * TYPE_COLUMN_WIDTH_IN_CONTROLS,
      className: "matrix-type-cell",
      render: (_, row) => renderType(row),
    },
    ...shownLevelIds.map((levelId) => ({
      key: levelId,
      width: token.controlHeight * LEVEL_COLUMN_WIDTH_IN_CONTROLS,
      className: "matrix-cell",
      title: (
        <Space orientation="vertical" size={0}>
          <span>
            {levelNames.get(levelId)}
            {changed.levels.has(levelId) && (
              <EditOutlined
                role="img"
                aria-label={CHANGED_NOTE}
                title={CHANGED_NOTE}
                className="matrix-mark"
              />
            )}
          </span>
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
  const tableWidth =
    token.controlHeight *
    (NAME_COLUMN_WIDTH_IN_CONTROLS +
      TYPE_COLUMN_WIDTH_IN_CONTROLS +
      LEVEL_COLUMN_WIDTH_IN_CONTROLS * shownLevelIds.length);
  const isGridShown = builder === undefined || builder.part === "requirements";
  const isBlocksShown = builder === undefined || builder.part === "formula";

  const usedScales = entries.scales ?? [];
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
            {[...scale.points]
              .sort((left, right) => left.score - right.score)
              .map((point) => (
                <li key={point.score}>
                  <span
                    className={`rating-chip rating-chip--${chipLevel(point.score, scale.points)}`}
                  >
                    {point.score}
                  </span>
                  <span>{point.label}</span>
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
      tableLayout="fixed"
      scroll={{ x: tableWidth }}
      rowClassName={(row) =>
        row.group
          ? "matrix-group-row"
          : row.competencyId && changed.competencies.has(row.competencyId)
            ? "matrix-changed-row"
            : ""
      }
    />
  );
  const compositionLink = (
    <Link to={`/desk/matrix-builder/${version.id}?step=2`}>
      Изменить состав и блоки в мастере, шаг 2
    </Link>
  );

  return (
    <ConfigProvider componentDisabled={save.isPending}>
      <Space
        orientation="vertical"
        size={isCompact ? "middle" : "large"}
        style={{ display: "flex" }}
      >
        {isEditable && !builder && (
          <Flex gap="small" align="center" wrap>
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
            {compositionLink}
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
            blockNames={blockNames}
            pointsOf={(id) => scaleOfCompetency(version, id).points}
          />
        )}
        {isGridShown &&
          (builder ? (
            <div
              className={
                isWide
                  ? "matrix-step-columns matrix-step-columns--wide"
                  : "matrix-step-columns"
              }
            >
              <Card size="small" styles={{ body: { padding: 0 } }}>
                {grid}
              </Card>
              <Card size="small" title="Требуемый балл">
                <Space orientation="vertical" size="small">
                  <Typography.Text type="secondary">
                    Выберите балл в клетке. Баллы — шкалы самой компетенции; для
                    критической балл — порог уровня.
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
        {builder && (
          <BuilderFooter
            step={builder.step}
            onBack={builder.onBack}
            onSkip={builder.onSkip}
            isEditable={isEditable}
            isUnsaved={isUnsaved}
            isSaving={isSaving}
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
      </Space>
    </ConfigProvider>
  );
}
