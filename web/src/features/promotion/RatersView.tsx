import { Alert, Button, Flex, Form, Typography } from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { EmployeeOption } from "./CommissionView";
import type { PromotionApplication } from "./promotionApi";
import { HEAD_RATERS_HINT, STAGE } from "./promotionTexts";
import { RatersField } from "./RatersField";

interface RatersFormValues {
  raterEmployeeIds: string[];
}

interface RatersViewProps {
  application: PromotionApplication;
  /** Employees the last search found. */
  candidates: readonly EmployeeOption[];
  isSearching: boolean;
  onSearch: (text: string) => void;
  /** Saves the list; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (raterEmployeeIds: string[]) => Promise<unknown>;
}

/** The raters of an admitted candidate, which the head of the target unit may add to or correct while the rating runs. */
export function RatersView({
  application,
  candidates,
  isSearching,
  onSearch,
  onSubmit,
}: RatersViewProps) {
  const [form] = Form.useForm<RatersFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit(values.raterEmployeeIds ?? []);
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });
  const canEdit =
    application.roles.includes("Head") &&
    application.stage === STAGE.rating &&
    application.status === "Admitted";
  const raters = application.raters ?? [];

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
          title="Список оценивающих можно менять на этапе «Круговая оценка»"
        />
      )}
      <Form
        form={form}
        name="promotion-raters"
        layout="vertical"
        disabled={isSubmitting}
        initialValues={{
          raterEmployeeIds: raters.map((rater) => rater.employeeId),
        }}
        onFinish={submit}
        onValuesChange={() => setIsDirty(form.isFieldsTouched())}
      >
        <Form.Item name="raterEmployeeIds" extra={HEAD_RATERS_HINT}>
          <RatersField
            initial={raters}
            candidates={candidates}
            excludedIds={[application.candidateEmployeeId]}
            isSearching={isSearching}
            isLocked={isSubmitting || !canEdit}
            onSearch={onSearch}
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
