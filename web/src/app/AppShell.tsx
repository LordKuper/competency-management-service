import {
  theme as antdTheme,
  ConfigProvider,
  Flex,
  Grid,
  Layout,
  Menu,
  Spin,
} from "antd";
import type { CSSProperties } from "react";
import { Link, Outlet, useLocation } from "react-router";
import { ApiError } from "../api/ApiError";
import {
  type CurrentUser,
  useCurrentUser,
} from "../features/auth/useCurrentUser";
import { ErrorAlert } from "./ErrorAlert";
import { navItemsFor } from "./features";
import { PRODUCT_NAME } from "./productName";
import { UserMenu } from "./UserMenu";

const productNameStyle: CSSProperties = {
  flex: "0 1 auto",
  minWidth: 0,
  overflow: "hidden",
  textOverflow: "ellipsis",
  whiteSpace: "nowrap",
};

/**
 * Frame of every signed-in screen and the route guard: nothing renders until the session is known,
 * and a missing session leaves the redirect to the single unauthorized handler.
 */
export function AppShell() {
  const { data: user, error, isPending, refetch } = useCurrentUser();
  if (isPending) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: "100vh" }}>
        <Spin size="large" />
      </Flex>
    );
  }
  if (user) return <ShellFrame user={user} />;
  if (error instanceof ApiError && error.status === 401) return null;
  return (
    <ErrorAlert
      title="Не удалось загрузить данные учётной записи"
      error={error}
      onRetry={() => void refetch()}
    />
  );
}

function ShellFrame({ user }: { user: CurrentUser }) {
  const { token } = antdTheme.useToken();
  const screens = Grid.useBreakpoint();
  const { pathname } = useLocation();
  const navItems = navItemsFor(user.role);
  const current = navItems.find(
    (item) => pathname === item.path || pathname.startsWith(`${item.path}/`),
  );
  return (
    <ConfigProvider componentSize={screens.lg ? "middle" : "large"}>
      <Layout style={{ minHeight: "100vh" }}>
        <Layout.Header
          style={{
            display: "flex",
            alignItems: "center",
            gap: token.marginLG,
          }}
        >
          <strong title={PRODUCT_NAME} style={productNameStyle}>
            {PRODUCT_NAME}
          </strong>
          {navItems.length > 0 && (
            <Menu
              theme="dark"
              mode="horizontal"
              selectedKeys={current ? [current.key] : []}
              items={navItems.map((item) => ({
                key: item.key,
                label: <Link to={item.path}>{item.label}</Link>,
              }))}
              style={{ flex: "none" }}
            />
          )}
          <div style={{ marginInlineStart: "auto" }}>
            <UserMenu user={user} />
          </div>
        </Layout.Header>
        <Layout.Content
          style={{ padding: screens.lg ? token.paddingLG : token.padding }}
        >
          <Outlet />
        </Layout.Content>
      </Layout>
    </ConfigProvider>
  );
}
