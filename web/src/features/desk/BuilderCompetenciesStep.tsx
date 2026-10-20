import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Button,
  Card,
  ConfigProvider,
  Flex,
  Input,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useEffect, useState } from "react";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { TruncationNote } from "../../app/TruncationNote";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { useEditBase } from "../../app/useEditBase";
import { BlockModal } from "../career/BlockModal";
import { BuilderFooter } from "../career/BuilderFooter";
import { CatalogStatusTag } from "../career/CatalogParts";
import { COMPETENCY_TYPES, CompetencyModal } from "../career/CompetencyModal";
import { useProcessRoles } from "../career/careerApi";
import {
  blockQuery,
  type Competency,
  type CompetencyBlock,
  competenciesQuery,
  levelsQuery,
  MAX_PAGE_SIZE,
} from "../career/catalogApi";
import { useCompetencies } from "../career/MatrixEditor";
import { MatrixErrorAlert } from "../career/MatrixErrorAlert";
import {
  invalidateMatrices,
  type MatrixVersion,
  matrixChangesQuery,
  matrixQuery,
  matrixVersionQuery,
  saveMatrixContent,
} from "../career/matrixApi";
import {
  competenciesText,
  compositionChanges,
  compositionsOf,
  scaleNameOf,
} from "../career/matrixGroups";
import { AddBlockModal } from "./AddBlockModal";
import {
  competencyIdsOf,
  sharedBlockIdsOf,
  withChosenCompetencies,
  withSharedBlocks,
} from "./builderContent";
import type { StepProps } from "./builderStep";

/** Step 2: the shared blocks and competencies of the matrix, chosen from the catalogues or created as drafts on the spot. */
export function BuilderCompetenciesStep(props: StepProps) {
  const { base, noteSaveFailure } = useEditBase(props.version);
  const [highlightedBlockId, setHighlightedBlockId] = useState<string>();
  const [leftover, setLeftover] = useState<AttachFailure>();
  return (
    <Flex vertical gap="middle">
      {leftover && (
        <Alert
          type="warning"
          showIcon
          closable
          onClose={() => setLeftover(undefined)}
          title={`Блок «${leftover.block.name}» создан черновиком, но к версии не подключён`}
          description={`${describeApiError(leftover.error)} Подключите его кнопкой «Добавить общий блок».`}
        />
      )}
      <CompetenciesBody
        key={base?.version}
        {...props}
        base={base ?? props.version}
        noteSaveFailure={noteSaveFailure}
        highlightedBlockId={highlightedBlockId}
        onBlockAttached={(id) => {
          setLeftover(undefined);
          setHighlightedBlockId(id);
        }}
        onAttachFailed={(block, error) => setLeftover({ block, error })}
      />
    </Flex>
  );
}

/** A block created in the wizard that the version could not take, kept so the user is told where it stands. */
interface AttachFailure {
  block: CompetencyBlock;
  error: unknown;
}

interface CompetenciesBodyProps extends StepProps {
  base: MatrixVersion;
  noteSaveFailure: (error: unknown) => void;
  /** The block created last, whose row stands out. */
  highlightedBlockId?: string;
  onBlockAttached: (blockId: string) => void;
  onAttachFailed: (block: CompetencyBlock, error: unknown) => void;
}

const FOR_ALL = "Для всех";

/** A shared block of the version as the step lists it, saved or just picked. */
interface BlockLine {
  id: string;
  /** Just picked, so its details are known here and not yet in the version. */
  isPicked?: boolean;
  scopeName?: string | null;
  name: string;
  status: "Draft" | "Active" | "Inactive";
  competencyIds: readonly string[];
}

function sameIds(left: readonly string[], right: readonly string[]) {
  return left.length === right.length && left.every((id, i) => id === right[i]);
}

