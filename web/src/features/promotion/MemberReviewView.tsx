import {
  Alert,
  Button,
  Col,
  Flex,
  Row,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import type { ReactNode } from "react";
import { EDUCATION_LABELS } from "../career/matrixLabels";
import { DeadlineNote } from "./DeadlineNote";
import { Panel } from "./Panel";
import type { RankingItem, Review } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  DECISION_TAGS,
  FORMAL_FAILURE_TEXTS,
  formatDate,
  formatPercent,
  monthsBetween,
  NO_VALUE,
  raterCountText,
  STAGE,
} from "./promotionTexts";
import { RankingView } from "./RankingView";
import { ReadinessTag } from "./ReadinessTag";
import {
  ArtifactsToggle,
  ChangeText,
  CircularText,
  HistoryPanel,
  RatingBasis,
  type ReviewCompetency,
  showHistory,
  useArtifactRows,
} from "./ReviewParts";
import { ScaleValue } from "./ScaleChoice";

function Fact({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Flex vertical>
      <Typography.Text type="secondary">{label}</Typography.Text>
      {children}
    </Flex>
  );
}

interface MemberReviewViewProps {
  review: Review;
  /** The candidates of the commission in the order of the ranking, with the viewed one marked. */
  ranking: readonly RankingItem[];
  isRankingLoading: boolean;
  /** Whether the viewer is the chair, who has no scores to enter now; otherwise the card tells that the chair enters them. */
  isChair: boolean;
}

