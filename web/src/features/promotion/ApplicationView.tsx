import {
  Alert,
  Button,
  Col,
  Flex,
  List,
  Row,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { ArtifactsView } from "./ArtifactsView";
import type { ArtifactInput, PromotionApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  ARTIFACT_MANDATORY_EXPLANATION,
  DECISION_TAGS,
  EDUCATION_WARNING,
  FORMAL_FAILURE_TEXTS,
  formatDate,
  STAGE,
} from "./promotionTexts";
import { StageSteps } from "./StageSteps";

const WITHDRAWABLE_STATUSES = ["Submitted", "Admitted", "UnderReview"];

const ARTIFACT_STATUSES = ["Admitted", "UnderReview", "Decided"];

type Result = NonNullable<PromotionApplication["result"]>;
type ResultCompetency = Result["competencies"][number];
type SelfScore = PromotionApplication["selfScores"][number];

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

const selfScoreColumns: TableColumnsType<SelfScore> = [
  { title: "Компетенция", dataIndex: "competencyName" },
  { title: "Балл", dataIndex: "score", align: "right" },
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
    <Flex vertical gap="middle">
      <Typography.Title level={2} style={{ margin: 0 }}>
        Результат
      </Typography.Title>
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
}

/** An application as its viewer may see it: the target, the status and stage, and the parts the stage asks the candidate to act on. A filed application cannot be changed, only withdrawn. */
export function ApplicationView({
  application,
  isBusy,
  onWithdraw,
  onMarkFeedback,
  onSaveArtifacts,
}: ApplicationViewProps) {
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
  const mandatoryIds = application.mandatoryArtifactCompetencyIds ?? [];
  const given = new Map(
    (application.artifacts ?? []).map((artifact) => [
      artifact.competencyId,
      artifact,
    ]),
  );
  const hasEducationWarning =
    isCandidate && application.warnings.includes("EducationBelowRequired");

  return (
    <Row gutter={[24, 24]}>
      <Col xs={24} lg={16}>
        <Flex vertical gap="large">
          <Flex vertical gap="small">
            <Typography.Text strong>
              {target.unitName}: {target.trackName}, {target.levelName}
            </Typography.Text>
            <Flex align="center" gap="small" wrap>
              <Tag color={statusTag.color}>{statusTag.label}</Tag>
              {!isCandidate && (
                <Typography.Text type="secondary">
                  {application.candidateName}
                </Typography.Text>
              )}
            </Flex>
          </Flex>
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
          {canWithdraw && (
            <Flex vertical gap="small" align="start">
              {isCandidate && status === "Submitted" && (
                <Typography.Text type="secondary">
                  Отправленную заявку нельзя изменить: можно только отозвать её.
                </Typography.Text>
              )}
              <Button danger disabled={isBusy} onClick={onWithdraw}>
                Отозвать заявку
              </Button>
            </Flex>
          )}
          {application.result && (
            <ResultSection
              result={application.result}
              formalFailures={application.formalFailures}
              status={status}
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
                Обратную связь вам дают вне системы. Когда получите её, отметьте
                это здесь.
              </Typography.Text>
              <Button type="primary" loading={isBusy} onClick={onMarkFeedback}>
                Обратную связь получил
              </Button>
            </Flex>
          )}
          {isCandidate &&
            application.artifacts &&
            (isArtifactStage || application.artifacts.length > 0) && (
              <Flex vertical gap="middle">
                <Typography.Title level={2} style={{ margin: 0 }}>
                  Артефакты
                </Typography.Title>
                {isArtifactStage && application.hasMandatoryArtifacts && (
                  <Alert
                    type="warning"
                    showIcon
                    title="Добавьте артефакты к отмеченным компетенциям"
                    description={ARTIFACT_MANDATORY_EXPLANATION}
                  />
                )}
                <ArtifactsView
                  competencies={application.selfScores.map((score) => ({
                    competencyId: score.competencyId,
                    competencyName: score.competencyName,
                    links: given.get(score.competencyId)?.links,
                  }))}
                  mandatoryIds={mandatoryIds}
                  isEditable={isArtifactStage}
                  onSubmit={onSaveArtifacts}
                />
              </Flex>
            )}
          <Flex vertical gap="middle">
            <Typography.Title level={2} style={{ margin: 0 }}>
              Самооценка
            </Typography.Title>
            <Table<SelfScore>
              rowKey="competencyId"
              columns={selfScoreColumns}
              dataSource={[...application.selfScores]}
              pagination={false}
            />
          </Flex>
          {application.raters && (
            <Flex vertical gap="middle">
              <Typography.Title level={2} style={{ margin: 0 }}>
                Оценивающие
              </Typography.Title>
              <List
                bordered
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
            </Flex>
          )}
        </Flex>
      </Col>
      <Col xs={24} lg={8}>
        <StageSteps stage={stage} />
      </Col>
    </Row>
  );
}
