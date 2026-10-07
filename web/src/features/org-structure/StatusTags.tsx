import { Tag } from "antd";

/** Names the state of a deactivated unit in words, since colour alone must not carry it. */
export function UnitStatusTag() {
  return <Tag>Неактивно</Tag>;
}

/** Names the state of a dismissed employee in words, since colour alone must not carry it. */
export function EmployeeStatusTag() {
  return <Tag>Не работает</Tag>;
}
