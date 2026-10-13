import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Alert, theme as antdTheme, Button, Flex, Form, Input } from "antd";
import { useNavigate, useSearchParams } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { HOME_PATH } from "../../app/featureContract";
import { AuthCard, AuthLink } from "./AuthCard";
import { FORGOT_PASSWORD_PATH } from "./ForgotPasswordPage";
import { returnTargetOf } from "./returnTo";
import { currentUserQueryKey } from "./useCurrentUser";

interface LoginValues {
  email: string;
  password: string;
}

/**
 * Sign-in screen shown outside the shell; every refusal looks the same so it reveals nothing about accounts. A signed-in
 * user goes on to the page the address names when it is a safe in-app target, otherwise home.
 */
export function LoginPage() {
  const { token } = antdTheme.useToken();
  const [form] = Form.useForm<LoginValues>();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const returnTarget = returnTargetOf(searchParams);
  const signIn = useMutation({
    mutationFn: async (values: LoginValues) =>
      unwrap(await api.POST("/api/v1/auth/login", { body: values })),
    onSuccess: (user) => {
      queryClient.setQueryData(currentUserQueryKey, user);
      void navigate(returnTarget ?? HOME_PATH, { replace: true });
    },
    onError: (error) => showFieldErrors(form, error),
  });

  return (
    <AuthCard title="Вход в систему">
      <Form
        form={form}
        name="login"
        layout="vertical"
        autoComplete="on"
        noValidate
        onFinish={(values) => signIn.mutate(values)}
      >
        {returnTarget && (
          <Form.Item>
            <Alert
              type="info"
              showIcon
              title="После входа откроется страница, на которую вела ссылка."
            />
          </Form.Item>
        )}
        {signIn.isError && (
          <Form.Item>
            <ErrorAlert title="Не удалось войти" error={signIn.error} />
          </Form.Item>
        )}
        <Form.Item
          name="email"
          label="E-mail"
          rules={[
            {
              required: true,
              whitespace: true,
              message: "Введите e-mail",
            },
          ]}
        >
          <Input type="email" autoComplete="username" autoFocus />
        </Form.Item>
        <Form.Item
          name="password"
          label="Пароль"
          rules={[{ required: true, message: "Введите пароль" }]}
        >
          <Input.Password autoComplete="current-password" />
        </Form.Item>
        <Button
          type="primary"
          htmlType="submit"
          size="large"
          block
          loading={signIn.isPending}
        >
          Войти
        </Button>
      </Form>
      <Flex justify="center" style={{ marginBlockStart: token.margin }}>
        <AuthLink to={FORGOT_PASSWORD_PATH}>Не помню пароль</AuthLink>
      </Flex>
    </AuthCard>
  );
}
