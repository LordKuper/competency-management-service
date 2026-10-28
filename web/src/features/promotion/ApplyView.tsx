import {
  CheckCircleOutlined,
  ClockCircleOutlined,
  MinusCircleOutlined,
} from "@ant-design/icons";
import {
  Alert,
  App,
  Button,
  Col,
  Flex,
  Form,
  Row,
  Select,
  Skeleton,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
  theme,
} from "antd";
import { type ReactNode, useEffect, useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { EDUCATION_LABELS, type EducationLevel } from "../career/matrixLabels";
import {
  ArtifactEditor,
  type ArtifactItems,
  artifactCountOf,
  artifactItemsOf,
  artifactsOf,
} from "./ArtifactFields";
import { type BlockRow, blockColumns, blockRows } from "./BlockRows";
import type { EmployeeOption } from "./CommissionView";
import { isEducationBelow } from "./educationRule";
import { Panel } from "./Panel";
import { PreliminaryRatingPanel } from "./PreliminaryRatingPanel";
import type {
  ApplicationInput,
  ArtifactInput,
  OpenWave,
  PromotionApplication,
  RaterEntry,
  TargetForm,
} from "./promotionApi";
import {
  EDUCATION_NOTE,
  EDUCATION_WARNING,
  formatDate,
  NO_VALUE,
  RATERS_HINT,
} from "./promotionTexts";
import { RatersField } from "./RatersField";
import { normalizedEmail } from "./raterEmail";
import { type ScaleAnswer, ScaleChoice } from "./ScaleChoice";
import { StageSteps } from "./StageSteps";

const DRAFT_FIELDS = ["matrixId", "levelId", "education"];

type ScoredCompetency = TargetForm["competencies"][number];

interface ApplyFormValues {
  matrixId: string;
  levelId: string;
  education: EducationLevel;
  scores: Record<string, ScaleAnswer>;
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

interface CheckItem {
  label: string;
  state: "done" | "pending" | "optional";
}

function CheckList({ items }: { items: readonly CheckItem[] }) {
  const { token } = theme.useToken();
  const icons: Record<CheckItem["state"], ReactNode> = {
    done: <CheckCircleOutlined style={{ color: token.colorSuccess }} />,
    pending: <ClockCircleOutlined style={{ color: token.colorWarning }} />,
    optional: (
      <MinusCircleOutlined style={{ color: token.colorTextTertiary }} />
    ),
  };
  return (
    <Flex vertical gap="small" component="ul" style={{ margin: 0, padding: 0 }}>
      {items.map((item) => (
        <Flex
          key={item.label}
          component="li"
          gap="small"
          align="start"
          style={{ listStyle: "none" }}
        >
          <span aria-hidden>{icons[item.state]}</span>
          <span>{item.label}</span>
        </Flex>
      ))}
    </Flex>
  );
}

/** The draft of an application for a career transition: the target, the education, the self-assessment with artifacts by competency, the people who rate the candidate, and beside them the rating the saved self-assessment earns and what is left to do before sending. */
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
  const [openIds, setOpenIds] = useState<readonly string[]>([]);
  const matrixId = Form.useWatch("matrixId", form);
  const levelId = Form.useWatch("levelId", form);
  const education = Form.useWatch("education", form);
  const scores = Form.useWatch("scores", form);
  const items = Form.useWatch("items", form);
  const raters = Form.useWatch("raters", form);

  const targets = wave.units.flatMap((unit) =>
    unit.targets.map((target) => ({ unit, target })),
  );
  const scoredCompetencies = (targetForm?.competencies ?? []).filter(
    (competency) => competency.scalePoints.length > 0,
  );
  const required = targetForm?.levels.find(
    (level) => level.id === levelId,
  )?.requiredEducation;
  const isEducationLow = isEducationBelow(education, required);
  const [applicationsEnd] = wave.endsOn;
  const scoredCount = scoredCompetencies.filter(
    (competency) => scores?.[competency.id] !== undefined,
  ).length;
  const artifactCompetencyCount = scoredCompetencies.filter(
    (competency) => artifactCountOf(items, competency.id) > 0,
  ).length;

  useEffect(() => {
    if (targetForm?.education && !form.getFieldValue("education")) {
      form.setFieldValue("education", targetForm.education);
    }
  }, [form, targetForm]);

  function draftOf(values: ApplyFormValues): ApplicationDraft {
    return {
      input: {
        matrixId: values.matrixId,
        levelId: values.levelId,
        education: values.education,
        selfScores: scoredCompetencies.flatMap((competency) => {
          const score = values.scores?.[competency.id];
          return typeof score === "number"
            ? [{ competencyId: competency.id, score }]
            : [];
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

  function toggleArtifacts(competencyId: string) {
    setOpenIds((current) =>
      current.includes(competencyId)
        ? current.filter((id) => id !== competencyId)
        : [...current, competencyId],
    );
  }

  const columns: TableColumnsType<ScoredCompetency> = [
    {
      title: "Компетенция",
      key: "name",
      render: (_, competency) => (
        <Flex vertical align="start">
          <span>{competency.name}</span>
          {competency.type === "Critical" && (
            <Tag color="warning">Критическая</Tag>
          )}
        </Flex>
      ),
    },
    {
      title: "Нужно",
      key: "required",
      align: "right",
      render: (_, competency) =>
        competency.requirements.find(
          (requirement) => requirement.levelId === levelId,
        )?.requiredScore ?? NO_VALUE,
    },
    {
      title: "Самооценка",
      key: "score",
      render: (_, competency) => (
        <Form.Item
          name={["scores", competency.id]}
          style={{ margin: 0 }}
          rules={[{ required: true, message: "Выберите балл" }]}
        >
          <ScaleChoice
            name={competency.name}
            points={competency.scalePoints}
            disabled={isLocked}
          />
        </Form.Item>
      ),
    },
    {
      title: "Артефакты",
      key: "artifacts",
      render: (_, competency) => {
        const count = artifactCountOf(items, competency.id);
        const isOpen = openIds.includes(competency.id);
        return (
          <Flex align="center" gap="small" wrap>
            {count > 0 && <Tag color="success">Приложено {count}</Tag>}
            <Button
              type="link"
              aria-expanded={isOpen}
              aria-label={`Артефакты: ${competency.name}`}
              onClick={() => toggleArtifacts(competency.id)}
            >
              {isOpen ? "Скрыть" : count > 0 ? "Изменить" : "Добавить"}
            </Button>
          </Flex>
        );
      },
    },
  ];

  const checks: CheckItem[] = [
    { label: "Цель", state: matrixId && levelId ? "done" : "pending" },
    { label: "Образование", state: education ? "done" : "pending" },
    {
      label: `Самооценка: ${scoredCount} из ${scoredCompetencies.length}`,
      state:
        scoredCompetencies.length > 0 &&
        scoredCount === scoredCompetencies.length
          ? "done"
          : "pending",
    },
    {
      label: `Оценивающие: выбрано ${raters?.length ?? 0}`,
      state: (raters?.length ?? 0) > 0 ? "done" : "pending",
    },
    {
      label: `Артефакты — по желанию: ${artifactCompetencyCount} из ${scoredCompetencies.length}`,
      state: "optional",
    },
  ];

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
      <Flex vertical gap="large">
        <StageSteps stage={1} endsOn={wave.endsOn} orientation="horizontal" />
        <Row gutter={[24, 24]}>
          <Col xs={24} lg={16}>
            <Flex vertical gap="large">
              <Panel title="1. Цель">
                <Row gutter={16}>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="matrixId"
                      label="Подразделение и трек"
                      rules={[
                        {
                          required: true,
                          message: "Выберите подразделение и трек",
                        },
                      ]}
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
                          setOpenIds([]);
                          onTargetChange(next);
                        }}
                      />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="levelId"
                      label="Уровень"
                      rules={[{ required: true, message: "Выберите уровень" }]}
                    >
                      <Select
                        placeholder="Выберите уровень"
                        disabled={!targetForm}
                        loading={isTargetLoading}
                        options={targetForm?.levels.map((level) => ({
                          value: level.id,
                          label: level.name,
                        }))}
                      />
                    </Form.Item>
                  </Col>
                </Row>
              </Panel>
              <Panel title="2. Образование">
                <Form.Item
                  name="education"
                  label="Уровень образования"
                  extra={EDUCATION_NOTE}
                  rules={[{ required: true, message: "Укажите образование" }]}
                >
                  <Select
                    placeholder="Выберите уровень образования"
                    options={Object.entries(EDUCATION_LABELS).map(
                      ([value, label]) => ({ value, label }),
                    )}
                  />
                </Form.Item>
                {isEducationLow && (
                  <Alert type="warning" showIcon title={EDUCATION_WARNING} />
                )}
              </Panel>
              <Panel
                title="3. Самооценка и артефакты"
                extra={
                  scoredCompetencies.length > 0 && (
                    <Typography.Text type="secondary">
                      Оценено {scoredCount} из {scoredCompetencies.length}
                    </Typography.Text>
                  )
                }
              >
                <Flex vertical gap="middle">
                  <Typography.Text type="secondary">
                    Критическая компетенция должна быть оценена не ниже балла из
                    столбца «Нужно», иначе перехода не будет. Остальные
                    компетенции входят в рейтинг, но нехватку по ним могут
                    восполнить другие компетенции. Артефакты можно приложить
                    сейчас или после круговой оценки.
                  </Typography.Text>
                  {!matrixId && (
                    <Typography.Text type="secondary">
                      Компетенции появятся, когда вы выберете подразделение и
                      трек.
                    </Typography.Text>
                  )}
                  {matrixId && isTargetLoading && <Skeleton active />}
                  {scoredCompetencies.length > 0 && (
                    <Table<BlockRow<ScoredCompetency>>
                      rowKey="key"
                      columns={blockColumns(columns)}
                      dataSource={blockRows(
                        scoredCompetencies,
                        (competency) => competency.block,
                        (competency) => competency.id,
                      )}
                      pagination={false}
                      scroll={{ x: "max-content" }}
                      expandable={{
                        showExpandColumn: false,
                        expandedRowKeys: [...openIds],
                        rowExpandable: (row) => row.item !== undefined,
                        expandedRowRender: ({ item }) =>
                          item && (
                            <ArtifactEditor
                              competencyId={item.id}
                              competencyName={item.name}
                            />
                          ),
                      }}
                    />
                  )}
                </Flex>
              </Panel>
              <Panel
                title="4. Оценивающие"
                extra={
                  application?.raterCounts && (
                    <Typography.Text type="secondary">
                      В сохранённом черновике {application.raterCounts.total} из{" "}
                      {application.raterCounts.requiredTotal}, из других
                      подразделений {application.raterCounts.fromOtherUnits} из{" "}
                      {application.raterCounts.requiredFromOtherUnits}
                    </Typography.Text>
                  )
                }
              >
                <Form.Item name="raters" extra={RATERS_HINT}>
                  <RatersField
                    saved={application?.raters ?? []}
                    candidates={candidates}
                    excludedIds={
                      candidateEmployeeId ? [candidateEmployeeId] : []
                    }
                    isSearching={isSearching}
                    isLocked={isLocked}
                    onSearch={onSearch}
                  />
                </Form.Item>
              </Panel>
            </Flex>
          </Col>
          <Col xs={24} lg={8}>
            <Flex vertical gap="large">
              <PreliminaryRatingPanel
                rating={application?.preliminaryRating ?? null}
              />
              <Panel title="Перед отправкой" level={3}>
                <Flex vertical gap="middle">
                  <CheckList items={checks} />
                  <Alert
                    type="info"
                    title="Черновик видите только вы"
                    description={
                      applicationsEnd
                        ? `Отправьте до ${formatDate(applicationsEnd)}: неотправленный черновик в сессию не попадёт. После отправки заявку нельзя изменить — только отозвать и подать новую.`
                        : "После отправки заявку нельзя изменить — только отозвать и подать новую."
                    }
                  />
                  <Flex vertical gap="small" align="start">
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
                      onClick={() =>
                        void draft.submit(form.getFieldsValue(true))
                      }
                    >
                      Сохранить черновик
                    </Button>
                    {onDelete && (
                      <Button
                        type="link"
                        danger
                        disabled={isLocked}
                        onClick={onDelete}
                      >
                        Удалить черновик
                      </Button>
                    )}
                  </Flex>
                </Flex>
              </Panel>
            </Flex>
          </Col>
        </Row>
      </Flex>
    </Form>
  );
}