function CompetenciesBody({
  version,
  base,
  step,
  noteSaveFailure,
  highlightedBlockId,
  onBlockAttached,
  onAttachFailed,
  onBack,
  onSaved,
  onSkip,
  onUnsavedChange,
}: CompetenciesBodyProps) {
  const queryClient = useQueryClient();
  const isEditable = version.actions.includes("Edit");
  const savedIds = competencyIdsOf(base.content);
  const savedBlockIds = sharedBlockIdsOf(base.content);
  const [chosen, setChosen] = useState(savedIds);
  const [blockIds, setBlockIds] = useState(savedBlockIds);
  const [picked, setPicked] = useState<ReadonlyMap<string, CompetencyBlock>>(
    new Map(),
  );
  const [search, setSearch] = useState("");
  const [highlightedId, setHighlightedId] = useState<string>();
  const [isCreating, setIsCreating] = useState(false);
  const [isPickingBlock, setIsPickingBlock] = useState(false);
  const [isCreatingBlock, setIsCreatingBlock] = useState(false);
  const q = useDebouncedValue(search.trim());
  const { entries } = version;
  const list = useQuery(
    competenciesQuery({
      q: q || undefined,
      page: 1,
      pageSize: MAX_PAGE_SIZE,
      directionId: entries.direction?.id,
      withGeneral: true,
      inWork: true,
    }),
  );
  const allowed = useQuery({
    ...competenciesQuery({
      page: 1,
      pageSize: MAX_PAGE_SIZE,
      directionId: entries.direction?.id,
      withGeneral: true,
      inWork: true,
    }),
    enabled: entries.direction !== null,
  });
  const allowedIds =
    allowed.data && allowed.data.total <= MAX_PAGE_SIZE
      ? new Set(allowed.data.items.map((item) => item.id))
      : undefined;
  const matrix = useQuery(matrixQuery(version.matrixId));
  const roles = useProcessRoles();
  const unit = matrix.data && {
    id: matrix.data.orgUnitId,
    name: matrix.data.orgUnitName,
  };
  const canCreateBlock =
    isEditable &&
    unit !== undefined &&
    roles.data?.headedUnits.some((item) => item.id === unit.id) === true;
  const changes = useQuery(matrixChangesQuery(version.id));
  const compositionChange = compositionChanges(changes.data);
  const trackLevels = useQuery({
    ...levelsQuery(entries.track?.id ?? ""),
    enabled: entries.track !== null,
  });

  const compositions = new Map<string, readonly string[]>([
    ...compositionsOf(version),
    ...[...picked].map(
      ([id, block]) => [id, block.competencies.map((item) => item.id)] as const,
    ),
  ]);
  const lines: BlockLine[] = blockIds.flatMap((id) => {
    const entry = entries.sharedBlocks?.find((item) => item.id === id);
    if (entry) {
      return [
        {
          id,
          name: entry.name,
          status: entry.status,
          competencyIds: entry.competencyIds,
        },
      ];
    }
    const block = picked.get(id);
    return block
      ? [
          {
            id,
            name: block.name,
            status: block.status,
            isPicked: true,
            scopeName: block.scopeUnitName,
            competencyIds: block.competencies.map((item) => item.id),
          },
        ]
      : [];
  });
  const brought = new Map(
    lines.flatMap((line) =>
      line.competencyIds.map((id) => [id, line.name] as const),
    ),
  );

  const save = useMutation({
    mutationFn: (request: { step: number; then: "next" | "exit" }) =>
      saveMatrixContent(
        base,
        withSharedBlocks(
          withChosenCompetencies(
            base.content,
            chosen,
            trackLevels.data?.items ?? [],
          ),
          blockIds,
          compositions,
        ),
        Math.max(base.builderStep ?? 0, request.step),
      ),
    onSuccess: (saved, request) => {
      queryClient.setQueryData(matrixVersionQuery(saved.id).queryKey, saved);
      onSaved(request.then);
    },
    onError: noteSaveFailure,
    onSettled: () => invalidateMatrices(queryClient),
  });
  const attach = useMutation({
    mutationFn: (block: CompetencyBlock) =>
      saveMatrixContent(
        base,
        withSharedBlocks(
          withChosenCompetencies(
            base.content,
            chosen,
            trackLevels.data?.items ?? [],
          ),
          [...blockIds, block.id],
          new Map([
            ...compositions,
            [block.id, block.competencies.map((item) => item.id)],
          ]),
        ),
      ),
    onSuccess: (saved, block) => {
      queryClient.setQueryData(matrixVersionQuery(saved.id).queryKey, saved);
      onBlockAttached(block.id);
    },
    onError: (error, block) => {
      noteSaveFailure(error);
      onAttachFailed(block, error);
    },
    onSettled: () => invalidateMatrices(queryClient),
  });
  const isUnsaved =
    isEditable &&
    (save.isPending ||
      attach.isPending ||
      chosen.size !== savedIds.size ||
      [...chosen].some((id) => !savedIds.has(id)) ||
      !sameIds(blockIds, savedBlockIds));
  useEffect(() => onUnsavedChange(isUnsaved), [isUnsaved, onUnsavedChange]);

  function choose(id: string, isChosen: boolean) {
    setChosen((previous) => {
      const next = new Set(previous);
      if (isChosen) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  function reasonOf(block: CompetencyBlock) {
    if (blockIds.includes(block.id)) return "уже в версии";
    const taken = block.competencies.find(
      (item) => chosen.has(item.id) || brought.has(item.id),
    );
    return taken ? `компетенция «${taken.name}» уже в версии` : undefined;
  }

  const columns: TableColumnsType<Competency> = [
    {
      title: "Компетенция",
      key: "name",
      render: (_, competency) =>
        brought.has(competency.id)
          ? `${competency.name} · уже в версии: общий блок «${brought.get(competency.id)}»`
          : competency.name,
    },
    {
      title: "Тип",
      dataIndex: "type",
      render: (type: Competency["type"]) => COMPETENCY_TYPES[type],
    },
    { title: "Шкала", dataIndex: "scaleName" },
    {
      title: "Статус",
      dataIndex: "status",
      render: (status: Competency["status"]) => (
        <CatalogStatusTag status={status} />
      ),
    },
  ];

  return (
    <ConfigProvider componentDisabled={save.isPending || attach.isPending}>
      <Flex vertical gap="middle">
        <Card
          size="small"
          title="Блоки компетенций"
          extra={
            isEditable && (
              <Space>
                <Button onClick={() => setIsPickingBlock(true)}>
                  Добавить общий блок
                </Button>
                {canCreateBlock && (
                  <Button onClick={() => setIsCreatingBlock(true)}>
                    Создать блок
                  </Button>
                )}
              </Space>
            )
          }
        >
          <BlocksTable
            version={version}
            lines={lines}
            isEditable={isEditable}
            highlightedId={highlightedBlockId}
            compositionChange={compositionChange}
            onRemove={(id) =>
              setBlockIds((previous) => previous.filter((item) => item !== id))
            }
          />
          <Typography.Text type="secondary">
            Состав общего блока здесь не правится: он меняется в справочнике
            «Общие блоки». Требования, коэффициент и признак «обязательный»
            блока задаются на следующих шагах.
          </Typography.Text>
        </Card>
        <Flex vertical gap="small">
          <Typography.Title level={4} style={{ margin: 0 }}>
            Компетенции вне блоков
          </Typography.Title>
          <Flex align="center" gap="middle" wrap>
            <Input.Search
              aria-label="Найти компетенцию"
              placeholder="Найти компетенцию"
              allowClear
              style={{ flex: 1 }}
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
              }}
            />
            <Typography.Text
              strong
            >{`Выбрано: ${chosen.size}`}</Typography.Text>
            {isEditable && (
              <Button onClick={() => setIsCreating(true)}>
                Создать компетенцию
              </Button>
            )}
          </Flex>
        </Flex>
        {list.error ? (
          <ErrorAlert
            title="Не удалось загрузить каталог компетенций"
            error={list.error}
            onRetry={() => void list.refetch()}
          />
        ) : (
          <Table<Competency>
            rowKey="id"
            columns={columns}
            dataSource={list.data?.items}
            loading={list.isFetching}
            scroll={{ x: "max-content" }}
            rowClassName={(row) =>
              row.id === highlightedId ? "queue-row-selected" : ""
            }
            rowSelection={{
              selectedRowKeys: [...chosen, ...brought.keys()],
              getCheckboxProps: (row) => ({
                disabled: !isEditable || brought.has(row.id),
              }),
              onSelect: (row, isChosen) => choose(row.id, isChosen),
              onSelectAll: (isChosen, _rows, changed) => {
                for (const row of changed) {
                  if (!brought.has(row.id)) choose(row.id, isChosen);
                }
              },
            }}
            pagination={false}
            locale={{
              emptyText: q
                ? `Компетенция «${q}» не найдена. Измените запрос или создайте компетенцию кнопкой выше.`
                : "Компетенции не найдены. В каталоге нет подходящих компетенций: создайте компетенцию кнопкой выше.",
            }}
          />
        )}
        <TruncationNote
          shown={list.data?.items.length ?? 0}
          total={list.data?.total}
        />
        <Typography.Text type="secondary">
          Черновые компетенции опубликуются вместе с матрицей компетенций.
        </Typography.Text>
        {save.error != null && (
          <MatrixErrorAlert
            title="Не удалось сохранить шаг"
            error={save.error}
          />
        )}
        <BuilderFooter
          step={step}
          onBack={onBack}
          onSkip={onSkip}
          isEditable={isEditable}
          isUnsaved={isUnsaved}
          isSaving={save.isPending}
          nextBlock={
            (chosen.size === 0 && blockIds.length === 0) ||
            (entries.track !== null && !trackLevels.data)
              ? "Выберите хотя бы одну компетенцию или общий блок"
              : undefined
          }
          onSave={(nextStep, then) => save.mutate({ step: nextStep, then })}
          onCancelEdits={() => {
            setChosen(savedIds);
            setBlockIds(savedBlockIds);
          }}
        />
      </Flex>
      {isCreating && (
        <CompetencyModal
          onSaved={(created) => {
            choose(created.id, true);
            setHighlightedId(created.id);
          }}
          onClose={() => setIsCreating(false)}
        />
      )}
      {isCreatingBlock && unit && (
        <BlockModal
          fixedScope={unit}
          onSaved={(created) => attach.mutate(created)}
          onClose={() => setIsCreatingBlock(false)}
        />
      )}
      {isPickingBlock && (
        <AddBlockModal
          entries={entries}
          reasonOf={reasonOf}
          allowedIds={allowedIds}
          availableFor={unit?.id}
          onPick={(block) => {
            setPicked((previous) => new Map(previous).set(block.id, block));
            setBlockIds((previous) => [...previous, block.id]);
            setIsPickingBlock(false);
          }}
          onClose={() => setIsPickingBlock(false)}
        />
      )}
    </ConfigProvider>
  );
}

interface BlocksTableProps {
  version: MatrixVersion;
  lines: readonly BlockLine[];
  isEditable: boolean;
  highlightedId?: string;
  compositionChange: ReturnType<typeof compositionChanges>;
  onRemove: (id: string) => void;
}

/** The shared blocks of the version, each opening to its composition for reading. */
function BlocksTable({
  version,
  lines,
  isEditable,
  highlightedId,
  compositionChange,
  onRemove,
}: BlocksTableProps) {
  const columns: TableColumnsType<BlockLine> = [
    {
      title: "Блок",
      key: "name",
      render: (_, line) => (
        <Space size="small" wrap>
          {line.name}
          {line.status === "Draft" && <CatalogStatusTag status="Draft" />}
        </Space>
      ),
    },
    {
      title: "Действует для",
      key: "scope",
      render: (_, line) => <ScopeCell line={line} />,
    },
    {
      title: "Состав",
      key: "count",
      render: (_, line) => competenciesText(line.competencyIds.length),
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, line) =>
        isEditable && (
          <Button
            type="link"
            size="small"
            danger
            aria-label={`Убрать блок «${line.name}»`}
            onClick={() => onRemove(line.id)}
          >
            Убрать блок
          </Button>
        ),
    },
  ];
  return (
    <Table<BlockLine>
      size="small"
      rowKey="id"
      columns={columns}
      dataSource={[...lines]}
      pagination={false}
      rowClassName={(line) =>
        line.id === highlightedId ? "queue-row-selected" : ""
      }
      locale={{ emptyText: "Общих блоков в версии нет" }}
      expandable={{
        defaultExpandedRowKeys: lines
          .filter((line) => compositionChange.has(line.id))
          .map((line) => line.id),
        expandedRowRender: (line) => (
          <Composition
            version={version}
            line={line}
            change={compositionChange.get(line.id)}
          />
        ),
      }}
    />
  );
}

