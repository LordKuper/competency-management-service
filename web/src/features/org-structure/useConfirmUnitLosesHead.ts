import { App } from "antd";
import type { OrgUnit } from "./orgStructureApi";

/**
 * The units an employee heads that none of their assignments stays in once a change is applied, since the server takes
 * them off the headship there. A head keeps a unit while any assignment, main or secondary, remains in it.
 */
export function headedUnitsLeft(
  units: readonly OrgUnit[] | undefined,
  employeeId: string,
  remainingUnitIds: readonly string[],
): OrgUnit[] {
  return (units ?? []).filter(
    (unit) =>
      unit.headEmployeeId === employeeId && !remainingUnitIds.includes(unit.id),
  );
}

/**
 * Returns the confirmation asked before an employee is left without an assignment in the units they head, since the
 * server then leaves those units without a head. It resolves to whether the user confirmed.
 */
export function useConfirmUnitLosesHead() {
  const { modal } = App.useApp();
  return (unitNames: readonly string[]) =>
    new Promise<boolean>((resolve) =>
      modal.confirm({
        title: "Снять с руководства?",
        content:
          unitNames.length === 1
            ? `Подразделение «${unitNames[0]}» останется без руководителя. Продолжить?`
            : `Подразделения ${unitNames.map((name) => `«${name}»`).join(", ")} останутся без руководителя. Продолжить?`,
        okText: "Продолжить",
        cancelText: "Отмена",
        onOk: () => resolve(true),
        onCancel: () => resolve(false),
      }),
    );
}
