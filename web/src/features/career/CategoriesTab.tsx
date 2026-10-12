import { useQuery } from "@tanstack/react-query";
import { Button, Empty, Flex, Space, Table, type TableColumnsType } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { EntryActions, entryColumns } from "./CatalogParts";
import {
  type CatalogEntry,
  type CatalogListParams,
  categoriesQuery,
  FIRST_PAGE,
} from "./catalogApi";
import { onFilterChange, pageProps } from "./columnFilters";
import { NameModal } from "./NameModal";
import { useCatalogRights } from "./useCatalogRights";

/** Dialog of the tab: creating (`"create"`) or editing the category with that id; null when closed. */
type Dialog = "create" | string | null;

/** The competency categories. */
export function CategoriesTab() {
  const rights = useCatalogRights();
  const [dialog, setDialog] = useState<Dialog>(null);
  const [params, setParams] = useState<CatalogListParams>(FIRST_PAGE);
  const { data, error, isFetching, refetch } = useQuery({
    ...categoriesQuery(params),
    placeholderData: (previous) => previous,
  });
  const edited = data?.items.find((category) => category.id === dialog);

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
        />
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
        pagination={pageProps(params, data?.total, setParams)}
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
          title="Новая категория"
          onClose={() => setDialog(null)}
        />
      )}
      {edited && (
        <NameModal
          segment="categories"
          title="Изменение категории"
          entry={edited}
          onClose={() => setDialog(null)}
        />
      )}
    </Space>
  );
}
