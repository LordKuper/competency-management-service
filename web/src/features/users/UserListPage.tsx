import {
  CheckCircleOutlined,
  EditOutlined,
  MoreOutlined,
  StopOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
  theme as antdTheme,
  Button,
  Col,
  Dropdown,
  Empty,
  Flex,
  Input,
  Row,
  Select,
  Space,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import type { UserRole } from "../../app/featureContract";
import { ROLE_LABEL, ROLE_OPTIONS } from "./roles";
import { UserModal } from "./UserModal";
import { UserStatusTag } from "./UserStatusTag";
import { useBlockUser } from "./useBlockUser";
import {
  type UserListParams,
  type UserResponse,
  userListQuery,
} from "./usersApi";

const DEFAULT_PAGE_SIZE = 20;

const BLOCKED_OPTIONS = [
  { value: false, label: "Активные" },
  { value: true, label: "Заблокированные" },
];

type UserDialog = { kind: "create" } | { kind: "edit"; userId: string };

/** List of accounts for administrators: search, role and state filters, paging, and a row menu to edit, block or unblock. The dialogs live here, once for all rows. */
export function UserListPage() {
  const { token } = antdTheme.useToken();
  const confirmBlockChange = useBlockUser();
  const [dialog, setDialog] = useState<UserDialog | null>(null);
  const [params, setParams] = useState<UserListParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const { data, error, isFetching, refetch } = useQuery(userListQuery(params));
  const closeDialog = () => setDialog(null);

  const columns: TableColumnsType<UserResponse> = [
    { title: "E-mail", dataIndex: "email" },
    {
      title: "Роль",
      dataIndex: "role",
      render: (role: UserRole) => ROLE_LABEL[role],
    },
    {
      title: "Сотрудник",
      dataIndex: "employeeName",
      render: (name: string | null) => name ?? "Не привязан",
    },
    {
      title: "Состояние",
      dataIndex: "isBlocked",
      render: (isBlocked: boolean) => <UserStatusTag isBlocked={isBlocked} />,
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, user) => (
        <Dropdown
          trigger={["click"]}
          menu={{
            items: [
              {
                key: "edit",
                icon: <EditOutlined />,
                label: "Править",
                onClick: () => setDialog({ kind: "edit", userId: user.id }),
              },
              {
                key: "toggleBlock",
                icon: user.isBlocked ? (
                  <CheckCircleOutlined />
                ) : (
                  <StopOutlined />
                ),
                danger: !user.isBlocked,
                label: user.isBlocked ? "Разблокировать" : "Заблокировать",
                onClick: () => confirmBlockChange(user),
              },
            ],
          }}
        >
          <Button
            type="text"
            icon={<MoreOutlined />}
            aria-label={`Действия с пользователем ${user.email}`}
          />
        </Dropdown>
      ),
    },
  ];

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Пользователи
        </Typography.Title>
        <Button type="primary" onClick={() => setDialog({ kind: "create" })}>
          Создать пользователя
        </Button>
      </Flex>
      <Row gutter={[token.margin, token.margin]}>
        <Col xs={24} md={8} lg={10}>
          <Input.Search
            allowClear
            aria-label="Поиск по e-mail"
            placeholder="Поиск по e-mail"
            onSearch={(text) =>
              setParams((previous) => ({
                ...previous,
                q: text.trim() || undefined,
                page: 1,
              }))
            }
          />
        </Col>
        <Col xs={24} sm={12} md={8} lg={6}>
          <Select
            allowClear
            aria-label="Фильтр по роли"
            placeholder="Роль"
            options={ROLE_OPTIONS}
            style={{ width: "100%" }}
            onChange={(role?: UserRole) =>
              setParams((previous) => ({ ...previous, role, page: 1 }))
            }
          />
        </Col>
        <Col xs={24} sm={12} md={8} lg={5}>
          <Select
            allowClear
            aria-label="Фильтр по состоянию"
            placeholder="Состояние"
            options={BLOCKED_OPTIONS}
            style={{ width: "100%" }}
            onChange={(isBlocked?: boolean) =>
              setParams((previous) => ({ ...previous, isBlocked, page: 1 }))
            }
          />
        </Col>
      </Row>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить пользователей"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<UserResponse>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        scroll={{ x: "max-content" }}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Пользователи не найдены. Измените условия поиска или создайте нового пользователя."
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
      {dialog?.kind === "create" && <UserModal onClose={closeDialog} />}
      {dialog?.kind === "edit" && (
        <UserModal userId={dialog.userId} onClose={closeDialog} />
      )}
    </Space>
  );
}
