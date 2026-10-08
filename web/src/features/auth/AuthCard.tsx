import { theme as antdTheme, Card, Col, Flex, Row, Typography } from "antd";
import type { ReactNode } from "react";
import { useHref, useLinkClickHandler } from "react-router";
import { PRODUCT_NAME } from "../../app/productName";

/** Frame of the screens used without a session (sign-in, forgotten password, password by link): a centred card with the logo and the screen title. */
export function AuthCard({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  const { token } = antdTheme.useToken();
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
          <Typography.Title level={1}>{title}</Typography.Title>
          {children}
        </Card>
      </Col>
    </Row>
  );
}

/** A link to another screen of the app, styled as an antd link and followed without reloading the page. */
export function AuthLink({ to, children }: { to: string; children: string }) {
  const href = useHref(to);
  const onClick = useLinkClickHandler<HTMLElement>(to);
  return (
    <Typography.Link href={href} onClick={onClick}>
      {children}
    </Typography.Link>
  );
}