/** The card of a candidate as a member of the commission reads it, under the ranking of the unit: the facts, every competency with the self, circular and final scores, the change against the last decided application and the artifacts. */
export function MemberReviewView({
  review,
  ranking,
  isRankingLoading,
  isChair,
}: MemberReviewViewProps) {
  const artifactRows = useArtifactRows();
  const { target, formal } = review;
  const statusTag = APPLICATION_STATUS_TAGS[review.status];

  const columns: TableColumnsType<ReviewCompetency> = [
    {
      title: "Компетенция",
      key: "name",
      render: (_, competency) => (
        <Flex align="center" gap="small" wrap>
          <span>{competency.name}</span>
          {competency.type === "Critical" && (
            <Tag color="warning">Критическая</Tag>
          )}
          {competency.isArtifactMandatory && !competency.artifact && (
            <Tag color="warning">Нет артефакта</Tag>
          )}
        </Flex>
      ),
    },
    {
      title: "Требуется",
      dataIndex: "requiredScore",
      align: "right",
      render: (score: number | null) => score ?? NO_VALUE,
    },
    {
      title: "Самооценка",
      key: "selfScore",
      align: "right",
      render: (_, competency) => (
        <ScaleValue
          name={competency.name}
          points={competency.scalePoints}
          score={competency.selfScore}
        />
      ),
    },
    {
      title: "Круговая",
      key: "circular",
      align: "right",
      render: (_, competency) => (
        <CircularText circular={competency.circular} hasCount />
      ),
    },
    {
      title: "К прошлой заявке",
      key: "change",
      render: (_, competency) => (
        <ChangeText change={competency.change} hasPrevious />
      ),
    },
    {
      title: "Итог",
      key: "finalScore",
      align: "right",
      render: (_, competency) => (
        <Flex vertical align="end">
          <ScaleValue
            name={competency.name}
            points={competency.scalePoints}
            score={competency.finalScore}
          />
          {competency.isMet === false && (
            <Tag color="error">Ниже требуемого</Tag>
          )}
        </Flex>
      ),
    },
    {
      title: "Артефакты",
      key: "artifacts",
      render: (_, competency) => (
        <ArtifactsToggle
          competency={competency}
          isOpen={artifactRows.isOpen(competency.competencyId)}
          onToggle={() => artifactRows.toggle(competency.competencyId)}
        />
      ),
    },
  ];

  return (
    <Flex vertical gap="large">
      <DeadlineNote
        session={review.session}
        label="Решения до"
        stage={STAGE.review}
      />
      <Panel title="Рейтинг кандидатов" tour="review-ranking">
        <RankingView
          items={ranking}
          isLoading={isRankingLoading}
          currentId={review.id}
        />
      </Panel>
      <Panel
        title={`${review.candidateName} — ${target.levelName}`}
        tour="review-card"
        extra={<Tag color={statusTag.color}>{statusTag.label}</Tag>}
      >
        <Flex vertical gap="middle">
          {!isChair && (
            <Typography.Text type="secondary">
              Итоговые оценки и решение вносит председатель комиссии
            </Typography.Text>
          )}
          {review.artifactsMissing && (
            <Alert
              type="warning"
              showIcon
              title="Обязательные артефакты не предоставлены"
            />
          )}
          {review.meetsAfterRating === false && (
            <Alert
              type="warning"
              showIcon
              title="По средним баллам круговой оценки обязательные требования не выполнены"
            />
          )}
          <Row gutter={[24, 16]}>
            <Col xs={12} lg={4}>
              <Fact label="Готовность">
                <Typography.Title level={2} style={{ margin: 0 }}>
                  {formatPercent(review.rating)}
                </Typography.Title>
                <ReadinessTag isReady={review.isReady} />
                {review.threshold !== null && (
                  <Typography.Text type="secondary">
                    порог {formatPercent(review.threshold)}
                  </Typography.Text>
                )}
                <RatingBasis
                  isRatingFromFinalScores={review.isRatingFromFinalScores}
                />
              </Fact>
            </Col>
            <Col xs={12} lg={4}>
              <Fact label="Формальная проверка">
                <Flex>
                  {review.formalFailures.length === 0 ? (
                    <Tag color="success">Пройдена</Tag>
                  ) : (
                    <Tag color="error">Не пройдена</Tag>
                  )}
                </Flex>
                <Typography.Text type="secondary">
                  {review.formalFailures.length === 0
                    ? formal.hiredOrPromotedOn
                      ? `с последнего перехода ${monthsBetween(formal.hiredOrPromotedOn, review.session.resultsPublishedOn)} мес.`
                      : "формальные признаки пройдены"
                    : review.formalFailures
                        .map((failure) => FORMAL_FAILURE_TEXTS[failure])
                        .join("; ")}
                </Typography.Text>
              </Fact>
            </Col>
            <Col xs={12} lg={4}>
              <Fact label="Образование">
                <span>
                  {review.education
                    ? EDUCATION_LABELS[review.education]
                    : NO_VALUE}
                </span>
              </Fact>
            </Col>
            <Col xs={12} lg={4}>
              <Fact label="Круговая оценка">
                <span>{raterCountText(review.raterCounts.total)}</span>
                <Typography.Text type="secondary">
                  из них {review.raterCounts.fromOtherUnits} из других
                  подразделений
                </Typography.Text>
              </Fact>
            </Col>
            <Col xs={12} lg={4}>
              <Fact label="Решение">
                {review.decision ? (
                  <Flex vertical align="start">
                    <Tag color={DECISION_TAGS[review.decision].color}>
                      {DECISION_TAGS[review.decision].label}
                    </Tag>
                    {review.decidedByName && review.decidedAt && (
                      <Typography.Text type="secondary">
                        {review.decidedByName}, {formatDate(review.decidedAt)}
                      </Typography.Text>
                    )}
                  </Flex>
                ) : (
                  <Typography.Text type="secondary">
                    {review.stage <= STAGE.review
                      ? "Ждёт решения"
                      : APPLICATION_STATUS_TAGS.NoDecision.label}
                  </Typography.Text>
                )}
              </Fact>
            </Col>
            <Col xs={12} lg={4}>
              <Fact label="Прошлые заявки">
                <span>{review.history.length} с решением</span>
                <Button
                  type="link"
                  style={{ padding: 0, justifyContent: "start" }}
                  onClick={showHistory}
                >
                  Смотреть сводку
                </Button>
              </Fact>
            </Col>
          </Row>
          <Table<ReviewCompetency>
            rowKey="competencyId"
            columns={columns}
            dataSource={[...review.competencies]}
            pagination={false}
            scroll={{ x: "max-content" }}
            expandable={artifactRows.expandable}
          />
          <Typography.Text type="secondary">
            В скобках — сколько оценивающих наблюдали компетенцию. «К прошлой
            заявке» — изменение средней круговой оценки по сравнению с последней
            заявкой с решением; «новая» — компетенции в той заявке не было.
          </Typography.Text>
        </Flex>
      </Panel>
      <HistoryPanel review={review} />
    </Flex>
  );
}
