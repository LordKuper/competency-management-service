import { Typography } from "antd";
import type { EmployeeImpact } from "./orgStructureApi";

/** What an employee's departure is confirmed for: dismissal keeps the record, deletion removes it and detaches the account. */
export type DepartureAction = "dismiss" | "delete";

function relatedChanges(
  impact: EmployeeImpact,
  action: DepartureAction,
): string[] {
  const { headOfUnits, account } = impact;
  const changes = headOfUnits.map(
    (unit) => `Подразделение «${unit.name}» останется без руководителя`,
  );
  if (account && !account.isBlocked) {
    changes.push(
      `Будет заблокирована учётная запись ${account.email}, её действующие сессии завершатся`,
    );
  }
  if (account && action === "delete") {
    changes.push(
      `Учётная запись ${account.email} будет отвязана от сотрудника`,
    );
  }
  return changes;
}

/** The confirmation text of a dismissal or deletion: the employee's own change, then every related change the server reports; only the refusal when the server would refuse. */
export function EmployeeImpactList({
  impact,
  action,
}: {
  impact: EmployeeImpact;
  action: DepartureAction;
}) {
  if (impact.account?.isLastActiveAdministrator) {
    return (
      <Typography.Paragraph type="danger">
        Нельзя уволить или удалить сотрудника: к нему привязан последний
        активный глобальный администратор ({impact.account.email}).
      </Typography.Paragraph>
    );
  }
  const changes = relatedChanges(impact, action);
  return (
    <>
      <Typography.Paragraph strong>
        {action === "delete"
          ? "Сотрудник будет удалён без возможности восстановления."
          : "Сотрудник получит статус «не работает»."}
      </Typography.Paragraph>
      {changes.length === 0 ? (
        <Typography.Paragraph>Связанных изменений нет.</Typography.Paragraph>
      ) : (
        <>
          <Typography.Paragraph>Связанные изменения:</Typography.Paragraph>
          <ul className="org-impact">
            {changes.map((change) => (
              <li key={change}>{change}</li>
            ))}
          </ul>
        </>
      )}
    </>
  );
}
