import { memo } from "react";
import type { Employee } from "./orgStructureApi";
import { EmployeeStatusTag } from "./StatusTags";

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
  /** Opens the employee in the dialog; the callback must be stable, or every card renders again with the page. */
  onOpen: (employeeId: string) => void;
}

/**
 * Compact card of an employee inside the unit hierarchy; the name opens the employee. Administrators also see the
 * status of a person who no longer works; the restricted projection of an ordinary user has none.
 * A unit may hold thousands of employees, so the card is plain markup styled from the theme variables: antd's own
 * card, avatar and text components cost several times more to render.
 */
export const EmployeeCard = memo(function EmployeeCard({
  employee,
  isAdmin,
  onOpen,
}: EmployeeCardProps) {
  return (
    <div className="org-tree__item org-employee">
      <span className="org-employee__avatar">
        {initialsOf(employee.fullName)}
      </span>
      <div className="org-employee__text">
        <div className="org-employee__title">
          <button
            type="button"
            className="org-link"
            onClick={() => onOpen(employee.id)}
          >
            <strong>{employee.fullName}</strong>
          </button>
          {isAdmin && employee.isActive === false && (
            <EmployeeStatusTag isActive={false} />
          )}
        </div>
        <span className="org-employee__details">{employee.position}</span>
      </div>
    </div>
  );
});
