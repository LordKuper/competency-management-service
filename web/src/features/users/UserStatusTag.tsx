import { Tag } from "antd";

/** Names the account state in words, since colour alone must not carry it. */
export function UserStatusTag({ isBlocked }: { isBlocked: boolean }) {
  return isBlocked ? (
    <Tag color="error">Заблокирован</Tag>
  ) : (
    <Tag color="success">Активен</Tag>
  );
}
