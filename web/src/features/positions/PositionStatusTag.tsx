import { Tag } from "antd";

/** Names the state of a position in words, since colour alone must not carry it. */
export function PositionStatusTag({ isActive }: { isActive: boolean }) {
  return isActive ? <Tag color="success">Активна</Tag> : <Tag>Неактивна</Tag>;
}
