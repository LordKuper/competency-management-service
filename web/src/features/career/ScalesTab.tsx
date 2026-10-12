import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Button,
  Empty,
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
  FIRST_PAGE,
  invalidateCatalogs,
  makeScaleDefault,
  type Scale,
  type ScaleListParams,
  type ScalePoint,
  scalesQuery,
} from "./catalogApi";
import { choiceFilter, onFilterChange, pageProps } from "./columnFilters";
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
  const [params, setParams] = useState<ScaleListParams>(FIRST_PAGE);
  const { data, error, isFetching, refetch } = useQuery({
    ...scalesQuery(params),
    placeholderData: (previous) => previous,
  });
  const edited = data?.items.find((scale) => scale.id === dialog);
  const columns: TableColumnsType<Scale> = [
    {
      title: "Название",
      dataIndex: "name",
    },
    {
      title: "По умолчанию",
      dataIndex: "isDefault",
      ...choiceFilter("isDefault", params.isDefault, [
        { text: "По умолчанию", value: true },
      ]),
      render: (isDefault: boolean) =>
        isDefault && <Tag color="processing">По умолчанию</Tag>,
    },
    ...entryColumns<Scale>(params.status),
    {
      title: "Действия",
      key: "actions",
      render: (_, scale) => (
        <EntryActions
          segment="scales"
          entry={scale}
          onEdit={() => setDialog(scale.id)}
          extra={
            rights.isHr && scale.status === "Active" && !scale.isDefault
              ? [
                  {
                    key: "makeDefault",
                    label: "Сделать по умолчанию",
                    disabled: makeDefault.isPending,
                    onClick: () => makeDefault.mutate(scale),
                  },
                ]
              : []
          }
        />
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
        pagination={pageProps(params, data?.total, setParams)}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Шкал пока нет"
            />
          ),
        }}
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
