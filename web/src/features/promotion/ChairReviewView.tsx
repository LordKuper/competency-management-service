import {
  Button,
  Col,
  Flex,
  Form,
  Radio,
  Row,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { Panel } from "./Panel";
import type {
  PromotionDecision,
  RankingItem,
  Review,
  ReviewInput,
} from "./promotionApi";
import {
  DECISION_TAGS,
  formatPercent,
  monthsBetween,
  NO_VALUE,
  raterCountText,
} from "./promotionTexts";
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
import { ScaleChoice } from "./ScaleChoice";
import { ThresholdBar } from "./ThresholdBar";

interface ReviewFormValues {
  finalScores: Record<string, number>;
  decision?: PromotionDecision;
}

const DECISION_OPTIONS = (
  Object.keys(DECISION_TAGS) as PromotionDecision[]
).map((value) => ({ value, label: DECISION_TAGS[value].label }));

interface ChairReviewViewProps {
  review: Review;
  /** The candidates of the commission in the order of the ranking, so the chair can move between them. */
  ranking: readonly RankingItem[];
  /** Saves the final scores and the decision; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: ReviewInput) => Promise<unknown>;
  onOpen: (applicationId: string) => void;
}

/** The card in which the chair enters the final score of every competency, with the circular score, its change against the last decided application and the artifacts beside it, and records the decision of the commission. */
export function ChairReviewView({
  review,
  ranking,
  onSubmit,
  onOpen,
}: ChairReviewViewProps) {
  const [form] = Form.useForm<ReviewFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const artifactRows = useArtifactRows();
  const scaled = review.competencies.filter(
    (competency) => competency.scalePoints.length > 0,
  );
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit({
      finalScores: scaled.flatMap(({ competencyId }) => {
        const score = values.finalScores[competencyId];
        return score === undefined ? [] : [{ competencyId, score }];
      }),
      decision: values.decision ?? null,
    });
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });

  const position = ranking.findIndex(
    (item) => item.applicationId === review.id,
  );
  const previous = position > 0 ? ranking[position - 1] : undefined;
  const next = position >= 0 ? ranking[position + 1] : undefined;
  const { target, formal } = review;

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
        </Flex>
      ),
    },
    {
      title: "Нужно",
      dataIndex: "requiredScore",
      align: "right",
      render: (score: number | null) => score ?? NO_VALUE,
    },
    {
      title: "Само",
      dataIndex: "selfScore",
      align: "right",
      render: (score: number | null) => score ?? NO_VALUE,
    },
    {
      title: "Круговая",
      key: "circular",
      align: "right",
      render: (_, competency) => (
        <Flex vertical align="end">
          <CircularText circular={competency.circular} />
          <ChangeText change={competency.change} />
        </Flex>
      ),
    },
    {
      title: "Артефакты",
      key: "artifacts",
      render: (_, competency) => (
        <ArtifactsToggle
          competency={competency}
          isCompact
          isOpen={artifactRows.isOpen(competency.competencyId)}
          onToggle={() => artifactRows.toggle(competency.competencyId)}
        />
      ),
    },
    {
      title: "Итог",
      key: "finalScore",
      render: (_, competency) =>
        competency.scalePoints.length > 0 ? (
          <Form.Item
            name={["finalScores", competency.competencyId]}
            style={{ margin: 0 }}
            rules={[{ required: true, message: "Выберите балл" }]}
          >
            <ScaleChoice
              name={competency.name}
              points={competency.scalePoints}
              disabled={isSubmitting}
            />
          </Form.Item>
        ) : (
          NO_VALUE
        ),
    },
  ];

  return (
    <Form
      form={form}
      name="promotion-review"
      layout="vertical"
      disabled={isSubmitting}
      initialValues={{
        finalScores: Object.fromEntries(
          scaled.flatMap((competency) => {
            const score = competency.finalScore ?? competency.defaultFinalScore;
            return score === null ? [] : [[competency.competencyId, score]];
          }),
        ),
        decision: review.decision ?? undefined,
      }}
      onFinish={submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      <Flex vertical gap="large">
        <Flex justify="space-between" align="end" gap="middle" wrap>
          <Flex vertical>
            <Typography.Text type="secondary">
              {target.trackName} · {target.unitName} ·{" "}
              {review.formalFailures.length === 0
                ? "формальная проверка пройдена"
                : "формальная проверка не пройдена"}{" "}
              · {raterCountText(review.raterCounts.total)}
              {formal.hiredOrPromotedOn &&
                ` · с последнего перехода ${monthsBetween(formal.hiredOrPromotedOn, review.session.resultsPublishedOn)} мес.`}
            </Typography.Text>
            <Button type="link" style={{ padding: 0 }} onClick={showHistory}>
              Прошлые заявки: {review.history.length} с решением
            </Button>
          </Flex>
          <Flex align="center" gap="small" data-tour="review-nav">
            {position >= 0 && (
              <Typography.Text type="secondary">
                {position + 1} из {ranking.length}
              </Typography.Text>
            )}
            <Button
              disabled={!previous || isSubmitting}
              onClick={() => previous && onOpen(previous.applicationId)}
            >
              Предыдущий
            </Button>
            <Button
              disabled={!next || isSubmitting}
              onClick={() => next && onOpen(next.applicationId)}
            >
              Следующий
            </Button>
          </Flex>
        </Flex>
        <Row gutter={[24, 24]}>
          <Col xs={24} lg={16}>
            <Flex vertical gap="large">
              <Panel title="Итоговые оценки" tour="review-scores">
                <Flex vertical gap="middle">
                  <Typography.Text type="secondary">
                    По умолчанию — округлённая круговая оценка. ▲▼ — изменение
                    круговой оценки к прошлой заявке с решением.
                  </Typography.Text>
                  <Table<ReviewCompetency>
                    rowKey="competencyId"
                    columns={columns}
                    dataSource={[...review.competencies]}
                    pagination={false}
                    scroll={{ x: "max-content" }}
                    expandable={artifactRows.expandable}
                  />
                </Flex>
              </Panel>
              <HistoryPanel review={review} />
            </Flex>
          </Col>
          <Col xs={24} lg={8}>
            <Flex vertical gap="large">
              <Panel title="Итоговый балл" tour="review-rating" level={3}>
                <Flex vertical gap="small">
                  <Flex align="baseline" gap="small" wrap>
                    <Typography.Title level={1} style={{ margin: 0 }}>
                      {formatPercent(review.rating)}
                    </Typography.Title>
                    {review.threshold !== null && (
                      <Typography.Text type="secondary">
                        порог {formatPercent(review.threshold)}
                      </Typography.Text>
                    )}
                  </Flex>
                  <ThresholdBar
                    value={review.rating}
                    threshold={review.threshold}
                    label={`${formatPercent(review.rating)}${review.threshold === null ? "" : `, порог ${formatPercent(review.threshold)}`}`}
                  />
                  <Flex>
                    <ReadinessTag isReady={review.isReady} />
                  </Flex>
                  <RatingBasis
                    isRatingFromFinalScores={review.isRatingFromFinalScores}
                  />
                  <Typography.Text type="secondary">
                    Пересчитывается при сохранении итоговых оценок.
                  </Typography.Text>
                </Flex>
              </Panel>
              <Panel title="Решение комиссии" tour="review-decision" level={3}>
                <Flex vertical gap="middle" align="start">
                  <Form.Item name="decision" style={{ margin: 0 }}>
                    <Radio.Group
                      orientation="vertical"
                      options={DECISION_OPTIONS}
                    />
                  </Form.Item>
                  <Button
                    type="primary"
                    htmlType="submit"
                    loading={isSubmitting}
                  >
                    Сохранить оценки и решение
                  </Button>
                  <Typography.Text type="secondary">
                    Кандидат увидит решение после публикации результатов. До тех
                    пор его можно изменить.
                  </Typography.Text>
                </Flex>
              </Panel>
            </Flex>
          </Col>
        </Row>
      </Flex>
    </Form>
  );
}
