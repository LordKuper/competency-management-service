import type { ButtonProps, ModalFuncProps } from "antd";

/**
 * Props of an antd `modal.confirm` that tie the danger styling of the OK button to the initial focus: a destructive
 * confirmation opens focused on «Отмена», so Enter cannot trigger the irreversible action by accident.
 */
export function destructiveConfirm(
  isDanger: boolean,
  okButtonProps: ButtonProps = {},
): Pick<ModalFuncProps, "okButtonProps" | "autoFocusButton"> {
  return {
    okButtonProps: { ...okButtonProps, danger: isDanger },
    autoFocusButton: isDanger ? "cancel" : "ok",
  };
}
