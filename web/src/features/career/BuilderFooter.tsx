import { Button, Flex, Typography } from "antd";

/** How the matrix editor serves one step of the wizard: the part of the version it shows and what its buttons do. */
export interface BuilderStepControls {
  /** The requirements grid or the blocks with their coefficients and thresholds. */
  part: "requirements" | "formula";
  /** The step shown; leaving from it records it, going on records the next. */
  step: number;
  onBack: () => void;
  /** Called once the save asked to go on or to leave succeeded. */
  onSaved: (then: "next" | "exit") => void;
  /** Goes on without saving, for a version the user may not edit. */
  onSkip: () => void;
}

interface BuilderFooterProps {
  /** The step the buttons belong to. */
  step: number;
  onBack: () => void;
  onSkip: () => void;
  isEditable: boolean;
  isUnsaved: boolean;
  isSaving: boolean;
  /** Why going on is not possible now, written beside the button; going on is off while this is given. */
  nextBlock?: string;
  onSave: (step: number, then: "next" | "exit") => void;
  onCancelEdits: () => void;
}

/**
 * Buttons under a wizard step. While the step holds unsaved edits, going back and leaving are off and the reason is
 * written beside them; the edits can be saved or dropped.
 */
export function BuilderFooter({
  step,
  onBack,
  onSkip,
  isEditable,
  isUnsaved,
  isSaving,
  nextBlock,
  onSave,
  onCancelEdits,
}: BuilderFooterProps) {
  const back = (
    <Button disabled={isUnsaved} onClick={onBack}>
      Назад
    </Button>
  );
  if (!isEditable) {
    return (
      <Flex justify="space-between" gap="small">
        {back}
        <Button type="primary" onClick={onSkip}>
          Далее
        </Button>
      </Flex>
    );
  }
  return (
    <Flex justify="space-between" align="center" gap="small" wrap>
      {back}
      <Flex align="center" gap="small" wrap>
        {nextBlock && (
          <Typography.Text type="secondary">{nextBlock}</Typography.Text>
        )}
        {isUnsaved && !isSaving && (
          <>
            <Typography.Text type="secondary">
              Есть несохранённые правки. Сохраните их, чтобы перейти на другой
              шаг.
            </Typography.Text>
            <Button type="link" onClick={onCancelEdits}>
              Отменить правки
            </Button>
          </>
        )}
        <Button disabled={isSaving} onClick={() => onSave(step, "exit")}>
          Сохранить и выйти
        </Button>
        <Button
          type="primary"
          loading={isSaving}
          disabled={nextBlock !== undefined || undefined}
          onClick={() => onSave(step + 1, "next")}
        >
          Сохранить и далее
        </Button>
      </Flex>
    </Flex>
  );
}
