import { theme as antdTheme, Grid, Layout } from "antd";
import { Outlet } from "react-router";

const PRODUCT_NAME =
  "Сервис управления матрицами компетенций и повышением сотрудников";

/** Frame of every signed-in screen: product header and a content area that tightens below the desktop breakpoint. */
export function AppShell() {
  const { token } = antdTheme.useToken();
  const screens = Grid.useBreakpoint();
  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Layout.Header>
        <strong>{PRODUCT_NAME}</strong>
      </Layout.Header>
      <Layout.Content
        style={{ padding: screens.lg ? token.paddingLG : token.padding }}
      >
        <Outlet />
      </Layout.Content>
    </Layout>
  );
}
