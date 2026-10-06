import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Button, Form, Input, Modal, Space, Typography } from "antd";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { PASSWORD_HINT } from "../auth/passwordPolicy";
import { ifMatchOf, type UserResponse, usersQueryKey } from "./usersApi";

interface ResetPasswordValues {
  newPassword: string;
}

interface ResetPasswordModalProps {
  user: UserResponse;
  open: boolean;
  onClose: () => void;
}

/** Dialog in which an administrator sets a new password for another account; the form is rebuilt on every opening. */
export function ResetPasswordModal({
  user,
  open,
  onClose,
}: ResetPasswordModalProps) {
  return (
    <Modal
      title="Сброс пароля"
      open={open}
      onCancel={onClose}
      footer={null}
      destroyOnHidden
    >
      <ResetPasswordForm user={user} onDone={onClose} />
    </Modal>
  );
}

function ResetPasswordForm({
  user,
  onDone,
}: {
  user: UserResponse;
  onDone: () => void;
}) {
  const [form] = Form.useForm<ResetPasswordValues>();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const reset = useMutation({
    mutationFn: async ({ newPassword }: ResetPasswordValues) =>
      unwrap(
        await api.POST("/api/v1/users/{id}/reset-password", {
          params: {
            path: { id: user.id },
            header: { "If-Match": ifMatchOf(user.version) },
          },
          body: { newPassword },
        }),
      ).data,
    onSuccess: () => {
      message.success("Пароль сброшен");
      onDone();
    },
    onError: (error) => showFieldErrors(form, error),
    onSettled: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });

  return (
    <Form
      form={form}
      name="reset-password"
      layout="vertical"
      onFinish={(values) => reset.mutate(values)}
    >
      <Typography.Paragraph>
        Задайте новый пароль для «{user.email}». Все действующие сессии
        пользователя будут завершены; передайте пароль пользователю безопасным
        способом.
      </Typography.Paragraph>
      {reset.isError && (
        <Form.Item>
          <ErrorAlert title="Не удалось сбросить пароль" error={reset.error} />
        </Form.Item>
      )}
      <Form.Item
        name="newPassword"
        label="Новый пароль"
        extra={PASSWORD_HINT}
        rules={[{ required: true, message: "Введите новый пароль" }]}
      >
        <Input.Password autoComplete="new-password" autoFocus />
      </Form.Item>
      <Space>
        <Button type="primary" htmlType="submit" loading={reset.isPending}>
          Сбросить пароль
        </Button>
        <Button onClick={onDone}>Отмена</Button>
      </Space>
    </Form>
  );
}
