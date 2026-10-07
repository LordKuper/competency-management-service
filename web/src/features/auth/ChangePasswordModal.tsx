import { useMutation } from "@tanstack/react-query";
import { App, Button, Form, Input, Modal, Space } from "antd";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { PASSWORD_HINT } from "./passwordPolicy";

interface ChangePasswordValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

interface ChangePasswordModalProps {
  open: boolean;
  onClose: () => void;
}

/** Dialog in which the signed-in user replaces their own password; the form is rebuilt on every opening. */
export function ChangePasswordModal({
  open,
  onClose,
}: ChangePasswordModalProps) {
  return (
    <Modal
      title="Смена пароля"
      open={open}
      onCancel={onClose}
      footer={null}
      destroyOnHidden
    >
      <ChangePasswordForm onDone={onClose} />
    </Modal>
  );
}

function ChangePasswordForm({ onDone }: { onDone: () => void }) {
  const [form] = Form.useForm<ChangePasswordValues>();
  const { message } = App.useApp();
  const change = useMutation({
    mutationFn: async ({
      currentPassword,
      newPassword,
    }: ChangePasswordValues) =>
      unwrap(
        await api.POST("/api/v1/auth/change-password", {
          body: { currentPassword, newPassword },
        }),
      ),
    onSuccess: () => {
      message.success("Пароль изменён");
      onDone();
    },
    onError: (error) => showFieldErrors(form, error),
  });

  return (
    <Form
      form={form}
      name="change-password"
      layout="vertical"
      onFinish={(values) => change.mutate(values)}
    >
      {change.isError && (
        <Form.Item>
          <ErrorAlert title="Не удалось изменить пароль" error={change.error} />
        </Form.Item>
      )}
      <Form.Item
        name="currentPassword"
        label="Текущий пароль"
        rules={[{ required: true, message: "Введите текущий пароль" }]}
      >
        <Input.Password autoComplete="current-password" autoFocus />
      </Form.Item>
      <Form.Item
        name="newPassword"
        label="Новый пароль"
        extra={PASSWORD_HINT}
        rules={[{ required: true, message: "Введите новый пароль" }]}
      >
        <Input.Password autoComplete="new-password" />
      </Form.Item>
      <Form.Item
        name="confirmPassword"
        label="Повторите новый пароль"
        dependencies={["newPassword"]}
        rules={[
          { required: true, message: "Повторите новый пароль" },
          ({ getFieldValue }) => ({
            validator: (_, value) =>
              !value || getFieldValue("newPassword") === value
                ? Promise.resolve()
                : Promise.reject(new Error("Пароли не совпадают")),
          }),
        ]}
      >
        <Input.Password autoComplete="new-password" />
      </Form.Item>
      <Space>
        <Button type="primary" htmlType="submit" loading={change.isPending}>
          Изменить пароль
        </Button>
        <Button onClick={onDone}>Отмена</Button>
      </Space>
    </Form>
  );
}
