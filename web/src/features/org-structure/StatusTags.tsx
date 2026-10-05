import { Tag } from "antd";

/** Names the unit state in words, since colour alone must not carry it. */
export function UnitStatusTag({ isActive }: { isActive: boolean }) {
  return isActive ? <Tag color="success">Активно</Tag> : <Tag>Неактивно</Tag>;
}

/** Names the employment state in words, since colour alone must not carry it. */
export function EmployeeStatusTag({ isActive }: { isActive: boolean }) {
  return isActive ? (
    <Tag color="success">Работает</Tag>
  ) : (
    <Tag>Не работает</Tag>
  );
}
