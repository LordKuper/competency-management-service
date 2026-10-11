import { useQuery } from "@tanstack/react-query";
import {
  Button,
  Empty,
  Flex,
  Input,
  Skeleton,
  Space,
  Table,
  type TableColumnsType,
} from "antd";
import { useState } from "react";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { EntryActions, entryColumns } from "./CatalogParts";
import { COMPETENCY_TYPES, CompetencyModal } from "./CompetencyModal";
import {
  type Competency,
  type CompetencyListParams,
  type CompetencyVersion,
  categoriesQuery,
  competenciesQuery,
  competencyVersionsQuery,
  directionsQuery,
  scalesQuery,
} from "./catalogApi";
import { choiceFilter, entryOptions, onFilterChange } from "./columnFilters";
import { useCatalogRights } from "./useCatalogRights";

const DEFAULT_PAGE_SIZE = 20;
const SEARCH_LABEL = "Найти компетенцию";
const DATE_TIME = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeStyle: "short",
});

/** Dialog of the tab: create, edit the competency with that id, or show the history of the one with that id. */
type Dialog =
  | { kind: "create" }
  | { kind: "edit"; id: string }
  | { kind: "versions"; id: string };

function namesById(items: readonly { id: string; name: string }[] | undefined) {
  return new Map(items?.map((item) => [item.id, item.name]));
}

/** The competency catalogue: search, paging, creation, editing, the status actions and the version history. */
export function CompetenciesTab() {
  const rights = useCatalogRights();
  const [dialog, setDialog] = useState<Dialog | null>(null);
  const [params, setParams] = useState<CompetencyListParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const { data, error, isFetching, refetch } = useQuery({
    ...competenciesQuery(params),
    placeholderData: (previous) => previous,
  });
  const categoryItems = useQuery(categoriesQuery()).data?.items;
  const directionItems = useQuery(directionsQuery()).data?.items;
  const scaleItems = useQuery(scalesQuery()).data?.items;
  const categories = namesById(categoryItems);
  const directions = namesById(directionItems);
  const scales = namesById(scaleItems);
  const columns: TableColumnsType<Competency> = [
    { title: "Название", dataIndex: "name" },
    {
      title: "Категория",
      dataIndex: "categoryId",
      ...choiceFilter(
        "categoryId",
        params.categoryId,
        entryOptions(categoryItems),
        true,
      ),
      render: (id: string) => categories.get(id),
    },
    {
      title: "Направление",
      dataIndex: "directionId",
      ...choiceFilter(
        "directionId",
        params.directionId,
        entryOptions(directionItems),
        true,
      ),
      render: (id: string | null) => (id ? directions.get(id) : "Общая"),
    },
    {
      title: "Тип",
      dataIndex: "type",
      ...choiceFilter(
        "type",
        params.type,
        Object.entries(COMPETENCY_TYPES).map(([value, text]) => ({
          text,
          value,
        })),
      ),
      render: (type) => COMPETENCY_TYPES[type as Competency["type"]],
    },
    {
      title: "Шкала",
      dataIndex: "scaleId",
      ...choiceFilter(
        "scaleId",
        params.scaleId,
        entryOptions(scaleItems),
        true,
      ),
      render: (id: string) => scales.get(id),
    },
    ...entryColumns<Competency>(params.status),
    { title: "Версия", dataIndex: "versionNumber" },
    {
      title: "Действия",
      key: "actions",
      render: (_, competency) => (
        <EntryActions
          segment="competencies"
          entry={competency}
          onEdit={() => setDialog({ kind: "edit", id: competency.id })}
        >
          <Button
            type="link"
            size="small"
            onClick={() => setDialog({ kind: "versions", id: competency.id })}
          >
            История версий
          </Button>
        </EntryActions>
      ),
    },
  ];
  const close = () => setDialog(null);
  const edited =
    dialog?.kind === "edit"
      ? data?.items.find((competency) => competency.id === dialog.id)
      : undefined;

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Input.Search
          allowClear
          aria-label={SEARCH_LABEL}
          placeholder={SEARCH_LABEL}
          onSearch={(text) =>
            setParams((previous) => ({
              ...previous,
              q: text.trim() || undefined,
              page: 1,
            }))
          }
        />
        {rights.canCreate && (
          <Button type="primary" onClick={() => setDialog({ kind: "create" })}>
            Добавить компетенцию
          </Button>
        )}
      </Flex>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить компетенции"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Competency>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Компетенций пока нет"
            />
          ),
        }}
        pagination={{
          current: params.page,
          pageSize: params.pageSize,
          total: data?.total ?? 0,
          showSizeChanger: true,
          onChange: (page, pageSize) =>
            setParams((previous) => ({ ...previous, page, pageSize })),
        }}
      />
      {dialog?.kind === "create" && <CompetencyModal onClose={close} />}
      {edited && <CompetencyModal competency={edited} onClose={close} />}
      {dialog?.kind === "versions" && (
        <VersionsModal competencyId={dialog.id} onClose={close} />
      )}
    </Space>
  );
}

function versionLine(version: CompetencyVersion) {
  const who = version.changedByName ? `, ${version.changedByName}` : "";
  return `Версия ${version.number} — ${DATE_TIME.format(new Date(version.changedAt))}${who}`;
}

function VersionsModal({
  competencyId,
  onClose,
}: {
  competencyId: string;
  onClose: () => void;
}) {
  const { data, error, isPending, refetch } = useQuery(
    competencyVersionsQuery(competencyId),
  );

  return (
    <AppModal open title="История версий" onCancel={onClose} footer={null}>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить историю версий"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {isPending && !error && <Skeleton active />}
      <ul>
        {data?.map((version) => (
          <li key={version.number}>{versionLine(version)}</li>
        ))}
      </ul>
    </AppModal>
  );
}
