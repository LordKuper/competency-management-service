import { Alert, Button, Flex, Form, Typography } from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { EmployeeOption } from "./CommissionView";
import type { PromotionApplication, RaterEntry } from "./promotionApi";
import { HEAD_RATERS_HINT, STAGE } from "./promotionTexts";
import { RatersField } from "./RatersField";
import { normalizedEmail } from "./raterEmail";

interface RatersFormValues {
  raters: RaterEntry[];
}

interface RatersViewProps {
  application: PromotionApplication;
  /** Employees the last search found. */
  candidates: readonly EmployeeOption[];
  isSearching: boolean;
  onSearch: (text: string) => void;
  /** Saves the list, which confirms the addresses it changes; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (raters: RaterEntry[]) => Promise<unknown>;
  /** Confirms the saved address of a rater without an account; a refusal is shown by the row. */
  onConfirmEmail: (employeeId: string, email: string) => Promise<unknown>;
}

function entriesOf(application: PromotionApplication): RaterEntry[] {
  return (application.raters ?? []).map((rater) => ({
    employeeId: rater.employeeId,
    email: rater.email,
  }));
}

/** The raters of a filed candidate, which the head of the target unit may add to or correct until the formal check ends, and the addresses of those without an account, which the head confirms. */
export function RatersView({
  application,
  candidates,
  isSearching,
  onSearch,
  onSubmit,
  onConfirmEmail,
}: RatersViewProps) {
  const [form] = Form.useForm<RatersFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit(
      (values.raters ?? []).map((rater) => ({
        employeeId: rater.employeeId,
        email: normalizedEmail(rater.email),
      })),
    );
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });
  const isBusy = isSubmitting || isConfirming;
  const canEdit =
    application.roles.includes("Head") &&
    application.stage <= STAGE.formalCheck &&
    application.status === "Submitted";

  async function confirmEmail(employeeId: string, email: string) {
    setIsConfirming(true);
    try {
      await onConfirmEmail(employeeId, email);
    } finally {
      setIsConfirming(false);
    }
  }

  return (
    <Flex vertical gap="middle">
      <Typography.Text strong>
        {application.candidateName}: {application.target.trackName},{" "}
        {application.target.levelName}
      </Typography.Text>
      {!canEdit && (
        <Alert
          type="info"
          showIcon
          title="Список оценивающих уже не меняется"
        />
      )}
      <Form
        form={form}
        name="promotion-raters"
        layout="vertical"
        disabled={isBusy}
        initialValues={{ raters: entriesOf(application) }}
        onFinish={submit}
        onValuesChange={() => setIsDirty(form.isFieldsTouched())}
      >
        <Form.Item name="raters" extra={HEAD_RATERS_HINT}>
          <RatersField
            saved={application.raters ?? []}
            candidates={candidates}
            excludedIds={[application.candidateEmployeeId]}
            isSearching={isSearching}
            isLocked={isBusy || !canEdit}
            onSearch={onSearch}
            onConfirmEmail={confirmEmail}
            isConfirmLocked={isDirty}
          />
        </Form.Item>
        {canEdit && (
          <Button type="primary" htmlType="submit" loading={isSubmitting}>
            Сохранить список
          </Button>
        )}
      </Form>
    </Flex>
  );
}
