import { MoreOutlined } from "@ant-design/icons";
import { Button, Dropdown, Space } from "antd";

export interface RowAction {
  key: string;
  label: string;
  danger?: boolean;
  disabled?: boolean;
  onClick: () => void;
}

const MAX_VISIBLE = 2;

interface RowActionsProps {
  actions: RowAction[];
  /** Accessible name of the «…» trigger, naming the row: «Действия с «Разработка»». */
  menuLabel: string;
}

/**
 * Row actions of a table: up to two as links, and with more the first two that are not destructive stay visible while
 * the rest, destructive ones included, go to a «…» menu, in their order.
 */
export function RowActions({ actions, menuLabel }: RowActionsProps) {
  const visible =
    actions.length <= MAX_VISIBLE
      ? actions
      : actions.filter((action) => !action.danger).slice(0, MAX_VISIBLE);
  const hidden = actions.filter((action) => !visible.includes(action));

  return (
    <Space size={0} wrap>
      {visible.map(({ key, label, danger, disabled, onClick }) => (
        <Button
          key={key}
          type="link"
          size="small"
          danger={danger}
          disabled={disabled}
          onClick={onClick}
        >
          {label}
        </Button>
      ))}
      {hidden.length > 0 && (
        <Dropdown
          trigger={["click"]}
          menu={{
            items: hidden.map(({ key, label, danger, disabled, onClick }) => ({
              key,
              label,
              danger,
              disabled,
              onClick,
            })),
          }}
        >
          <Button
            type="text"
            size="small"
            icon={<MoreOutlined />}
            aria-label={menuLabel}
          />
        </Dropdown>
      )}
    </Space>
  );
}
