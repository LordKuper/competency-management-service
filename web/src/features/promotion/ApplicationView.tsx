import {
  Alert,
  App,
  Button,
  Col,
  Descriptions,
  Flex,
  Form,
  List,
  Row,
  Table,
  type TableColumnsType,
  Tag,
  Timeline,
  Typography,
  theme,
} from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { EDUCATION_LABELS } from "../career/matrixLabels";
import {
  ArtifactEditor,
  type ArtifactItems,
  artifactCountOf,
  artifactItemsOf,
  artifactsOf,
} from "./ArtifactFields";
import { Panel } from "./Panel";
import type { ArtifactInput, PromotionApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  ARTIFACT_MANDATORY_EXPLANATION,
  competenciesText,
  DECISION_TAGS,
  EDUCATION_WARNING,
  FORMAL_FAILURE_TEXTS,
  formatDate,
  STAGE,
} from "./promotionTexts";
import { ScaleValue } from "./ScaleChoice";
import { StageSteps } from "./StageSteps";

const WITHDRAWABLE_STATUSES = ["Submitted", "Admitted", "UnderReview"];

const ARTIFACT_STATUSES = ["Admitted", "UnderReview", "Decided"];

type Result = NonNullable<PromotionApplication["result"]>;
type ResultCompetency = Result["competencies"][number];
type SelfScore = PromotionApplication["selfScores"][number];
type Artifact = NonNullable<PromotionApplication["artifacts"]>[number];

interface ArtifactsFormValues {
  items: ArtifactItems;
}

const resultColumns: TableColumnsType<ResultCompetency> = [
  { title: "Компетенция", dataIndex: "competencyName" },
  { title: "Требуется", dataIndex: "requiredScore", align: "right" },
  {
    title: "Итоговый балл",
    dataIndex: "score",
    align: "right",
    render: (score: number | null) => score ?? "—",
  },
  {
    title: "Требование",
    dataIndex: "isMet",
    render: (isMet: boolean) =>
      isMet ? (
        <Tag color="success">Выполнено</Tag>
      ) : (
        <Tag color="error">Не выполнено</Tag>
      ),
  },
];

interface ResultSectionProps {
  result: Result;
  formalFailures: PromotionApplication["formalFailures"];
  status: PromotionApplication["status"];
}

/** What the candidate is told once the results are published: the decision, the formal check and the final scores against the requirements. */
function ResultSection({ result, formalFailures, status }: ResultSectionProps) {
  const hasScores = result.competencies.some(
    (competency) => competency.score !== null,
  );
  const decisionTag = result.decision
    ? DECISION_TAGS[result.decision]
    : APPLICATION_STATUS_TAGS[status];
  return (
    <Panel title="Результат">
      <Flex vertical gap="middle">
        <Flex>
          <Tag color={decisionTag.color}>{decisionTag.label}</Tag>
        </Flex>
        {formalFailures.length === 0 ? (
          <Typography.Text>Формальные признаки пройдены.</Typography.Text>
        ) : (
          <Alert
            type="error"
            showIcon
            title="Формальные признаки не пройдены"
            description={formalFailures
              .map((failure) => FORMAL_FAILURE_TEXTS[failure])
              .join("; ")}
          />
        )}
        {hasScores && (
          <Table<ResultCompetency>
            rowKey="competencyId"
            columns={resultColumns}
            dataSource={[...result.competencies]}
            pagination={false}
            scroll={{ x: "max-content" }}
          />
        )}
      </Flex>
    </Panel>
  );
}

function ArtifactLinks({ artifact }: { artifact?: Artifact }) {
  if (!artifact || artifact.links.length === 0) {
    return <Typography.Text type="secondary">Не приложен</Typography.Text>;
  }
  return (
    <ul style={{ margin: 0, paddingInlineStart: "1em" }}>
      {artifact.links.map((link) => (
        <li key={link.url}>
          <a href={link.url} target="_blank" rel="noreferrer noopener">
            {link.description}
          </a>
        </li>
      ))}
    </ul>
  );
}

type TimelineState = "done" | "now" | "next" | "failed";

interface ProgressStep {
  label: string;
  state: TimelineState;
}

