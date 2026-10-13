import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Button,
  ConfigProvider,
  Flex,
  Input,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import { useEffect, useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { useEditBase } from "../../app/useEditBase";
import { BuilderFooter } from "../career/BuilderFooter";
import { CatalogStatusTag } from "../career/CatalogParts";
import { COMPETENCY_TYPES, CompetencyModal } from "../career/CompetencyModal";
import {
  type Competency,
  competenciesQuery,
  levelsQuery,
} from "../career/catalogApi";
import { MatrixErrorAlert } from "../career/MatrixErrorAlert";
import {
  invalidateMatrices,
  type MatrixVersion,
  matrixVersionQuery,
  saveMatrixContent,
} from "../career/matrixApi";
import { competencyIdsOf, withChosenCompetencies } from "./builderContent";
import type { StepProps } from "./builderStep";

const PAGE_SIZE = 20;

/** Step 2: the competencies of the matrix, chosen from the catalogue or created as drafts on the spot. */
export function BuilderCompetenciesStep(props: StepProps) {
  const { base, noteSaveFailure } = useEditBase(props.version);
  return (
    <CompetenciesBody
      key={base?.version}
      {...props}
      base={base ?? props.version}
      noteSaveFailure={noteSaveFailure}
    />
  );
}

interface CompetenciesBodyProps extends StepProps {
  base: MatrixVersion;
  noteSaveFailure: (error: unknown) => void;
}

function CompetenciesBody({
  version,
  base,
  step,
  noteSaveFailure,
  onBack,
  onSaved,
  onSkip,
  onUnsavedChange,
}: CompetenciesBodyProps) {
  const queryClient = useQueryClient();
  const isEditable = version.actions.includes("Edit");
  const savedIds = competencyIdsOf(base.content);
  const [chosen, setChosen] = useState(savedIds);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [highlightedId, setHighlightedId] = useState<string>();
  const [isCreating, setIsCreating] = useState(false);
  const q = useDebouncedValue(search.trim());
  const list = useQuery(
    competenciesQuery({
      q: q || undefined,
      page,
      pageSize: PAGE_SIZE,
      scaleId: version.scaleId,
      directionId: version.entries.direction.id,
      withGeneral: true,
      inWork: true,
    }),
  );
  const trackLevels = useQuery(levelsQuery(version.entries.track.id));
  const save = useMutation({
    mutationFn: (request: { step: number; then: "next" | "exit" }) =>
      saveMatrixContent(
        base,
        withChosenCompetencies(
          base.content,
          chosen,
          trackLevels.data?.items ?? [],
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
  const isUnsaved =
    isEditable &&
    (save.isPending ||
      chosen.size !== savedIds.size ||
      [...chosen].some((id) => !savedIds.has(id)));
  useEffect(() => onUnsavedChange(isUnsaved), [isUnsaved, onUnsavedChange]);

  function choose(id: string, isChosen: boolean) {
    setChosen((previous) => {
      const next = new Set(previous);
      if (isChosen) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  const columns: TableColumnsType<Competency> = [
    { title: "Компетенция", dataIndex: "name" },
    { title: "Категория", dataIndex: "categoryName" },
    {
      title: "Тип",
      dataIndex: "type",
      render: (type: Competency["type"]) => COMPETENCY_TYPES[type],
    },
    {
      title: "Статус",
      dataIndex: "status",
      render: (status: Competency["status"]) => (
        <CatalogStatusTag status={status} />
      ),
    },
  ];

  return (
    <ConfigProvider componentDisabled={save.isPending}>
      <Flex vertical gap="middle">
        <Flex justify="space-between" align="center" wrap gap="small">
          <Typography.Title level={3} style={{ margin: 0 }}>
            {`Компетенции. Выбрано: ${chosen.size}`}
          </Typography.Title>
          {isEditable && (
            <Button onClick={() => setIsCreating(true)}>
              Создать черновую компетенцию
            </Button>
          )}
        </Flex>
        <Input.Search
          aria-label="Найти компетенцию"
          placeholder="Найти компетенцию"
          allowClear
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
        />
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
              selectedRowKeys: [...chosen],
              getCheckboxProps: () => ({ disabled: !isEditable }),
              onSelect: (row, isChosen) => choose(row.id, isChosen),
              onSelectAll: (isChosen, _rows, changed) => {
                for (const row of changed) choose(row.id, isChosen);
              },
            }}
            pagination={{
              current: page,
              pageSize: PAGE_SIZE,
              total: list.data?.total,
              showSizeChanger: false,
              onChange: setPage,
            }}
            locale={{
              emptyText: q
                ? `Компетенция «${q}» не найдена. Измените запрос или создайте черновую компетенцию кнопкой выше.`
                : "Компетенции не найдены. В каталоге нет компетенций этой шкалы и области: создайте черновую компетенцию кнопкой выше.",
            }}
          />
        )}
        <Alert
          type="info"
          showIcon
          title="Черновая компетенция опубликуется вместе с версией, если согласующий вправе её утвердить."
        />
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
            chosen.size === 0 || !trackLevels.data
              ? "Выберите хотя бы одну компетенцию"
              : undefined
          }
          onSave={(nextStep, then) => save.mutate({ step: nextStep, then })}
          onCancelEdits={() => setChosen(savedIds)}
        />
      </Flex>
      {isCreating && (
        <CompetencyModal
          fixedScale={version.entries.scale}
          onSaved={(created) => {
            choose(created.id, true);
            setHighlightedId(created.id);
          }}
          onClose={() => setIsCreating(false)}
        />
      )}
    </ConfigProvider>
  );
}
