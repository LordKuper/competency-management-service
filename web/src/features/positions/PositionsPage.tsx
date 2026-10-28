import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  theme as antdTheme,
  Button,
  Empty,
  Flex,
  Input,
  Space,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import { useState } from "react";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import { ErrorAlert } from "../../app/ErrorAlert";
import { choiceFilter, onFilterChange } from "../career/columnFilters";
import { PositionNameModal } from "./PositionNameModal";
import { PositionStatusTag } from "./PositionStatusTag";
import {
  fetchPositionImpact,
  invalidatePositions,
  type Position,
  type PositionListParams,
  positionListQuery,
} from "./positionsApi";

const DEFAULT_PAGE_SIZE = 20;

const STATUS_OPTIONS = [
  { text: "Активна", value: true },
  { text: "Неактивна", value: false },
];

type PositionDialog = { kind: "create" } | { kind: "rename"; id: string };

/**
 * The position catalogue for administrators and HR: search, paging, the status filter in the column header, creation, renaming, deactivation after
 * showing how many working employees keep the position, and return to work. The dialogs live here, once for all rows.
 */
export function PositionsPage() {
  const { token } = antdTheme.useToken();
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<PositionDialog | null>(null);
  const [params, setParams] = useState<PositionListParams>({
    isActive: true,
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const { data, error, isFetching, refetch } = useQuery(
    positionListQuery(params),
  );
  const { mutateAsync: setActive } = useMutation({
    mutationFn: async (position: Position) => {
      const options = {
        params: {
          path: { id: position.id },
          header: { "If-Match": ifMatchOf(position.version) },
        },
      };
      return unwrap(
        position.isActive
          ? await api.POST("/api/v1/positions/{id}/deactivate", options)
          : await api.POST("/api/v1/positions/{id}/activate", options),
      );
    },
    onSettled: () => invalidatePositions(queryClient),
  });
  const closeDialog = () => setDialog(null);
  const reportFailure = (failure: unknown) => {
    message.error(describeApiError(failure));
  };

  async function confirmDeactivation(position: Position) {
    let employeeCount: number;
    try {
      ({ employeeCount } = await fetchPositionImpact(position.id));
    } catch (failure) {
      reportFailure(failure);
      return;
    }
    modal.confirm({
      title: "Деактивировать должность?",
      content: `Сотрудники, которые её занимают (${employeeCount}), сохранят должность. Выбрать её для других сотрудников будет нельзя.`,
      okText: "Деактивировать",
      ...destructiveConfirm(true),
      cancelText: "Отмена",
      onOk: () => setActive(position).then(() => undefined, reportFailure),
    });
  }

  const columns: TableColumnsType<Position> = [
    { title: "Название", dataIndex: "name" },
    {
      title: "Статус",
      dataIndex: "isActive",
      ...choiceFilter("isActive", params.isActive, STATUS_OPTIONS),
      render: (isActive: boolean) => <PositionStatusTag isActive={isActive} />,
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, position) => (
        <Space size={0} wrap>
          <Button
            type="link"
            size="small"
            onClick={() => setDialog({ kind: "rename", id: position.id })}
          >
            Переименовать
          </Button>
          {position.isActive ? (
            <Button
              type="link"
              size="small"
              danger
              onClick={() => void confirmDeactivation(position)}
            >
              Деактивировать
            </Button>
          ) : (
            <Button
              type="link"
              size="small"
              onClick={() => void setActive(position).catch(reportFailure)}
            >
              Вернуть в работу
            </Button>
          )}
        </Space>
      ),
    },
  ];

  const renamed =
    dialog?.kind === "rename"
      ? data?.items.find((position) => position.id === dialog.id)
      : undefined;

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Должности
        </Typography.Title>
        <Button
          type="primary"
          data-tour="positions-add"
          onClick={() => setDialog({ kind: "create" })}
        >
          Добавить должность
        </Button>
      </Flex>
      <Input.Search
        allowClear
        aria-label="Найти должность"
        data-tour="positions-search"
        placeholder="Найти должность"
        style={{ maxWidth: token.controlHeight * 10 }}
        onSearch={(text) =>
          setParams((previous) => ({
            ...previous,
            q: text.trim() || undefined,
            page: 1,
          }))
        }
      />
      {error && (
        <ErrorAlert
          title="Не удалось загрузить должности"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Position>
        data-tour="positions-table"
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
              description="Должностей пока нет"
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
      {dialog?.kind === "create" && <PositionNameModal onClose={closeDialog} />}
      {renamed && (
        <PositionNameModal position={renamed} onClose={closeDialog} />
      )}
    </Space>
  );
}
