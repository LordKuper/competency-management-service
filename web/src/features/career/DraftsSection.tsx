import { useQuery } from "@tanstack/react-query";
import { Button, Empty, Table, type TableColumnsType, Typography } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import {
  type CatalogDraft,
  type CatalogKind,
  type DraftListParams,
  draftsQuery,
  SEGMENT_OF_KIND,
} from "./catalogApi";
import { choiceFilter, onFilterChange } from "./columnFilters";
import { unitFilter } from "./UnitTreeFilter";
import { useCatalogRights } from "./useCatalogRights";
import { usePublishEntry } from "./usePublishEntry";

/** What each kind of draft entry is called. */
export const KIND_LABELS: Record<CatalogKind, string> = {
  Direction: "Направление",
  Track: "Трек",
  Category: "Категория",
  Competency: "Компетенция",
  Scale: "Шкала",
};

/** Draft entries waiting for the signed-in HR, each approved from the row. Nothing is shown to a user who is no HR. */
export function DraftsSection() {
  const rights = useCatalogRights();
  const [params, setParams] = useState<DraftListParams>({});
  const { data, error, isFetching, refetch } = useQuery({
    ...draftsQuery(params),
    enabled: rights.isHr,
  });
  const { publish, isBusy } = usePublishEntry();

  if (!rights.isHr) return null;

  const columns: TableColumnsType<CatalogDraft> = [
    { title: "Запись", dataIndex: "name" },
    {
      title: "Тип",
      dataIndex: "kind",
      ...choiceFilter(
        "kind",
        params.kind,
        Object.entries(KIND_LABELS).map(([value, text]) => ({ text, value })),
      ),
      render: (kind: CatalogKind) => KIND_LABELS[kind],
    },
    {
      title: "Подразделение",
      dataIndex: "ownerOrgUnitName",
      ...unitFilter("orgUnitId", params.orgUnitId),
      render: (name: string | null) => name ?? "Общая",
    },
    { title: "Автор", dataIndex: "authorName" },
    {
      title: "Действия",
      key: "actions",
      render: (_, draft) =>
        rights.canManage(draft.ownerOrgUnitId ?? undefined) && (
          <Button
            type="link"
            size="small"
            disabled={isBusy}
            onClick={() =>
              void publish(
                {
                  segment: SEGMENT_OF_KIND[draft.kind],
                  entry: draft,
                },
                "approve",
              )
            }
          >
            Согласовать
          </Button>
        ),
    },
  ];

  return (
    <section>
      <Typography.Title level={2}>Черновые записи</Typography.Title>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить черновые записи"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<CatalogDraft>
        rowKey={(draft) => `${draft.kind}:${draft.id}`}
        columns={columns}
        dataSource={data}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Черновых записей нет"
            />
          ),
        }}
      />
    </section>
  );
}