function progressOf(application: PromotionApplication): ProgressStep[] {
  const { stage, status } = application;
  const state = (stageNumber: number): TimelineState =>
    stage > stageNumber ? "done" : stage === stageNumber ? "now" : "next";
  const isRejected = status === "FormalRejected";
  return [
    { label: "Заявка подана", state: "done" },
    isRejected
      ? { label: "Отклонена по формальным признакам", state: "failed" }
      : {
          label:
            stage > STAGE.formalCheck
              ? "Формальная проверка пройдена"
              : "Формальная проверка",
          state: state(STAGE.formalCheck),
        },
    {
      label:
        stage > STAGE.rating ? "Круговая оценка завершена" : "Круговая оценка",
      state: isRejected ? "next" : state(STAGE.rating),
    },
    {
      label:
        stage > STAGE.artifacts
          ? "Сбор артефактов завершён"
          : "Сбор артефактов",
      state: isRejected ? "next" : state(STAGE.artifacts),
    },
    {
      label:
        stage > STAGE.review
          ? "Рассмотрение комиссией завершено"
          : "Рассмотрение комиссией",
      state: isRejected ? "next" : state(STAGE.review),
    },
    {
      label: stage > STAGE.review ? "Результат опубликован" : "Результат",
      state: isRejected || stage <= STAGE.review ? "next" : "done",
    },
  ];
}

function ApplicationProgress({
  application,
}: {
  application: PromotionApplication;
}) {
  const { token } = theme.useToken();
  const colors: Record<TimelineState, string> = {
    done: token.colorSuccess,
    now: token.colorPrimary,
    next: token.colorTextQuaternary,
    failed: token.colorError,
  };
  return (
    <Panel title="Ход заявки" level={3}>
      <Timeline
        items={progressOf(application).map((step) => ({
          color: colors[step.state],
          content:
            step.state === "next" ? (
              <Typography.Text type="secondary">{step.label}</Typography.Text>
            ) : (
              <Typography.Text strong={step.state === "now"}>
                {step.label}
              </Typography.Text>
            ),
        }))}
      />
    </Panel>
  );
}

interface ApplicationViewProps {
  application: PromotionApplication;
  /** Whether a change of the application is being saved, so its actions wait. */
  isBusy: boolean;
  onWithdraw: () => void;
  onMarkFeedback: () => void;
  /** Saves the artifacts; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSaveArtifacts: (artifacts: ArtifactInput[]) => Promise<unknown>;
  /** Sends the application to the commission with the artifacts saved so far. */
  onSendToReview: () => Promise<unknown>;
}

