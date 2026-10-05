import { useQuery } from "@tanstack/react-query";
import {
  theme as antdTheme,
  Button,
  Col,
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
import { Link, useNavigate } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import type { UserRole } from "../../app/featureContract";
import { ROLE_LABEL, ROLE_OPTIONS } from "./roles";
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

/** List of accounts for administrators: search, role and state filters, paging, and block or unblock from a row. */
export function UserListPage() {
  const { token } = antdTheme.useToken();
  const navigate = useNavigate();
  const confirmBlockChange = useBlockUser();
  const [params, setParams] = useState<UserListParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const { data, error, isFetching, refetch } = useQuery(userListQuery(params));

  const columns: TableColumnsType<UserResponse> = [
    {
      title: "Имя пользователя",
      dataIndex: "userName",
      render: (userName: string, user) => (
        <Link to={`/users/${user.id}`}>{userName}</Link>
      ),
    },
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
      render: (_, user) => {
        const action = user.isBlocked ? "Разблокировать" : "Заблокировать";
        return (
          <Button
            type="link"
            aria-label={`${action} ${user.userName}`}
            onClick={() => confirmBlockChange(user)}
          >
            {action}
          </Button>
        );
      },
    },
  ];

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Пользователи
        </Typography.Title>
        <Button type="primary" onClick={() => navigate("/users/new")}>
          Создать пользователя
        </Button>
      </Flex>
      <Row gutter={[token.margin, token.margin]}>
        <Col xs={24} md={12} lg={10}>
          <Input.Search
            allowClear
            aria-label="Поиск по имени пользователя"
            placeholder="Поиск по имени пользователя"
            onSearch={(text) =>
              setParams((previous) => ({
                ...previous,
                q: text.trim() || undefined,
                page: 1,
              }))
            }
          />
        </Col>
        <Col xs={12} md={6} lg={4}>
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
        <Col xs={12} md={6} lg={4}>
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
    </Space>
  );
}
