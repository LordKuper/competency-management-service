import {
  CheckCircleOutlined,
  EditOutlined,
  KeyOutlined,
  MailOutlined,
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
import { useSendUserMail } from "./useSendUserMail";

const DEFAULT_PAGE_SIZE = 20;

/** The state filter; an invited account that is also blocked is listed under both of its states, as its tags show. */
const STATE_FILTERS = {
  active: { label: "Активные", isBlocked: false, isInvited: false },
  invited: { label: "Приглашённые", isBlocked: undefined, isInvited: true },
  blocked: { label: "Заблокированные", isBlocked: true, isInvited: undefined },
};

type StateFilter = keyof typeof STATE_FILTERS;

const STATE_OPTIONS = Object.entries(STATE_FILTERS).map(
  ([value, { label }]) => ({
    value,
    label,
  }),
);

type UserDialog = { kind: "create" } | { kind: "edit"; userId: string };

/** List of accounts for administrators: search, role and state filters, paging, and a row menu to edit, block or unblock, and to e-mail an invitation again or a password reset link. The dialogs live here, once for all rows. */
export function UserListPage() {
  const { token } = antdTheme.useToken();
  const confirmBlockChange = useBlockUser();
  const sendMail = useSendUserMail();
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
      key: "state",
      render: (_, user) => (
        <UserStatusTag isBlocked={user.isBlocked} isInvited={user.isInvited} />
      ),
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
              ...(user.isBlocked
                ? []
                : [
                    {
                      key: "sendMail",
                      icon: user.isInvited ? <MailOutlined /> : <KeyOutlined />,
                      label: user.isInvited
                        ? "Отправить приглашение повторно"
                        : "Отправить ссылку для сброса пароля",
                      onClick: () => sendMail(user),
                    },
                  ]),
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
            options={STATE_OPTIONS}
            style={{ width: "100%" }}
            onChange={(state?: StateFilter) => {
              const { isBlocked, isInvited } = state
                ? STATE_FILTERS[state]
                : { isBlocked: undefined, isInvited: undefined };
              setParams((previous) => ({
                ...previous,
                isBlocked,
                isInvited,
                page: 1,
              }));
            }}
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
