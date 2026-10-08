import { Flex, Tag } from "antd";

/** Names the account state in words, since colour alone must not carry it; an invited account may be blocked as well. */
export function UserStatusTag({
  isBlocked,
  isInvited,
}: {
  isBlocked: boolean;
  isInvited: boolean;
}) {
  if (!isInvited) {
    return isBlocked ? (
      <Tag color="error">Заблокирован</Tag>
    ) : (
      <Tag color="success">Активен</Tag>
    );
  }
  return (
    <Flex gap="small" wrap>
      <Tag color="processing">Приглашён</Tag>
      {isBlocked && <Tag color="error">Заблокирован</Tag>}
    </Flex>
  );
}
