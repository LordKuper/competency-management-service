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

const NON_BREAKING_SPACE = " ";

/** The employee's initials and last name ("А. Б. Иванов"), or the e-mail when the account has no employee. */
function formatDisplayName(user: CurrentUser): string {
  const { employeeLastName, employeeFirstName, employeeMiddleName } = user;
  if (!employeeLastName || !employeeFirstName) {
    return user.email;
  }
  const givenNames = employeeMiddleName
    ? [employeeFirstName, employeeMiddleName]
    : [employeeFirstName];
  return [
    ...givenNames.map((name) => `${name.charAt(0).toUpperCase()}.`),
    employeeLastName,
  ].join(NON_BREAKING_SPACE);
}

/** Header control that names the signed-in user and offers changing their password and signing out; its menu shows the e-mail. */
export function UserMenu({ user }: { user: CurrentUser }) {
  const { token } = antdTheme.useToken();
  const screens = Grid.useBreakpoint();
  const signOut = useSignOut();
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);
  const displayName = formatDisplayName(user);

  return (
    <>
      <Dropdown
        trigger={["click"]}
        menu={{
          items: [
            {
              type: "group",
              label: user.email,
              children: [
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
            },
          ],
        }}
      >
        <Button
          type="text"
          className="header-user-button"
          aria-label={displayName}
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
