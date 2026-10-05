import { KeyOutlined, LogoutOutlined } from "@ant-design/icons";
import {
  Avatar,
  theme as antdTheme,
  Button,
  Dropdown,
  Grid,
  Space,
} from "antd";
import { useState } from "react";
import { ChangePasswordModal } from "../features/auth/ChangePasswordModal";
import type { CurrentUser } from "../features/auth/useCurrentUser";
import { useSignOut } from "../features/auth/useSignOut";

/** Header control that names the signed-in user and offers changing their password and signing out. */
export function UserMenu({ user }: { user: CurrentUser }) {
  const { token } = antdTheme.useToken();
  const screens = Grid.useBreakpoint();
  const signOut = useSignOut();
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);
  const displayName = user.employeeName
    ? `${user.employeeName} (${user.userName})`
    : user.userName;

  return (
    <>
      <Dropdown
        trigger={["click"]}
        menu={{
          items: [
            {
              key: "change-password",
              icon: <KeyOutlined />,
              label: "Сменить пароль",
              onClick: () => setIsChangePasswordOpen(true),
            },
            {
              key: "sign-out",
              icon: <LogoutOutlined />,
              label: "Выйти",
              onClick: () => signOut.mutate(),
            },
          ],
        }}
      >
        <Button
          type="text"
          aria-label={displayName}
          style={{ color: token.colorTextLightSolid }}
        >
          <Space>
            <Avatar
              style={{
                background: token.colorPrimaryBg,
                color: token.colorPrimary,
              }}
            >
              {displayName.charAt(0).toUpperCase()}
            </Avatar>
            {screens.lg && displayName}
          </Space>
        </Button>
      </Dropdown>
      <ChangePasswordModal
        open={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </>
  );
}
