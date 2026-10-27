import { App } from "antd";

/**
 * Returns the confirmation asked before the head of a unit is moved out of it, since the server then leaves the unit without a head.
 * It resolves to whether the user confirmed.
 */
export function useConfirmUnitLosesHead() {
  const { modal } = App.useApp();
  return (unitName: string) =>
    new Promise<boolean>((resolve) =>
      modal.confirm({
        title: "Перевести руководителя?",
        content: `Подразделение «${unitName}» останется без руководителя. Продолжить?`,
        okText: "Перевести",
        cancelText: "Отмена",
        onOk: () => resolve(true),
        onCancel: () => resolve(false),
      }),
    );
}
