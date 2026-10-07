import { useQuery } from "@tanstack/react-query";
import { Card, Typography } from "antd";
import { memo, useMemo } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { type EmployeeAction, EmployeeCard } from "./EmployeeCard";
import { type Employee, unitEmployeesQuery } from "./orgStructureApi";

interface UnitEmployeesProps {
  unitId: string;
  /** The unit's head; listed first when among the unit's employees. */
  headEmployeeId: string | null;
  hasChildUnits: boolean;
  isAdmin: boolean;
  onAction: (action: EmployeeAction, employee: Employee) => void;
}

const FULL_NAME_ORDER = new Intl.Collator("ru");

/** The unit's employees in listing order: the head first, then by full name in Russian alphabetical order, with «ё» sorted next to «е». */
function inListingOrder(
  employees: readonly Employee[],
  headEmployeeId: string | null,
): Employee[] {
  const isHead = (employee: Employee) => employee.id === headEmployeeId;
  return [...employees].sort(
    (a, b) =>
      Number(isHead(b)) - Number(isHead(a)) ||
      FULL_NAME_ORDER.compare(a.fullName, b.fullName),
  );
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
  const { data, error, refetch } = useQuery(unitEmployeesQuery(unitId));
  const employees = useMemo(
    () => data && inListingOrder(data, headEmployeeId),
    [data, headEmployeeId],
  );

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
  return employees.map((employee) => (
    <li key={employee.id} className="org-tree__node">
      <EmployeeCard employee={employee} isAdmin={isAdmin} onAction={onAction} />
    </li>
  ));
});
