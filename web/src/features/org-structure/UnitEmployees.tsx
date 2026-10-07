import { useQuery } from "@tanstack/react-query";
import { Card, Typography } from "antd";
import { memo } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import type { EmployeeAction } from "./EmployeeCard";
import { EmployeeCards } from "./EmployeeCards";
import { type Employee, unitEmployeesQuery } from "./orgStructureApi";

interface UnitEmployeesProps {
  unitId: string;
  /** The unit's head; listed first when among the unit's employees. */
  headEmployeeId: string | null;
  hasChildUnits: boolean;
  isAdmin: boolean;
  onAction: (action: EmployeeAction, employee: Employee) => void;
}

/**
 * The employee cards of an open unit, as list items of the unit's children list. They are read when the unit opens
 * and only for that unit, so a collapsed unit costs no request. A unit with neither employees nor child units says so
 * once; with either, there is nothing to report.
 */
export const UnitEmployees = memo(function UnitEmployees({
  unitId,
  headEmployeeId,
  hasChildUnits,
  isAdmin,
  onAction,
}: UnitEmployeesProps) {
  const {
    data: employees,
    error,
    refetch,
  } = useQuery(unitEmployeesQuery(unitId));

  if (error) {
    return (
      <li className="org-tree__node">
        <div className="org-tree__item">
          <ErrorAlert
            title="Не удалось загрузить сотрудников"
            error={error}
            onRetry={() => void refetch()}
          />
        </div>
      </li>
    );
  }
  if (!employees) {
    return (
      <li className="org-tree__node">
        <Card size="small" loading className="org-tree__item" />
      </li>
    );
  }
  if (employees.length === 0) {
    if (hasChildUnits) return null;
    return (
      <li className="org-tree__node">
        <div className="org-tree__item org-tree__note">
          <Typography.Text type="secondary">
            Нет сотрудников или дочерних подразделений
          </Typography.Text>
        </div>
      </li>
    );
  }
  return (
    <EmployeeCards
      employees={employees}
      headEmployeeId={headEmployeeId}
      isAdmin={isAdmin}
      onAction={onAction}
    />
  );
});
