import { useQuery } from "@tanstack/react-query";
import {
  Button,
  Descriptions,
  Empty,
  Flex,
  Input,
  Select,
  Skeleton,
  Space,
  Table,
  type TableColumnsType,
} from "antd";
import { useState } from "react";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { CatalogStatusTag, EntryActions, entryColumns } from "./CatalogParts";
import { catalogFilter } from "./CatalogSelect";
import { COMPETENCY_TYPES, CompetencyModal } from "./CompetencyModal";
import {
  type Competency,
  type CompetencyListParams,
  type CompetencyVersion,
  competenciesQuery,
  competencyVersionsQuery,
  entryQuery,
  FIRST_PAGE,
} from "./catalogApi";
import { choiceFilter, onFilterChange, pageProps } from "./columnFilters";
import { useCatalogRights } from "./useCatalogRights";

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

/** The competency catalogue: search, paging, creation, editing, the status actions and the version history. */
export function CompetenciesTab() {
  const rights = useCatalogRights();
  const [dialog, setDialog] = useState<Dialog | null>(null);
  const [params, setParams] = useState<CompetencyListParams>(FIRST_PAGE);
  const { data, error, isFetching, refetch } = useQuery({
    ...competenciesQuery(params),
    placeholderData: (previous) => previous,
  });
  const columns: TableColumnsType<Competency> = [
    { title: "Название", dataIndex: "name" },
    {
      title: "Категория",
      dataIndex: "categoryId",
      ...catalogFilter(
        "categoryId",
        "categories",
        params.categoryId,
        "Категория",
      ),
      render: (_, competency) => competency.categoryName,
    },
    {
      title: "Направление",
      dataIndex: "directionId",
      ...catalogFilter(
        "directionId",
        "directions",
        params.directionId,
        "Направление",
      ),
      render: (_, competency) => competency.directionName ?? "Общая",
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
      ...catalogFilter("scaleId", "scales", params.scaleId, "Шкала"),
      render: (_, competency) => competency.scaleName,
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
        pagination={pageProps(params, data?.total, setParams)}
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

/** The history of a competency: its versions, newest first, and the fields of the chosen one as it was saved. */
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
  const [number, setNumber] = useState<number>();
  const chosen =
    data?.find((version) => version.number === number) ?? data?.[0];
  const category = useQuery({
    ...entryQuery("categories", chosen?.categoryId ?? ""),
    enabled: chosen !== undefined,
  });
  const direction = useQuery({
    ...entryQuery("directions", chosen?.directionId ?? ""),
    enabled: !!chosen?.directionId,
  });
  const scale = useQuery({
    ...entryQuery("scales", chosen?.scaleId ?? ""),
    enabled: chosen !== undefined,
  });

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
      {chosen && (
        <Space orientation="vertical" style={{ display: "flex" }}>
          <Select
            aria-label="Версия"
            value={chosen.number}
            options={data?.map((version) => ({
              value: version.number,
              label: versionLine(version),
            }))}
            onChange={setNumber}
          />
          <Descriptions
            column={1}
            items={[
              { key: "name", label: "Название", children: chosen.name },
              {
                key: "description",
                label: "Описание",
                children: chosen.description,
              },
              {
                key: "category",
                label: "Категория",
                children: category.data?.name,
              },
              {
                key: "direction",
                label: "Направление",
                children: chosen.directionId ? direction.data?.name : "Общая",
              },
              {
                key: "type",
                label: "Тип",
                children: COMPETENCY_TYPES[chosen.type],
              },
              { key: "scale", label: "Шкала", children: scale.data?.name },
              {
                key: "status",
                label: "Статус",
                children: <CatalogStatusTag status={chosen.status} />,
              },
            ]}
          />
        </Space>
      )}
    </AppModal>
  );
}
