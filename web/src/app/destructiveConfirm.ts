import type { ButtonProps, ModalFuncProps } from "antd";

/**
 * Props of an antd `modal.confirm` for a destructive or irreversible action: it opens focused on «Отмена», so Enter
 * cannot trigger the action by accident. The OK button is danger-styled and the focus is on «Отмена» together by default;
 * `isIrreversible` sets the focus alone, for an action that must not be restyled as danger.
 */
export function destructiveConfirm(
  isDanger: boolean,
  okButtonProps: ButtonProps = {},
  isIrreversible: boolean = isDanger,
): Pick<ModalFuncProps, "okButtonProps" | "autoFocusButton"> {
  return {
    okButtonProps: { ...okButtonProps, danger: isDanger },
    autoFocusButton: isDanger || isIrreversible ? "cancel" : "ok",
  };
}
