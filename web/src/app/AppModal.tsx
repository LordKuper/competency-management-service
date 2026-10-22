import { Form, Modal, type ModalProps } from "antd";
import { useEffect, useRef, useState } from "react";
import { useLeaveGuard } from "./leaveGuard";

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
 * the focus too, unless the user has already moved it inside the dialog. Re-renders never move focus. A form changed
 * in the dialog, or `isUnsaved`, makes closing it by the cross, Escape or the scrim ask for confirmation.
 */
export function AppModal({
  afterOpenChange,
  children,
  isUnsaved = false,
  onCancel,
  ...props
}: ModalProps & { isUnsaved?: boolean }) {
  const [isTouched, setIsTouched] = useState(false);
  const isModalUnsaved = !!props.open && (isTouched || isUnsaved);
  const { confirmLeave } = useLeaveGuard({ isUnsaved: isModalUnsaved });
  const bodyRef = useRef<HTMLDivElement>(null);
  const stopWaiting = useRef<() => void>(() => {});
  useEffect(() => () => stopWaiting.current(), []);

  function handleOpenChange(isOpen: boolean) {
    afterOpenChange?.(isOpen);
    if (!isOpen) setIsTouched(false);
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
    <Modal
      {...props}
      afterOpenChange={handleOpenChange}
      onCancel={(event) => {
        if (event.defaultPrevented) return;
        const close = () => onCancel?.(event);
        if (isModalUnsaved) confirmLeave(close);
        else close();
      }}
    >
      <Form.Provider
        onFormChange={(_, { changedFields }) => {
          if (changedFields.some((field) => field.touched)) {
            setIsTouched(true);
          }
        }}
      >
        <div ref={bodyRef}>{children}</div>
      </Form.Provider>
    </Modal>
  );
}
