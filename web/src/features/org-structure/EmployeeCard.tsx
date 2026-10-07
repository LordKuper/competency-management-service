import {
  DeleteOutlined,
  EditOutlined,
  MoreOutlined,
  UndoOutlined,
  UserDeleteOutlined,
} from "@ant-design/icons";
import { Dropdown } from "antd";
import { memo, useState } from "react";
import type { Employee } from "./orgStructureApi";
import { EmployeeStatusTag } from "./StatusTags";

/** What an administrator can do with an employee from the card. */
export type EmployeeAction = "edit" | "dismiss" | "rehire" | "delete";

function initialsOf(fullName: string): string {
  return fullName
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word.charAt(0).toUpperCase())
    .join("");
}

interface EmployeeCardProps {
  employee: Employee;
  isAdmin: boolean;
  /** Runs an action chosen in the card's menu; the callback must be stable, or every card renders again with the page. */
  onAction: (action: EmployeeAction, employee: Employee) => void;
}

/**
 * The actions button of a card, which opens the menu of what an administrator can do with the employee.
 * antd's Dropdown costs about a quarter of a millisecond per instance, which adds up to seconds for the thousands of
 * cards a unit may hold, so the button is plain markup until it is first used and only then becomes a Dropdown, already open.
 */
function EmployeeActions({
  employee,
  onAction,
}: Pick<EmployeeCardProps, "employee" | "onAction">) {
  const [isOpen, setIsOpen] = useState<boolean | null>(null);
  const isDismissed = employee.isActive === false;
  const button = (
    <button
      type="button"
      className="org-employee__menu"
      aria-label={`Действия с сотрудником ${employee.fullName}`}
      aria-haspopup="menu"
      onClick={isOpen === null ? () => setIsOpen(true) : undefined}
    >
      <MoreOutlined />
    </button>
  );
  if (isOpen === null) return button;

  return (
    <Dropdown
      open={isOpen}
      onOpenChange={setIsOpen}
      trigger={["click"]}
      menu={{
        items: [
          {
            key: "edit",
            icon: <EditOutlined />,
            label: "Править",
            onClick: () => onAction("edit", employee),
          },
          isDismissed
            ? {
                key: "rehire",
                icon: <UndoOutlined />,
                label: "Вернуть на работу",
                onClick: () => onAction("rehire", employee),
              }
            : {
                key: "dismiss",
                icon: <UserDeleteOutlined />,
                label: "Уволить",
                onClick: () => onAction("dismiss", employee),
              },
          { type: "divider" },
          {
            key: "delete",
            icon: <DeleteOutlined />,
            danger: true,
            label: "Удалить",
            onClick: () => onAction("delete", employee),
          },
        ],
      }}
    >
      {button}
    </Dropdown>
  );
}

/**
 * Compact card of an employee inside the unit hierarchy. Next to the position it shows the e-mail of the employee's
 * account, when there is one. Administrators also see the status of a person who no longer works and the menu of
 * actions; the restricted projection of an ordinary user has no status, and such a user only reads.
 * A unit may hold thousands of employees, so the card is plain markup styled from the theme variables: antd's own
 * card, avatar and text components cost several times more to render.
 */
export const EmployeeCard = memo(function EmployeeCard({
  employee,
  isAdmin,
  onAction,
}: EmployeeCardProps) {
  const details = [employee.position, employee.email].filter(Boolean);

  return (
    <div className="org-tree__item org-employee">
      <span className="org-employee__avatar">
        {initialsOf(employee.fullName)}
      </span>
      <div className="org-employee__text">
        <div className="org-employee__title">
          <strong>{employee.fullName}</strong>
          {isAdmin && employee.isActive === false && (
            <EmployeeStatusTag isActive={false} />
          )}
        </div>
        <span className="org-employee__details">{details.join(" · ")}</span>
      </div>
      {isAdmin && <EmployeeActions employee={employee} onAction={onAction} />}
    </div>
  );
});
