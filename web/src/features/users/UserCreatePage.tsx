import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Card, Space, Typography } from "antd";
import { Link, useNavigate } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { layout } from "../../app/theme";
import { UserForm, type UserInput } from "./UserForm";
import { usersQueryKey } from "./usersApi";

/** Page on which an administrator creates an account; success opens the new account's card. */
export function UserCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const create = useMutation({
    mutationFn: async ({ password, ...account }: UserInput) =>
      unwrap(
        await api.POST("/api/v1/users", {
          body: { ...account, password: password ?? "" },
        }),
      ).data,
    onSuccess: async (user) => {
      await queryClient.invalidateQueries({ queryKey: usersQueryKey });
      message.success("Пользователь создан");
      void navigate(`/users/${user.id}`);
    },
  });

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Breadcrumb
        items={[
          { title: <Link to="/users">Пользователи</Link> },
          { title: "Новый пользователь" },
        ]}
      />
      <Typography.Title level={1}>Новый пользователь</Typography.Title>
      {create.isError && (
        <ErrorAlert
          title="Не удалось создать пользователя"
          error={create.error}
        />
      )}
      <Card style={{ maxWidth: layout.readingMaxWidth }}>
        <UserForm
          isNew
          submitLabel="Создать"
          onSubmit={(input) => create.mutateAsync(input)}
        />
      </Card>
    </Space>
  );
}
