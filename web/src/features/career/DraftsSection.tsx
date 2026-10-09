import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Button,
  Empty,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import {
  approveDraft,
  type CatalogDraft,
  type CatalogKind,
  draftsQuery,
  invalidateCatalogs,
} from "./catalogApi";
import { useCatalogRights } from "./useCatalogRights";

const KIND_LABELS: Record<CatalogKind, string> = {
  Direction: "Направление",
  Track: "Трек",
  Level: "Уровень",
  Category: "Категория",
  Competency: "Компетенция",
  Scale: "Шкала",
};

/** Draft entries waiting for the signed-in HR, each approved from the row. Nothing is shown to a user who is no HR. */
export function DraftsSection() {
  const rights = useCatalogRights();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const { data, error, isFetching, refetch } = useQuery({
    ...draftsQuery,
    enabled: rights.isHr,
  });
  const approve = useMutation({
    mutationFn: approveDraft,
    onError: (failure) => message.error(describeApiError(failure)),
    onSettled: () => invalidateCatalogs(queryClient),
  });

  if (!rights.isHr) return null;

  const columns: TableColumnsType<CatalogDraft> = [
    { title: "Запись", dataIndex: "name" },
    {
      title: "Тип",
      dataIndex: "kind",
      render: (kind: CatalogKind) => KIND_LABELS[kind],
    },
    {
      title: "Подразделение",
      dataIndex: "ownerOrgUnitName",
      render: (name: string | null) => name ?? "Общая",
    },
    { title: "Автор", dataIndex: "authorName" },
    {
      title: "Действия",
      key: "actions",
      render: (_, draft) =>
        rights.keeps(draft.ownerOrgUnitId) && (
          <Button
            type="link"
            size="small"
            disabled={approve.isPending}
            onClick={() => approve.mutate(draft)}
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
