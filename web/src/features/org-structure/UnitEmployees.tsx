import { useQuery } from "@tanstack/react-query";
import { Card, Typography } from "antd";
import { memo } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { type EmployeeAction, EmployeeCard } from "./EmployeeCard";
import { type Employee, unitEmployeesQuery } from "./orgStructureApi";

interface UnitEmployeesProps {
  unitId: string;
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
  hasChildUnits,
  isAdmin,
  onAction,
}: UnitEmployeesProps) {
  const { data, error, refetch } = useQuery(unitEmployeesQuery(unitId));

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
  if (!data) {
    return (
      <li className="org-tree__node">
        <Card size="small" loading className="org-tree__item" />
      </li>
    );
  }
  if (data.length === 0) {
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
  return data.map((employee) => (
    <li key={employee.id} className="org-tree__node">
      <EmployeeCard employee={employee} isAdmin={isAdmin} onAction={onAction} />
    </li>
  ));
});
