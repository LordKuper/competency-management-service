import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Breadcrumb,
  Button,
  Card,
  Flex,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { useState } from "react";
import { Link, useParams } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { layout } from "../../app/theme";
import { ResetPasswordModal } from "./ResetPasswordModal";
import { UserForm, type UserInput } from "./UserForm";
import { UserStatusTag } from "./UserStatusTag";
import { useBlockUser } from "./useBlockUser";
import {
  ifMatchOf,
  type UserResponse,
  userQuery,
  usersQueryKey,
} from "./usersApi";

/** Card of one account: its fields for editing, its state, and the block and password-reset actions. */
export function UserCardPage() {
  const { id = "" } = useParams();
  const { data: user, error, isPending, refetch } = useQuery(userQuery(id));
  if (isPending) return <Skeleton active />;
  if (!user) {
    return (
      <ErrorAlert
        title="Не удалось загрузить пользователя"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <UserCard user={user} />;
}

function UserCard({ user }: { user: UserResponse }) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const confirmBlockChange = useBlockUser();
  const [isResetOpen, setIsResetOpen] = useState(false);
  const update = useMutation({
    mutationFn: async ({ userName, role, employeeId }: UserInput) =>
      unwrap(
        await api.PUT("/api/v1/users/{id}", {
          params: {
            path: { id: user.id },
            header: { "If-Match": ifMatchOf(user.version) },
          },
          body: { userName, role, employeeId },
        }),
      ).data,
    onSuccess: () => message.success("Изменения сохранены"),
    onSettled: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Breadcrumb
        items={[
          { title: <Link to="/users">Пользователи</Link> },
          { title: user.userName },
        ]}
      />
      <Flex align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          {user.userName}
        </Typography.Title>
        <UserStatusTag isBlocked={user.isBlocked} />
      </Flex>
      {update.isError && (
        <ErrorAlert
          title="Не удалось сохранить изменения"
          error={update.error}
        />
      )}
      <Card title="Учётная запись" style={{ maxWidth: layout.readingMaxWidth }}>
        <UserForm
          key={user.version}
          initialValues={{
            userName: user.userName,
            role: user.role,
            employeeId: user.employeeId ?? undefined,
          }}
          employee={
            user.employeeId && user.employeeName
              ? { id: user.employeeId, name: user.employeeName }
              : undefined
          }
          submitLabel="Сохранить"
          onSubmit={(input) => update.mutateAsync(input)}
        />
      </Card>
      <Card title="Доступ" style={{ maxWidth: layout.readingMaxWidth }}>
        <Space wrap>
          <Button onClick={() => confirmBlockChange(user)}>
            {user.isBlocked ? "Разблокировать" : "Заблокировать"}
          </Button>
          <Button onClick={() => setIsResetOpen(true)}>Сбросить пароль</Button>
        </Space>
      </Card>
      <ResetPasswordModal
        user={user}
        open={isResetOpen}
        onClose={() => setIsResetOpen(false)}
      />
    </Space>
  );
}