/** Where the block applies, in words: the unit it is for, or «Для всех». */
function ScopeCell({ line }: { line: BlockLine }) {
  const read = useQuery({ ...blockQuery(line.id), enabled: !line.isPicked });
  return (line.isPicked ? line.scopeName : read.data?.scopeUnitName) ?? FOR_ALL;
}

interface CompositionProps {
  version: MatrixVersion;
  line: BlockLine;
  change?: { added: readonly string[]; removed: readonly string[] };
}

function Composition({ version, line, change }: CompositionProps) {
  const competencies = useCompetencies(version.entries, [
    ...line.competencyIds,
    ...(change?.removed ?? []),
  ]);
  const rows = [...line.competencyIds];
  return (
    <Flex vertical gap="small">
      <Table
        size="small"
        rowKey="id"
        pagination={false}
        dataSource={rows.map((id) => ({ id }))}
        columns={[
          {
            title: "Компетенция",
            key: "name",
            render: (_, { id }) => (
              <Space size="small" wrap>
                {competencies.get(id)?.name}
                {change?.added.includes(id) && (
                  <Tag color="warning">⚠ Добавлена в блок</Tag>
                )}
              </Space>
            ),
          },
          {
            title: "Шкала",
            key: "scale",
            render: (_, { id }) =>
              scaleNameOf(version, id, competencies.get(id)),
          },
          {
            title: "Тип",
            key: "type",
            render: (_, { id }) => {
              const type = competencies.get(id)?.type;
              return type && COMPETENCY_TYPES[type];
            },
          },
        ]}
      />
      {change && change.removed.length > 0 && (
        <Typography.Text type="secondary">
          {`Убрана из блока: ${change.removed.map((id) => `«${competencies.get(id)?.name}»`).join(", ")}`}
        </Typography.Text>
      )}
    </Flex>
  );
}
