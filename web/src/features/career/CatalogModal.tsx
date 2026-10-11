import { Alert, Button, Space } from "antd";
import type { ReactNode } from "react";
import { ApiError } from "../../api/ApiError";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";

const CONFLICT_STATUS = 409;

interface CatalogModalProps {
  title: string;
  /** Failure of the last save, if any. */
  error: unknown;
  /** Fields of the form: a rejection naming only these is shown under them, so an alert would repeat it. */
  fields: readonly string[];
  /** Shown instead of the generic alert when the save is refused as a conflict with no field to blame. */
  conflictNotice?: string;
  onClose: () => void;
  children: ReactNode;
}

function isShownUnderFields(error: unknown, fields: readonly string[]) {
  return (
    error instanceof ApiError &&
    error.errors !== undefined &&
    Object.keys(error.errors).every((field) => fields.includes(field))
  );
}

/**
 * Dialog shell of the catalogue forms. Mount it only while it is shown, so every opening starts clean. An Escape that
 * something inside has already handled, such as cancelling the move of a level, leaves the dialog open.
 */
export function CatalogModal({
  title,
  error,
  fields,
  conflictNotice,
  onClose,
  children,
}: CatalogModalProps) {
  const isConflict =
    error instanceof ApiError &&
    error.status === CONFLICT_STATUS &&
    error.errors === undefined;

  return (
    <AppModal
      open
      title={title}
      onCancel={(event) => {
        if (!event.defaultPrevented) onClose();
      }}
      footer={null}
    >
      <Space orientation="vertical" size="large" style={{ display: "flex" }}>
        {isConflict && conflictNotice && (
          <Alert type="warning" showIcon title={conflictNotice} />
        )}
        {error != null &&
          !(isConflict && conflictNotice) &&
          !isShownUnderFields(error, fields) && (
            <ErrorAlert title="Не удалось сохранить запись" error={error} />
          )}
        {children}
      </Space>
    </AppModal>
  );
}

interface FormActionsProps {
  isSubmitting: boolean;
  onCancel: () => void;
}

/** Save and cancel buttons of a catalogue form. */
export function FormActions({ isSubmitting, onCancel }: FormActionsProps) {
  return (
    <Space>
      <Button type="primary" htmlType="submit" loading={isSubmitting}>
        Сохранить
      </Button>
      <Button onClick={onCancel}>Отмена</Button>
    </Space>
  );
}
