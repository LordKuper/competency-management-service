import { Modal, type ModalProps } from "antd";
import { useEffect, useRef } from "react";

// Select and picker inputs are read-only unless searchable, yet they are the controls to focus.
const FIELD =
  'input:not([type="hidden"]):not([disabled]), textarea:not([disabled]), select:not([disabled])';

function firstField(body: HTMLElement) {
  return Array.from(body.querySelectorAll<HTMLElement>(FIELD)).find(
    (el) =>
      el.getClientRects().length > 0 &&
      (!el.hasAttribute("readonly") ||
        el.closest(".ant-select, .ant-picker") !== null),
  );
}

/** First field of the body; with none, the primary button of the footer, if the dialog has one. */
function focusEntry(dialog: HTMLElement, body: HTMLElement) {
  const target =
    firstField(body) ??
    dialog.querySelector<HTMLElement>(".ant-modal-footer .ant-btn-primary");
  target?.focus();
  return target != null;
}

/**
 * The application's dialog: antd `Modal` that, once opened, moves focus to its first form field, or to the primary
 * footer button when it has no field. A field that appears after the opening, as when a record is still loading, gets
 * the focus too, unless the user has already moved it inside the dialog. Re-renders never move focus.
 */
export function AppModal({ afterOpenChange, children, ...props }: ModalProps) {
  const bodyRef = useRef<HTMLDivElement>(null);
  const stopWaiting = useRef<() => void>(() => {});
  useEffect(() => () => stopWaiting.current(), []);

  function handleOpenChange(isOpen: boolean) {
    afterOpenChange?.(isOpen);
    stopWaiting.current();
    const body = bodyRef.current;
    const dialog = body?.closest<HTMLElement>('[role="dialog"]');
    if (!isOpen || !body || !dialog || focusEntry(dialog, body)) return;

    const observer = new MutationObserver(() => {
      if (focusEntry(dialog, body)) stopWaiting.current();
    });
    const onFocusIn = (event: FocusEvent) => {
      if (event.target !== dialog) stopWaiting.current();
    };
    stopWaiting.current = () => {
      observer.disconnect();
      dialog.removeEventListener("focusin", onFocusIn);
      stopWaiting.current = () => {};
    };
    observer.observe(body, { childList: true, subtree: true });
    dialog.addEventListener("focusin", onFocusIn);
  }

  return (
    <Modal {...props} afterOpenChange={handleOpenChange}>
      <div ref={bodyRef}>{children}</div>
    </Modal>
  );
}
