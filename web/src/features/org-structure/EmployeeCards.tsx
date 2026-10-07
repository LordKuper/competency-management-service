import { useMemo } from "react";
import { type EmployeeAction, EmployeeCard } from "./EmployeeCard";
import type { Employee } from "./orgStructureApi";

interface EmployeeCardsProps {
  employees: readonly Employee[];
  /** The unit's head; listed first when among the employees. */
  headEmployeeId: string | null;
  /** Lower-cased search text to highlight in the cards; blank when not searching. */
  needle?: string;
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

/** The given employees of one unit as list items of the unit's children list, in listing order. */
export function EmployeeCards({
  employees,
  headEmployeeId,
  needle,
  isAdmin,
  onAction,
}: EmployeeCardsProps) {
  const ordered = useMemo(
    () => inListingOrder(employees, headEmployeeId),
    [employees, headEmployeeId],
  );
  return ordered.map((employee) => (
    <li key={employee.id} className="org-tree__node">
      <EmployeeCard
        employee={employee}
        needle={needle}
        isAdmin={isAdmin}
        onAction={onAction}
      />
    </li>
  ));
}
