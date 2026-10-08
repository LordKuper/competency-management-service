import { useMutation } from "@tanstack/react-query";
import {
  Alert,
  theme as antdTheme,
  Button,
  Flex,
  Form,
  Input,
  Typography,
} from "antd";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { LOGIN_PATH } from "../../app/featureContract";
import { AuthCard, AuthLink } from "./AuthCard";

/** Address of the screen that asks for a password reset link. */
export const FORGOT_PASSWORD_PATH = "/forgot-password";

interface ForgotPasswordValues {
  email: string;
}

/**
 * Asks for a password reset link without a session. Every accepted request ends in the same neutral notice,
 * so the screen never tells whether an account with the address exists.
 */
export function ForgotPasswordPage() {
  const { token } = antdTheme.useToken();
  const [form] = Form.useForm<ForgotPasswordValues>();
  const request = useMutation({
    mutationFn: async (body: ForgotPasswordValues) =>
      unwrap(await api.POST("/api/v1/auth/forgot-password", { body })),
    onError: (error) => showFieldErrors(form, error),
  });

  return (
    <AuthCard title="Восстановление пароля">
      {request.isSuccess ? (
        <Alert
          type="info"
          showIcon
          title="Проверьте почту"
          description="Если учётная запись с этим e-mail существует, на него отправлено письмо со ссылкой для задания нового пароля."
        />
      ) : (
        <>
          <Typography.Paragraph type="secondary">
            Введите e-mail учётной записи: на него придёт письмо со ссылкой для
            задания нового пароля.
          </Typography.Paragraph>
          <Form
            form={form}
            name="forgot-password"
            layout="vertical"
            noValidate
            onFinish={(values) => request.mutate(values)}
          >
            {request.isError && (
              <Form.Item>
                <ErrorAlert
                  title="Не удалось отправить запрос"
                  error={request.error}
                />
              </Form.Item>
            )}
            <Form.Item
              name="email"
              label="E-mail"
              rules={[
                { required: true, whitespace: true, message: "Введите e-mail" },
              ]}
            >
              <Input type="email" autoComplete="username" autoFocus />
            </Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              size="large"
              block
              loading={request.isPending}
            >
              Отправить ссылку
            </Button>
          </Form>
        </>
      )}
      <Flex justify="center" style={{ marginBlockStart: token.margin }}>
        <AuthLink to={LOGIN_PATH}>Вернуться ко входу</AuthLink>
      </Flex>
    </AuthCard>
  );
}
