import { useQuery } from "@tanstack/react-query";
import {
  theme as antdTheme,
  Button,
  DatePicker,
  Empty,
  Form,
  Input,
  Space,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import type { Dayjs } from "dayjs";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import {
  type AuditEventResponse,
  type AuditFilters,
  auditListQuery,
} from "./auditApi";

const DEFAULT_PAGE_SIZE = 50;

interface FilterFormValues {
  period?: [Dayjs | null, Dayjs | null] | null;
  actor?: string;
  action?: string;
  entityType?: string;
  entityId?: string;
  requestId?: string;
}

const TIMESTAMP_FORMAT = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeStyle: "medium",
});

function toFilters(values: FilterFormValues): AuditFilters {
  const text = (value?: string) => value?.trim() || undefined;
  return {
    from: values.period?.[0]?.toISOString(),
    to: values.period?.[1]?.toISOString(),
    actor: text(values.actor),
    action: text(values.action),
    entityType: text(values.entityType),
    entityId: text(values.entityId),
    requestId: text(values.requestId),
  };
}

function JsonValue({ label, value }: { label: string; value: unknown }) {
  const { token } = antdTheme.useToken();
  return (
    <div>
      <Typography.Text strong>{label}</Typography.Text>
      <pre
        style={{
          margin: 0,
          padding: token.paddingXS,
          background: token.colorFillTertiary,
          borderRadius: token.borderRadius,
          overflow: "auto",
        }}
      >
        {value == null ? "—" : JSON.stringify(value, null, 2)}
      </pre>
    </div>
  );
}

const COLUMNS: TableColumnsType<AuditEventResponse> = [
  {
    title: "Время",
    dataIndex: "timestamp",
    render: (timestamp: string) => TIMESTAMP_FORMAT.format(new Date(timestamp)),
  },
  { title: "Актор", dataIndex: "actor" },
  { title: "Роль", dataIndex: "role" },
  { title: "Действие", dataIndex: "action" },
  { title: "Сущность", dataIndex: "entityType" },
  { title: "Идентификатор", dataIndex: "entityId" },
  { title: "Запрос", dataIndex: "requestId" },
  { title: "Причина", dataIndex: "reason" },
];

/** Read-only journal of audit events for administrators: filters, server-side paging, and old and new values on row expansion. */
export function AuditPage() {
  const { token } = antdTheme.useToken();
  const [filters, setFilters] = useState<AuditFilters>({});
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const { data, error, isFetching, refetch } = useQuery(
    auditListQuery(filters, page, pageSize),
  );

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Typography.Title level={1} style={{ margin: 0 }}>
        Журнал аудита
      </Typography.Title>
      <Form<FilterFormValues>
        layout="inline"
        style={{ rowGap: token.marginXS }}
        onFinish={(values) => {
          setFilters(toFilters(values));
          setPage(1);
        }}
        onReset={() => {
          setFilters({});
          setPage(1);
        }}
      >
        <Form.Item name="period" label="Период">
          <DatePicker.RangePicker showTime />
        </Form.Item>
        <Form.Item name="actor" label="Актор">
          <Input allowClear />
        </Form.Item>
        <Form.Item name="action" label="Действие">
          <Input allowClear />
        </Form.Item>
        <Form.Item name="entityType" label="Сущность">
          <Input allowClear />
        </Form.Item>
        <Form.Item name="entityId" label="Идентификатор сущности">
          <Input allowClear />
        </Form.Item>
        <Form.Item name="requestId" label="Идентификатор запроса">
          <Input allowClear />
        </Form.Item>
        <Form.Item>
          <Space>
            <Button type="primary" htmlType="submit">
              Применить
            </Button>
            <Button htmlType="reset">Сбросить</Button>
          </Space>
        </Form.Item>
      </Form>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить журнал"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<AuditEventResponse>
        rowKey="id"
        columns={COLUMNS}
        dataSource={data?.items}
        loading={isFetching}
        scroll={{ x: "max-content" }}
        expandable={{
          expandedRowRender: (event) => (
            <Space orientation="vertical" style={{ display: "flex" }}>
              <JsonValue label="Старое значение" value={event.oldValue} />
              <JsonValue label="Новое значение" value={event.newValue} />
            </Space>
          ),
        }}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="События не найдены. Измените условия фильтра."
            />
          ),
        }}
        pagination={{
          current: page,
          pageSize,
          total: data?.total ?? 0,
          pageSizeOptions: [20, 50, 100],
          showSizeChanger: true,
          onChange: (nextPage, nextSize) => {
            setPage(nextPage);
            setPageSize(nextSize);
          },
        }}
      />
    </Space>
  );
}
