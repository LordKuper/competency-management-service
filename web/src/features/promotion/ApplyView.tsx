import {
  Alert,
  App,
  Button,
  Flex,
  Form,
  Select,
  Skeleton,
  Typography,
} from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { EDUCATION_LABELS, type EducationLevel } from "../career/matrixLabels";
import {
  ArtifactFields,
  type ArtifactItems,
  artifactItemsOf,
  artifactsOf,
} from "./ArtifactFields";
import type { EmployeeOption } from "./CommissionView";
import { isEducationBelow } from "./educationRule";
import type {
  ApplicationInput,
  ArtifactInput,
  OpenWave,
  PromotionApplication,
  RaterEntry,
  TargetForm,
} from "./promotionApi";
import {
  EDUCATION_WARNING,
  formatDate,
  RATERS_HINT,
  scalePointLabel,
} from "./promotionTexts";
import { RatersField } from "./RatersField";
import { normalizedEmail } from "./raterEmail";

const DRAFT_FIELDS = ["matrixId", "levelId", "education"];

interface ApplyFormValues {
  matrixId: string;
  levelId: string;
  education: EducationLevel;
  scores: Record<string, number>;
  raters: RaterEntry[];
  items: ArtifactItems;
}

/** What the form holds, ready to be saved: the draft itself and its artifacts. */
export interface ApplicationDraft {
  input: ApplicationInput;
  artifacts: ArtifactInput[];
}

interface ApplyViewProps {
  wave: OpenWave;
  /** The draft being changed; omitted before the first save. */
  application?: PromotionApplication;
  /** The levels and competencies of the chosen target; omitted until one is chosen and loaded. */
  targetForm?: TargetForm;
  isTargetLoading: boolean;
  /** Employees the last search found. */
  candidates: readonly EmployeeOption[];
  /** The viewer, who cannot rate themselves. */
  candidateEmployeeId?: string;
  isSearching: boolean;
  /** Whether another action on the draft, such as its deletion, is under way. */
  isBusy: boolean;
  onSearch: (text: string) => void;
  /** Called when another unit and track is chosen, so the competencies of the new target are loaded. */
  onTargetChange: (matrixId: string) => void;
  /** Saves the draft; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSaveDraft: (draft: ApplicationDraft) => Promise<unknown>;
  /** Saves the draft and files it; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSend: (draft: ApplicationDraft) => Promise<unknown>;
  /** Asks to delete the draft; offered only for a draft that is saved. */
  onDelete?: () => void;
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
    raters: (application.raters ?? []).map((rater) => ({
      employeeId: rater.employeeId,
      email: rater.email,
    })),
    items: artifactItemsOf(
      (application.artifacts ?? []).map((artifact) => ({
        competencyId: artifact.competencyId,
        competencyName: artifact.competencyName,
        links: artifact.links,
      })),
    ),
  };
}

/** The draft of an application for promotion: the target, the education, the self-assessment by competency, the people who rate the candidate and the artifacts given in advance. */
export function ApplyView({
  wave,
  application,
  targetForm,
  isTargetLoading,
  candidates,
  candidateEmployeeId,
  isSearching,
  isBusy,
  onSearch,
  onTargetChange,
  onSaveDraft,
  onSend,
  onDelete,
}: ApplyViewProps) {
  const { modal } = App.useApp();
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
  const isEducationLow = isEducationBelow(education, required);
  const [applicationsEnd] = wave.endsOn;

  function draftOf(values: ApplyFormValues): ApplicationDraft {
    return {
      input: {
        matrixId: values.matrixId,
        levelId: values.levelId,
        education: values.education,
        selfScores: scoredCompetencies.flatMap((competency) => {
          const score = values.scores?.[competency.id];
          return score === undefined
            ? []
            : [{ competencyId: competency.id, score }];
        }),
        raters: (values.raters ?? []).map((rater) => ({
          employeeId: rater.employeeId,
          email: normalizedEmail(rater.email),
        })),
      },
      artifacts: artifactsOf(
        values.items,
        scoredCompetencies.map((competency) => competency.id),
      ),
    };
  }

  function confirmSending(): Promise<boolean> {
    return new Promise((resolve) => {
      modal.confirm({
        title: "Отправить заявку?",
        content: isEducationLow
          ? `${EDUCATION_WARNING} После отправки заявку нельзя изменить, можно только отозвать.`
          : "После отправки заявку нельзя изменить, можно только отозвать.",
        okText: "Отправить заявку",
        cancelText: "Вернуться к заявке",
        onOk: () => resolve(true),
        onCancel: () => resolve(false),
      });
    });
  }

  const draft = useFormSubmit(form, async () => {
    await form.validateFields(DRAFT_FIELDS);
    await onSaveDraft(draftOf(form.getFieldsValue(true)));
    setIsDirty(false);
  });
  const send = useFormSubmit(form, async (values) => {
    if (!(await confirmSending())) return;
    await onSend(draftOf(values));
    setIsDirty(false);
  });
  const isSubmitting = draft.isSubmitting || send.isSubmitting;
  const isLocked = isSubmitting || isBusy;
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });

  return (
    <Form
      form={form}
      name="promotion-application"
      layout="vertical"
      disabled={isLocked}
      initialValues={initialValuesOf(application)}
      onFinish={send.submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      <Form.Item>
        <Alert
          type="info"
          showIcon
          title="Пока заявка не отправлена, её видите только вы"
          description={
            applicationsEnd &&
            `Отправьте заявку до ${formatDate(applicationsEnd)}: неотправленный черновик в волну не попадёт.`
          }
        />
      </Form.Item>
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
            form.setFieldValue("items", {});
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
      {isEducationLow && (
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
      <Form.Item name="raters" extra={RATERS_HINT}>
        <RatersField
          saved={application?.raters ?? []}
          candidates={candidates}
          excludedIds={candidateEmployeeId ? [candidateEmployeeId] : []}
          isSearching={isSearching}
          isLocked={isLocked}
          onSearch={onSearch}
        />
      </Form.Item>
      {scoredCompetencies.length > 0 && (
        <>
          <Typography.Title level={2}>Артефакты</Typography.Title>
          <Typography.Paragraph type="secondary">
            Необязательно: можно приложить описание и ссылки уже сейчас.
          </Typography.Paragraph>
          <ArtifactFields
            competencies={scoredCompetencies.map((competency) => ({
              competencyId: competency.id,
              competencyName: competency.name,
            }))}
            mandatoryIds={[]}
          />
        </>
      )}
      <Form.Item>
        <Flex gap="small" wrap>
          <Button
            type="primary"
            htmlType="submit"
            loading={send.isSubmitting}
            disabled={isLocked}
          >
            Отправить заявку
          </Button>
          <Button
            loading={draft.isSubmitting}
            disabled={isLocked}
            onClick={() => void draft.submit(form.getFieldsValue(true))}
          >
            Сохранить черновик
          </Button>
          {onDelete && (
            <Button danger disabled={isLocked} onClick={onDelete}>
              Удалить черновик
            </Button>
          )}
        </Flex>
      </Form.Item>
    </Form>
  );
}