/** An application as its viewer may see it: the target, the status and stage, and the parts the stage asks the candidate to act on. A filed application cannot be changed, only withdrawn; its artifacts can be added until it is sent to the commission. */
export function ApplicationView({
  application,
  isBusy,
  onWithdraw,
  onMarkFeedback,
  onSaveArtifacts,
  onSendToReview,
}: ApplicationViewProps) {
  const { modal } = App.useApp();
  const [form] = Form.useForm<ArtifactsFormValues>();
  const { token } = theme.useToken();
  const [isDirty, setIsDirty] = useState(false);
  const [openIds, setOpenIds] = useState<readonly string[]>([]);
  const items = Form.useWatch("items", form);
  const { stage, status, target } = application;
  const isCandidate = application.roles.includes("Candidate");
  const statusTag = APPLICATION_STATUS_TAGS[status];
  const canWithdraw = isCandidate && WITHDRAWABLE_STATUSES.includes(status);
  const canMarkFeedback =
    isCandidate &&
    stage === STAGE.feedback &&
    (status === "Decided" || status === "NoDecision") &&
    !application.feedbackReceivedAt;
  const isArtifactStage =
    isCandidate &&
    stage === STAGE.artifacts &&
    ARTIFACT_STATUSES.includes(status);
  const isEditable = isArtifactStage && !application.isSentToReview;
  const canSend = isEditable && status === "Admitted";
  const mandatoryIds = application.mandatoryArtifactCompetencyIds ?? [];
  const missingIds = application.missingArtifactCompetencyIds ?? [];
  const given = new Map(
    (application.artifacts ?? []).map((artifact) => [
      artifact.competencyId,
      artifact,
    ]),
  );
  const nameOf = new Map(
    application.selfScores.map((score) => [
      score.competencyId,
      score.competencyName,
    ]),
  );
  const hasEducationWarning =
    isCandidate && application.warnings.includes("EducationBelowRequired");
  const showsArtifacts = isCandidate && application.artifacts !== null;

  const saveArtifacts = useFormSubmit(form, async (values) => {
    await onSaveArtifacts(
      artifactsOf(
        values.items,
        application.selfScores.map((score) => score.competencyId),
      ),
    );
    setIsDirty(false);
  });
  const isSaving = saveArtifacts.isSubmitting;
  useLeaveGuard({ isUnsaved: isDirty, isSaving });

  function confirmSending() {
    const missing = missingIds.map((id) => nameOf.get(id) ?? id);
    modal.confirm({
      title: "Отправить заявку на рассмотрение?",
      content:
        missing.length > 0 ? (
          <Flex vertical>
            <span>
              Обязательные артефакты приложены не ко всем компетенциям, заявку
              всё равно можно отправить. Не хватает:
            </span>
            <ul style={{ margin: 0 }}>
              {missing.map((name) => (
                <li key={name}>{name}</li>
              ))}
            </ul>
            <span>После отправки артефакты изменить нельзя.</span>
          </Flex>
        ) : (
          "После отправки артефакты изменить нельзя."
        ),
      okText: "Отправить на рассмотрение",
      cancelText: "Вернуться к заявке",
      onOk: () => onSendToReview().then(undefined, () => undefined),
    });
  }

  function toggleArtifacts(competencyId: string) {
    setOpenIds((current) =>
      current.includes(competencyId)
        ? current.filter((id) => id !== competencyId)
        : [...current, competencyId],
    );
  }

  const columns: TableColumnsType<SelfScore> = [
    {
      title: "Компетенция",
      key: "name",
      render: (_, score) => score.competencyName,
    },
    {
      title: "Самооценка",
      key: "score",
      render: (_, score) => (
        <ScaleValue
          name={score.competencyName}
          points={[]}
          score={score.score}
        />
      ),
    },
    ...(showsArtifacts
      ? [
          {
            title: "Артефакты",
            key: "artifacts",
            render: (_: unknown, score: SelfScore) => {
              const id = score.competencyId;
              const isMissing = isEditable && missingIds.includes(id);
              if (!isEditable) {
                return <ArtifactLinks artifact={given.get(id)} />;
              }
              const count = artifactCountOf(items, id);
              const isOpen = openIds.includes(id);
              return (
                <Flex align="center" gap="small" wrap>
                  {count > 0 && <Tag color="success">Приложено {count}</Tag>}
                  {isMissing && count === 0 && (
                    <Tag color="warning">Обязателен</Tag>
                  )}
                  <Button
                    type={isMissing && count === 0 ? "primary" : "link"}
                    aria-expanded={isOpen}
                    aria-label={`Артефакты: ${score.competencyName}`}
                    onClick={() => toggleArtifacts(id)}
                  >
                    {isOpen ? "Скрыть" : count > 0 ? "Изменить" : "Добавить"}
                  </Button>
                </Flex>
              );
            },
          },
        ]
      : []),
  ];

  const table = (
    <Table<SelfScore>
      rowKey="competencyId"
      columns={columns}
      dataSource={[...application.selfScores]}
      pagination={false}
      scroll={{ x: "max-content" }}
      onRow={(score) => ({
        style:
          isEditable && missingIds.includes(score.competencyId)
            ? { background: token.colorWarningBg }
            : undefined,
      })}
      expandable={
        isEditable
          ? {
              showExpandColumn: false,
              expandedRowKeys: [...openIds],
              expandedRowRender: (score) => (
                <ArtifactEditor
                  competencyId={score.competencyId}
                  competencyName={score.competencyName}
                />
              ),
            }
          : undefined
      }
    />
  );

  return (
    <Flex vertical gap="large">
      <Flex align="center" justify="space-between" gap="middle" wrap>
        <Flex align="center" gap="small" wrap>
          <Tag color={statusTag.color}>{statusTag.label}</Tag>
          {!isCandidate && (
            <Typography.Text type="secondary">
              {application.candidateName}
            </Typography.Text>
          )}
        </Flex>
        {canWithdraw && (
          <Button danger disabled={isBusy || isSaving} onClick={onWithdraw}>
            Отозвать заявку
          </Button>
        )}
      </Flex>
      <StageSteps stage={stage} orientation="horizontal" />
      {hasEducationWarning && (
        <Alert type="warning" showIcon title={EDUCATION_WARNING} />
      )}
      {status === "FormalRejected" && (
        <Alert
          type="error"
          showIcon
          title="Заявка отклонена по формальным признакам"
          description={application.formalFailures
            .map((failure) => FORMAL_FAILURE_TEXTS[failure])
            .join("; ")}
        />
      )}
      {canSend && application.hasMandatoryArtifacts && (
        <Alert
          type="warning"
          showIcon
          title={`Нужны артефакты к ${competenciesText(mandatoryIds.length)}`}
          description={`${ARTIFACT_MANDATORY_EXPLANATION} Когда будете готовы, нажмите «Отправить на рассмотрение». Если не отправите до конца этапа, заявка уйдёт в комиссию с тем, что приложено.`}
        />
      )}
      {application.feedbackReceivedAt && (
        <Alert
          type="success"
          showIcon
          title={`Получение обратной связи отмечено ${formatDate(application.feedbackReceivedAt)}`}
        />
      )}
      {canMarkFeedback && (
        <Flex vertical gap="small" align="start">
          <Typography.Text>
            Обратную связь вам дают вне системы. Когда получите её, отметьте это
            здесь.
          </Typography.Text>
          <Button type="primary" loading={isBusy} onClick={onMarkFeedback}>
            Обратную связь получил
          </Button>
        </Flex>
      )}
      <Row gutter={[24, 24]}>
        <Col xs={24} lg={16}>
          <Flex vertical gap="large">
            <Panel title="Цель">
              <Descriptions
                size="small"
                column={{ xs: 1, md: 3 }}
                layout="vertical"
                items={[
                  {
                    label: "Подразделение и трек",
                    children: `${target.unitName}: ${target.trackName}`,
                  },
                  { label: "Уровень", children: target.levelName },
                  {
                    label: "Образование",
                    children: application.education
                      ? EDUCATION_LABELS[application.education]
                      : "—",
                  },
                ]}
              />
            </Panel>
            {application.result && (
              <ResultSection
                result={application.result}
                formalFailures={application.formalFailures}
                status={status}
              />
            )}
            <Panel
              title="Самооценка и артефакты"
              extra={
                isCandidate &&
                mandatoryIds.length > 0 && (
                  <Typography.Text type="secondary">
                    Обязательные: {mandatoryIds.length - missingIds.length} из{" "}
                    {mandatoryIds.length}
                  </Typography.Text>
                )
              }
            >
              <Flex vertical gap="middle">
                <Form
                  form={form}
                  name="promotion-artifacts"
                  layout="vertical"
                  disabled={isSaving}
                  initialValues={{
                    items: artifactItemsOf(
                      application.selfScores.map((score) => ({
                        competencyId: score.competencyId,
                        competencyName: score.competencyName,
                        links: given.get(score.competencyId)?.links,
                      })),
                    ),
                  }}
                  onFinish={saveArtifacts.submit}
                  onValuesChange={() => setIsDirty(form.isFieldsTouched())}
                >
                  <Flex vertical gap="middle">
                    {table}
                    {isEditable && (
                      <Flex>
                        <Button
                          htmlType="submit"
                          loading={isSaving}
                          disabled={!isDirty}
                        >
                          Сохранить артефакты
                        </Button>
                      </Flex>
                    )}
                  </Flex>
                </Form>
                <Typography.Text type="secondary">
                  Самооценку и цель после отправки изменить нельзя.
                  {stage === STAGE.submission &&
                    " Нашли ошибку — отзовите заявку и подайте новую до конца приёма заявок."}
                </Typography.Text>
              </Flex>
            </Panel>
            {application.raters && (
              <Panel title="Оценивающие">
                <List
                  size="small"
                  dataSource={[...application.raters]}
                  renderItem={(rater) => (
                    <List.Item>
                      <Flex vertical>
                        <span>{rater.fullName}</span>
                        {!rater.hasAccount && rater.email && (
                          <Typography.Text type="secondary">
                            {rater.email},{" "}
                            {rater.isEmailConfirmed
                              ? "почта подтверждена"
                              : "почта не подтверждена"}
                          </Typography.Text>
                        )}
                      </Flex>
                    </List.Item>
                  )}
                />
              </Panel>
            )}
          </Flex>
        </Col>
        <Col xs={24} lg={8}>
          <Flex vertical gap="large">
            {isArtifactStage && (
              <Panel title="Рассмотрение комиссией" level={3}>
                <Flex vertical gap="small" align="start">
                  {mandatoryIds.length > 0 && (
                    <Typography.Text>
                      Обязательные артефакты:{" "}
                      <b>
                        {mandatoryIds.length - missingIds.length} из{" "}
                        {mandatoryIds.length}
                      </b>
                    </Typography.Text>
                  )}
                  {application.isSentToReview && (
                    <Tag color="success">Отправлена на рассмотрение</Tag>
                  )}
                  {canSend && (
                    <>
                      <Button
                        type="primary"
                        disabled={isDirty || isBusy || isSaving}
                        onClick={confirmSending}
                      >
                        Отправить на рассмотрение
                      </Button>
                      <Typography.Text type="secondary">
                        {isDirty
                          ? "Сначала сохраните артефакты."
                          : "После отправки артефакты изменить нельзя."}
                      </Typography.Text>
                    </>
                  )}
                  {application.isSentToReview && (
                    <Typography.Text type="secondary">
                      Артефакты изменить нельзя.
                    </Typography.Text>
                  )}
                </Flex>
              </Panel>
            )}
            <ApplicationProgress application={application} />
          </Flex>
        </Col>
      </Row>
    </Flex>
  );
}
