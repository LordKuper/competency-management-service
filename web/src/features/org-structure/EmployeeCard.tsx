import { memo } from "react";
import { Link } from "react-router";
import type { Employee } from "./orgStructureApi";
import { paths } from "./paths";
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
}

/**
 * Compact card of an employee inside the unit hierarchy. Administrators also see the status of
 * a person who no longer works and the way to edit; the restricted projection of an ordinary user has neither.
 * A unit may hold thousands of employees, so the card is plain markup styled from the theme variables: antd's own
 * card, avatar and text components cost several times more to render.
 */
export const EmployeeCard = memo(function EmployeeCard({
  employee,
  isAdmin,
}: EmployeeCardProps) {
  const details = [employee.position, employee.email].filter(Boolean);

  return (
    <div className="org-tree__item org-employee">
      <span className="org-employee__avatar">
        {initialsOf(employee.fullName)}
      </span>
      <div className="org-employee__text">
        <div className="org-employee__title">
          <Link to={paths.employee(employee.id)}>
            <strong>{employee.fullName}</strong>
          </Link>
          {isAdmin && employee.isActive === false && (
            <EmployeeStatusTag isActive={false} />
          )}
        </div>
        <span className="org-employee__details">{details.join(" · ")}</span>
      </div>
      {isAdmin && (
        <Link
          to={paths.employee(employee.id)}
          aria-label={`Править сотрудника ${employee.fullName}`}
        >
          Править
        </Link>
      )}
    </div>
  );
});
