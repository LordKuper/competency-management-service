import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Button,
  Empty,
  Flex,
  Space,
  Table,
  type TableColumnsType,
} from "antd";
import { useState } from "react";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { EntryActions, entryColumns } from "./CatalogParts";
import {
  ALL_ENTRIES,
  type CatalogEntry,
  type CatalogListParams,
  categoriesQuery,
  deleteCategory,
  invalidateCatalogs,
} from "./catalogApi";
import { onFilterChange } from "./columnFilters";
import { NameModal } from "./NameModal";
import { useCatalogRights } from "./useCatalogRights";

/** Dialog of the tab: creating (`"create"`) or editing the category with that id; null when closed. */
type Dialog = "create" | string | null;

/** The competency categories. */
export function CategoriesTab() {
  const rights = useCatalogRights();
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<Dialog>(null);
  const [params, setParams] = useState<CatalogListParams>(ALL_ENTRIES);
  const { data, error, isFetching, refetch } = useQuery(
    categoriesQuery(params),
  );
  const edited = data?.items.find((category) => category.id === dialog);

  function confirmDeletion(category: CatalogEntry) {
    modal.confirm({
      title: `Удалить категорию «${category.name}»?`,
      okText: "Удалить",
      okButtonProps: { danger: true },
      cancelText: "Отмена",
      onOk: () =>
        deleteCategory(category).then(
          () => invalidateCatalogs(queryClient),
          (failure) => {
            message.error(
              failure instanceof ApiError &&
                failure.status === 409 &&
                failure.detail
                ? failure.detail
                : describeApiError(failure),
            );
            void invalidateCatalogs(queryClient);
          },
        ),
    });
  }

  const columns: TableColumnsType<CatalogEntry> = [
    { title: "Название", dataIndex: "name" },
    ...entryColumns<CatalogEntry>(params.status),
    {
      title: "Действия",
      key: "actions",
      render: (_, category) => (
        <EntryActions
          segment="categories"
          entry={category}
          onEdit={() => setDialog(category.id)}
        >
          {rights.isHr && (
            <Button
              type="link"
              size="small"
              danger
              onClick={() => confirmDeletion(category)}
            >
              Удалить
            </Button>
          )}
        </EntryActions>
      ),
    },
  ];

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      {rights.canCreate && (
        <Flex>
          <Button type="primary" onClick={() => setDialog("create")}>
            Добавить категорию
          </Button>
        </Flex>
      )}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить категории"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<CatalogEntry>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Категорий пока нет"
            />
          ),
        }}
      />
      {dialog === "create" && (
        <NameModal
          segment="categories"
          title="Добавить категорию"
          onClose={() => setDialog(null)}
        />
      )}
      {edited && (
        <NameModal
          segment="categories"
          title="Править"
          entry={edited}
          onClose={() => setDialog(null)}
        />
      )}
    </Space>
  );
}
