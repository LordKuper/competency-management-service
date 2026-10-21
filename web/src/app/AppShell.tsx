import {
  theme as antdTheme,
  ConfigProvider,
  Flex,
  Grid,
  Layout,
  Menu,
  type MenuProps,
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
import type { NavGroup, NavItem } from "./featureContract";
import { navItemsFor } from "./features";
import { LeaveGuardProvider } from "./leaveGuard";
import { PRODUCT_NAME } from "./productName";
import { UserMenu } from "./UserMenu";

const brandStyle: CSSProperties = { flex: "0 1 auto", minWidth: 0 };

const productNameStyle: CSSProperties = {
  minWidth: 0,
  overflow: "hidden",
  textOverflow: "ellipsis",
  whiteSpace: "nowrap",
};

const GROUP_LABELS: Record<NavGroup, string> = {
  reference: "Справочники",
  admin: "Администрирование",
};

interface Submenu {
  key: string;
  label: string;
  children: NonNullable<MenuProps["items"]>;
}

/** Entries of one group form one submenu, placed where its first entry would be. */
function menuItems(navItems: NavItem[]): MenuProps["items"] {
  const leaf = (item: NavItem) => ({
    key: item.key,
    label: (
      <Link to={item.path}>
        {item.label}
        {item.badge && <item.badge />}
      </Link>
    ),
  });
  const items: NonNullable<MenuProps["items"]> = [];
  const submenus = new Map<NavGroup, Submenu>();
  for (const item of navItems) {
    if (!item.group) {
      items.push(leaf(item));
      continue;
    }
    let submenu = submenus.get(item.group);
    if (!submenu) {
      submenu = {
        key: item.group,
        label: GROUP_LABELS[item.group],
        children: [],
      };
      submenus.set(item.group, submenu);
      items.push(submenu);
    }
    submenu.children.push(leaf(item));
  }
  return items;
}

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
      <LeaveGuardProvider>
        <Layout style={{ minHeight: "100vh" }}>
          <Layout.Header
            style={{
              display: "flex",
              alignItems: "center",
              gap: token.marginLG,
            }}
          >
            <Flex align="center" gap={token.marginXS} style={brandStyle}>
              <Flex
                align="center"
                justify="center"
                style={{
                  flex: "none",
                  background: token.colorBgContainer,
                  borderRadius: "50%",
                }}
              >
                <img src="/brand/logo-256.png" alt="" width={32} height={32} />
              </Flex>
              <strong title={PRODUCT_NAME} style={productNameStyle}>
                {PRODUCT_NAME}
              </strong>
            </Flex>
            {navItems.length > 0 && (
              <Menu
                theme="dark"
                mode="horizontal"
                selectedKeys={current ? [current.key] : []}
                items={menuItems(navItems)}
                style={{ flex: 1, minWidth: 0 }}
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
      </LeaveGuardProvider>
    </ConfigProvider>
  );
}
