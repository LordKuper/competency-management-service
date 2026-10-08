import { useMutation } from "@tanstack/react-query";
import {
  Alert,
  App,
  theme as antdTheme,
  Button,
  Flex,
  Form,
  Input,
  Typography,
} from "antd";
import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { LOGIN_PATH } from "../../app/featureContract";
import { AuthCard, AuthLink } from "./AuthCard";
import { FORGOT_PASSWORD_PATH } from "./ForgotPasswordPage";
import { PASSWORD_HINT } from "./passwordPolicy";

interface LinkPasswordValues {
  password: string;
  confirmPassword: string;
}

interface LinkPasswordPageProps {
  /** The anonymous method that spends the link. */
  path: "/api/v1/auth/accept-invitation" | "/api/v1/auth/reset-password";
  title: string;
  intro: string;
  submitLabel: string;
  /** Shown on the sign-in screen once the password is set. */
  doneMessage: string;
}

/** The server refuses an unusable link with a 400 that names no field; a rejected password comes with field errors. */
function isInvalidLink(error: unknown): error is ApiError {
  return (
    error instanceof ApiError &&
    error.status === 400 &&
    error.errors === undefined
  );
}

/** Screen of the invitation link: the user sets the first password of the account. */
export function AcceptInvitationPage() {
  return (
    <LinkPasswordPage
      path="/api/v1/auth/accept-invitation"
      title="Задание пароля"
      intro="Задайте пароль, чтобы завершить регистрацию."
      submitLabel="Задать пароль"
      doneMessage="Пароль задан. Войдите в систему."
    />
  );
}

/** Screen of the password reset link: the user replaces a forgotten password. */
export function ResetPasswordPage() {
  return (
    <LinkPasswordPage
      path="/api/v1/auth/reset-password"
      title="Новый пароль"
      intro="Задайте новый пароль. Все действующие сессии учётной записи будут завершены."
      submitLabel="Сохранить пароль"
      doneMessage="Пароль изменён. Войдите в систему с новым паролем."
    />
  );
}

/**
 * Sets a password from an e-mailed link without a session. The token comes in the address fragment, which never reaches
 * the server or a referrer; it is read once into state and the fragment is dropped from the address at once, so the
 * token stays out of the history and of a copied address. Only the POST spends it.
 */
function LinkPasswordPage({
  path,
  title,
  intro,
  submitLabel,
  doneMessage,
}: LinkPasswordPageProps) {
  const { token: designToken } = antdTheme.useToken();
  const { hash, pathname } = useLocation();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [form] = Form.useForm<LinkPasswordValues>();
  const [token] = useState(() =>
    new URLSearchParams(hash.slice(1)).get("token"),
  );
  useEffect(() => {
    if (hash) void navigate(pathname, { replace: true });
  }, [hash, pathname, navigate]);
  const setPassword = useMutation({
    mutationFn: async ({ password }: LinkPasswordValues) =>
      unwrap(await api.POST(path, { body: { token: token ?? "", password } })),
    onSuccess: () => {
      message.success(doneMessage);
      void navigate(LOGIN_PATH, { replace: true });
    },
    onError: (error) => showFieldErrors(form, error),
  });
  const invalidLink = isInvalidLink(setPassword.error)
    ? setPassword.error
    : undefined;

  return (
    <AuthCard title={title}>
      {!token || invalidLink ? (
        <>
          <Alert
            type="error"
            showIcon
            title={invalidLink?.title ?? "Ссылка недействительна"}
            description={
              invalidLink?.detail ??
              "В адресе нет ключа ссылки. Откройте ссылку из письма полностью."
            }
          />
          <Flex
            justify="center"
            gap={designToken.margin}
            wrap
            style={{ marginBlockStart: designToken.margin }}
          >
            <AuthLink to={FORGOT_PASSWORD_PATH}>Не помню пароль</AuthLink>
            <AuthLink to={LOGIN_PATH}>Вернуться ко входу</AuthLink>
          </Flex>
        </>
      ) : (
        <>
          <Typography.Paragraph type="secondary">{intro}</Typography.Paragraph>
          <Form
            form={form}
            name="link-password"
            layout="vertical"
            noValidate
            onFinish={(values) => setPassword.mutate(values)}
          >
            {setPassword.isError && (
              <Form.Item>
                <ErrorAlert
                  title="Не удалось задать пароль"
                  error={setPassword.error}
                />
              </Form.Item>
            )}
            <Form.Item
              name="password"
              label="Пароль"
              extra={PASSWORD_HINT}
              rules={[{ required: true, message: "Введите пароль" }]}
            >
              <Input.Password autoComplete="new-password" autoFocus />
            </Form.Item>
            <Form.Item
              name="confirmPassword"
              label="Повторите пароль"
              dependencies={["password"]}
              rules={[
                { required: true, message: "Повторите пароль" },
                ({ getFieldValue }) => ({
                  validator: (_, value) =>
                    !value || getFieldValue("password") === value
                      ? Promise.resolve()
                      : Promise.reject(new Error("Пароли не совпадают")),
                }),
              ]}
            >
              <Input.Password autoComplete="new-password" />
            </Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              size="large"
              block
              loading={setPassword.isPending}
            >
              {submitLabel}
            </Button>
          </Form>
        </>
      )}
    </AuthCard>
  );
}
