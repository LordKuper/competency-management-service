import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  theme as antdTheme,
  Button,
  Card,
  Col,
  Flex,
  Form,
  Input,
  Row,
  Typography,
} from "antd";
import { useNavigate } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { PRODUCT_NAME, PRODUCT_TAGLINE } from "../../app/productName";
import { currentUserQueryKey } from "./useCurrentUser";

interface LoginValues {
  email: string;
  password: string;
}

/** Sign-in screen shown outside the shell; every refusal looks the same so it reveals nothing about accounts. */
export function LoginPage() {
  const { token } = antdTheme.useToken();
  const [form] = Form.useForm<LoginValues>();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const signIn = useMutation({
    mutationFn: async (values: LoginValues) =>
      unwrap(await api.POST("/api/v1/auth/login", { body: values })).data,
    onSuccess: (user) => {
      queryClient.setQueryData(currentUserQueryKey, user);
      void navigate("/", { replace: true });
    },
    onError: (error) => showFieldErrors(form, error),
  });

  return (
    <Row
      justify="center"
      align="middle"
      style={{ minHeight: "100vh", background: token.colorBgLayout }}
    >
      <Col xs={22} sm={16} md={12} lg={8} xl={6}>
        <Card>
          <Flex justify="center">
            <img
              src="/brand/logo-256.png"
              alt={PRODUCT_NAME}
              width={80}
              height={80}
            />
          </Flex>
          <Typography.Title level={1}>Вход в систему</Typography.Title>
          <Typography.Paragraph type="secondary">
            {PRODUCT_TAGLINE}
          </Typography.Paragraph>
          <Form
            form={form}
            name="login"
            layout="vertical"
            autoComplete="on"
            onFinish={(values) => signIn.mutate(values)}
          >
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
              <Input autoComplete="username" autoFocus />
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
        </Card>
      </Col>
    </Row>
  );
}
