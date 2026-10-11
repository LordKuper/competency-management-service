import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Button,
  Flex,
  Space,
  Table,
  type TableColumnsType,
  Tag,
} from "antd";
import { useState } from "react";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { EntryActions, entryColumns } from "./CatalogParts";
import {
  invalidateCatalogs,
  makeScaleDefault,
  type Scale,
  type ScalePoint,
  scalesQuery,
} from "./catalogApi";
import { ScaleModal } from "./ScaleModal";
import { useCatalogRights } from "./useCatalogRights";

/** Dialog of the tab: creating (`"create"`) or editing the scale with that id; null when closed. */
type Dialog = "create" | string | null;

const POINT_COLUMNS: TableColumnsType<ScalePoint> = [
  { title: "Балл", dataIndex: "score" },
  { title: "Значение", dataIndex: "label" },
];

/** The scales, the base one and those added later, each opening to its points. */
export function ScalesTab() {
  const rights = useCatalogRights();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<Dialog>(null);
  const makeDefault = useMutation({
    mutationFn: makeScaleDefault,
    onError: (failure) => message.error(describeApiError(failure)),
    onSettled: () => invalidateCatalogs(queryClient),
  });
  const { data, error, isFetching, refetch } = useQuery(scalesQuery());
  const edited = data?.items.find((scale) => scale.id === dialog);
  const columns: TableColumnsType<Scale> = [
    {
      title: "Название",
      dataIndex: "name",
      render: (name: string, scale) => (
        <>
          {name} {scale.isDefault && <Tag color="processing">По умолчанию</Tag>}
        </>
      ),
    },
    ...entryColumns<Scale>(),
    {
      title: "Действия",
      key: "actions",
      render: (_, scale) => (
        <EntryActions
          segment="scales"
          entry={scale}
          onEdit={() => setDialog(scale.id)}
        >
          {rights.isHr && scale.status === "Active" && !scale.isDefault && (
            <Button
              type="link"
              size="small"
              disabled={makeDefault.isPending}
              onClick={() => makeDefault.mutate(scale)}
            >
              Сделать по умолчанию
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
            Добавить шкалу
          </Button>
        </Flex>
      )}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить шкалы"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Scale>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        expandable={{
          expandedRowRender: (scale) => (
            <Table<ScalePoint>
              rowKey="score"
              columns={POINT_COLUMNS}
              dataSource={[...scale.points]}
              pagination={false}
              size="small"
            />
          ),
        }}
      />
      {dialog === "create" && <ScaleModal onClose={() => setDialog(null)} />}
      {edited && <ScaleModal scale={edited} onClose={() => setDialog(null)} />}
    </Space>
  );
}
