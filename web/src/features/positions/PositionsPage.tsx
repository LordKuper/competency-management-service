import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  theme as antdTheme,
  Button,
  Col,
  Empty,
  Flex,
  Input,
  Row,
  Segmented,
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
import { ErrorAlert } from "../../app/ErrorAlert";
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

const STATUS_FILTERS = {
  active: { label: "Активные", isActive: true },
  inactive: { label: "Неактивные", isActive: false },
  all: { label: "Все", isActive: undefined },
};

type StatusFilter = keyof typeof STATUS_FILTERS;

const STATUS_OPTIONS = Object.entries(STATUS_FILTERS).map(
  ([value, { label }]) => ({ value: value as StatusFilter, label }),
);

type PositionDialog = { kind: "create" } | { kind: "rename"; id: string };

/**
 * The position catalogue for administrators: search, status filter, paging, creation, renaming, deactivation after
 * showing how many working employees keep the position, and return to work. The dialogs live here, once for all rows.
 */
export function PositionsPage() {
  const { token } = antdTheme.useToken();
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<PositionDialog | null>(null);
  const [status, setStatus] = useState<StatusFilter>("active");
  const [params, setParams] = useState<PositionListParams>({
    isActive: STATUS_FILTERS.active.isActive,
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
      okButtonProps: { danger: true },
      cancelText: "Отмена",
      onOk: () => setActive(position).then(() => undefined, reportFailure),
    });
  }

  const columns: TableColumnsType<Position> = [
    { title: "Название", dataIndex: "name" },
    {
      title: "Статус",
      dataIndex: "isActive",
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
        <Button type="primary" onClick={() => setDialog({ kind: "create" })}>
          Добавить должность
        </Button>
      </Flex>
      <Row gutter={[token.margin, token.margin]} align="middle">
        <Col xs={24} md={12} lg={10}>
          <Input.Search
            allowClear
            aria-label="Найти должность"
            placeholder="Найти должность"
            onSearch={(text) =>
              setParams((previous) => ({
                ...previous,
                q: text.trim() || undefined,
                page: 1,
              }))
            }
          />
        </Col>
        <Col xs={24} md={12} lg={14}>
          <Segmented<StatusFilter>
            options={STATUS_OPTIONS}
            value={status}
            onChange={(next) => {
              setStatus(next);
              setParams((previous) => ({
                ...previous,
                isActive: STATUS_FILTERS[next].isActive,
                page: 1,
              }));
            }}
          />
        </Col>
      </Row>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить должности"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Position>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        scroll={{ x: "max-content" }}
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
