import { Alert, Button, Flex, Form, Select, Skeleton, Typography } from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { EDUCATION_LABELS, type EducationLevel } from "../career/matrixLabels";
import type { EmployeeOption } from "./CommissionView";
import { isEducationBelow } from "./educationRule";
import type {
  ApplicationInput,
  OpenWave,
  PromotionApplication,
  TargetForm,
} from "./promotionApi";
import {
  EDUCATION_WARNING,
  RATERS_HINT,
  scalePointLabel,
} from "./promotionTexts";
import { RatersField } from "./RatersField";

interface ApplyFormValues {
  matrixId: string;
  levelId: string;
  education: EducationLevel;
  scores: Record<string, number>;
  raterEmployeeIds: string[];
}

interface ApplyViewProps {
  wave: OpenWave;
  /** The application being changed; omitted when one is filed. */
  application?: PromotionApplication;
  /** The levels and competencies of the chosen target; omitted until one is chosen and loaded. */
  targetForm?: TargetForm;
  isTargetLoading: boolean;
  /** Employees the last search found. */
  candidates: readonly EmployeeOption[];
  /** The viewer, who cannot rate themselves. */
  candidateEmployeeId?: string;
  isSearching: boolean;
  onSearch: (text: string) => void;
  /** Called when another unit and track is chosen, so the competencies of the new target are loaded. */
  onTargetChange: (matrixId: string) => void;
  /** Saves the application; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: ApplicationInput) => Promise<unknown>;
}

function initialValuesOf(
  application?: PromotionApplication,
): Partial<ApplyFormValues> {
  if (!application) return {};
  return {
    matrixId: application.target.matrixId,
    levelId: application.target.levelId,
    education: application.education ?? undefined,
    scores: Object.fromEntries(
      application.selfScores.map((score) => [score.competencyId, score.score]),
    ),
    raterEmployeeIds:
      application.raters?.map((rater) => rater.employeeId) ?? [],
  };
}

/** The application for promotion: the target, the self-assessment by competency, the education and the people who rate the candidate. */
export function ApplyView({
  wave,
  application,
  targetForm,
  isTargetLoading,
  candidates,
  candidateEmployeeId,
  isSearching,
  onSearch,
  onTargetChange,
  onSubmit,
}: ApplyViewProps) {
  const [form] = Form.useForm<ApplyFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const matrixId = Form.useWatch("matrixId", form);
  const levelId = Form.useWatch("levelId", form);
  const education = Form.useWatch("education", form);

  const targets = wave.units.flatMap((unit) =>
    unit.targets.map((target) => ({ unit, target })),
  );
  const levels = targets.find(({ target }) => target.matrixId === matrixId)
    ?.target.levels;
  const scoredCompetencies = (targetForm?.competencies ?? []).filter(
    (competency) => competency.scalePoints.length > 0,
  );
  const required = targetForm?.levels.find(
    (level) => level.id === levelId,
  )?.requiredEducation;

  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit({
      matrixId: values.matrixId,
      levelId: values.levelId,
      education: values.education,
      selfScores: scoredCompetencies.flatMap((competency) => {
        const score = values.scores?.[competency.id];
        return score === undefined
          ? []
          : [{ competencyId: competency.id, score }];
      }),
      raterEmployeeIds: values.raterEmployeeIds ?? [],
    });
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });
  return (
    <Form
      form={form}
      name="promotion-application"
      layout="vertical"
      disabled={isSubmitting}
      initialValues={initialValuesOf(application)}
      onFinish={submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      <Form.Item
        name="matrixId"
        label="Подразделение и трек"
        rules={[{ required: true, message: "Выберите подразделение и трек" }]}
      >
        <Select
          placeholder="Выберите подразделение и трек"
          options={targets.map(({ unit, target }) => ({
            value: target.matrixId,
            label: `${unit.name}: ${target.trackName}`,
          }))}
          onChange={(next: string) => {
            form.setFieldValue("levelId", undefined);
            form.setFieldValue("scores", {});
            onTargetChange(next);
          }}
        />
      </Form.Item>
      <Form.Item
        name="levelId"
        label="Уровень"
        rules={[{ required: true, message: "Выберите уровень" }]}
      >
        <Select
          placeholder="Выберите уровень"
          disabled={!levels}
          options={levels?.map((level) => ({
            value: level.id,
            label: level.name,
          }))}
        />
      </Form.Item>
      <Form.Item
        name="education"
        label="Образование"
        rules={[{ required: true, message: "Укажите образование" }]}
      >
        <Select
          placeholder="Выберите уровень образования"
          options={Object.entries(EDUCATION_LABELS).map(([value, label]) => ({
            value,
            label,
          }))}
        />
      </Form.Item>
      {isEducationBelow(education, required) && (
        <Form.Item>
          <Alert type="warning" showIcon title={EDUCATION_WARNING} />
        </Form.Item>
      )}
      <Typography.Title level={2}>Самооценка</Typography.Title>
      {!matrixId && (
        <Typography.Paragraph type="secondary">
          Компетенции появятся, когда вы выберете подразделение и трек.
        </Typography.Paragraph>
      )}
      {matrixId && isTargetLoading && <Skeleton active />}
      {scoredCompetencies.map((competency) => (
        <Form.Item
          key={competency.id}
          name={["scores", competency.id]}
          label={competency.name}
          rules={[{ required: true, message: "Выберите балл" }]}
        >
          <Select
            placeholder="Выберите балл"
            options={competency.scalePoints.map((point) => ({
              value: point.score,
              label: scalePointLabel(point),
            }))}
          />
        </Form.Item>
      ))}
      <Typography.Title level={2}>Оценивающие</Typography.Title>
      <Form.Item name="raterEmployeeIds" extra={RATERS_HINT}>
        <RatersField
          initial={application?.raters ?? []}
          candidates={candidates}
          excludedIds={candidateEmployeeId ? [candidateEmployeeId] : []}
          isSearching={isSearching}
          isLocked={isSubmitting}
          onSearch={onSearch}
        />
      </Form.Item>
      <Flex gap="small">
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          {application ? "Сохранить заявку" : "Подать заявку"}
        </Button>
      </Flex>
    </Form>
  );
}
