import {
  Alert,
  Button,
  Descriptions,
  Flex,
  Form,
  Radio,
  Select,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { type ReactNode, useState } from "react";
import { Link } from "react-router";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { COMPETENCY_TYPES } from "../career/competencyType";
import { EDUCATION_LABELS } from "../career/matrixLabels";
import { ArtifactsView } from "./ArtifactsView";
import type { PromotionDecision, Review, ReviewInput } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  DECISION_TAGS,
  FORMAL_FAILURE_TEXTS,
  formatAverage,
  formatDate,
  formatPercent,
  NO_VALUE,
  STAGE,
  scalePointLabel,
} from "./promotionTexts";
import { ReadinessTag } from "./ReadinessTag";
import { promotionPaths } from "./routes";

type ReviewCompetency = Review["competencies"][number];
type HistoryItem = Review["history"][number];

interface ReviewFormValues {
  finalScores: Record<string, number>;
  decision?: PromotionDecision;
}

interface ReviewViewProps {
  review: Review;
  /** Saves the final scores and the decision; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: ReviewInput) => Promise<unknown>;
}

const EDITABLE_STATUSES: readonly Review["status"][] = [
  "UnderReview",
  "Decided",
];

const DECISION_OPTIONS = (
  Object.keys(DECISION_TAGS) as PromotionDecision[]
).map((value) => ({ value, label: DECISION_TAGS[value].label }));

const SCORE_SELECT_STYLE = { minWidth: "calc(var(--ant-control-height) * 7)" };

async function savesNothing() {
  return undefined;
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Flex vertical gap="middle">
      <Typography.Title level={2} style={{ margin: 0 }}>
        {title}
      </Typography.Title>
      {children}
    </Flex>
  );
}

function RequirementTag({
  required,
  score,
}: {
  required: number | null;
  score: number | null | undefined;
}) {
  if (required === null || score === null || score === undefined) {
    return NO_VALUE;
  }
  return score >= required ? (
    <Tag color="success">Выполнено</Tag>
  ) : (
    <Tag color="error">Не выполнено</Tag>
  );
}

function CircularCell({
  circular,
}: {
  circular: ReviewCompetency["circular"];
}) {
  const { average, distribution, notObservedCount } = circular;
  if (average === null && notObservedCount === 0) return NO_VALUE;
  return (
    <Flex vertical>
      <Typography.Text>
        Среднее: {average === null ? NO_VALUE : formatAverage(average)}
      </Typography.Text>
      {distribution.length > 0 && (
        <Typography.Text type="secondary">
          Баллы:{" "}
          {distribution
            .map(({ score, count }) => `${score} × ${count}`)
            .join(", ")}
        </Typography.Text>
      )}
      <Typography.Text type="secondary">
        Не наблюдали: {notObservedCount}
      </Typography.Text>
    </Flex>
  );
}

function DecisionTag({ decision }: { decision: Review["decision"] }) {
  if (!decision) return null;
  return (
    <Tag color={DECISION_TAGS[decision].color}>
      {DECISION_TAGS[decision].label}
    </Tag>
  );
}

const historyColumns: TableColumnsType<HistoryItem> = [
  {
    title: "Волна",
    dataIndex: "waveStartsOn",
    render: (startsOn: string) => `с ${formatDate(startsOn)}`,
  },
  {
    title: "Цель",
    key: "target",
    render: (_, { target }) =>
      `${target.unitName}: ${target.trackName}, ${target.levelName}`,
  },
  {
    title: "Решение",
    dataIndex: "decision",
    render: (decision: HistoryItem["decision"]) =>
      decision ? (
        <DecisionTag decision={decision} />
      ) : (
        APPLICATION_STATUS_TAGS.NoDecision.label
      ),
  },
  {
    title: "Итоговые оценки",
    key: "finalScores",
    render: (_, { finalScores }) =>
      finalScores.length === 0
        ? NO_VALUE
        : finalScores.map((score) => (
            <div key={score.competencyId}>
              {score.competencyName}: {score.score}
            </div>
          )),
  },
];

/** The card of a candidate for the commission: what they and their raters gave, the requirements, the formal check and earlier applications; the chair also enters the final scores and the decision. */
export function ReviewView({ review, onSubmit }: ReviewViewProps) {
  const [form] = Form.useForm<ReviewFormValues>();
  const [isDirty, setIsDirty] = useState(false);
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
  const entered = Form.useWatch("finalScores", form);

  const isChair = review.roles.includes("Chair");
  const canEdit =
    isChair &&
    review.stage <= STAGE.review &&
    EDITABLE_STATUSES.includes(review.status);
  const statusTag = APPLICATION_STATUS_TAGS[review.status];
  const { target, formal } = review;

  const columns: TableColumnsType<ReviewCompetency> = [
    {
      title: "Компетенция",
      key: "name",
      render: (_, competency) => (
        <Flex align="center" gap="small" wrap>
          <span>{competency.name}</span>
          {competency.type === "Critical" && (
            <Tag color="error">{COMPETENCY_TYPES.Critical}</Tag>
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
      dataIndex: "selfScore",
      align: "right",
      render: (score: number | null) => score ?? NO_VALUE,
    },
    {
      title: "Круговая оценка",
      key: "circular",
      render: (_, competency) => (
        <CircularCell circular={competency.circular} />
      ),
    },
    {
      title: "Итоговый балл",
      key: "finalScore",
      align: canEdit ? undefined : "right",
      render: (_, competency) =>
        canEdit && competency.scalePoints.length > 0 ? (
          <Form.Item
            name={["finalScores", competency.competencyId]}
            style={{ margin: 0 }}
            rules={[{ required: true, message: "Выберите балл" }]}
          >
            <Select
              aria-label={`Итоговый балл: ${competency.name}`}
              style={SCORE_SELECT_STYLE}
              popupMatchSelectWidth={false}
              options={competency.scalePoints.map((point) => ({
                value: point.score,
                label: scalePointLabel(point),
              }))}
            />
          </Form.Item>
        ) : (
          (competency.finalScore ?? NO_VALUE)
        ),
    },
    {
      title: "Требование",
      key: "requirement",
      render: (_, competency) => (
        <RequirementTag
          required={competency.requiredScore}
          score={
            canEdit ? entered?.[competency.competencyId] : competency.finalScore
          }
        />
      ),
    },
  ];

  return (
    <Flex vertical gap="large">
      <Flex vertical gap="small">
        <Typography.Text strong>
          {target.unitName}: {target.trackName}, {target.levelName}
        </Typography.Text>
        <Flex align="center" gap="small" wrap>
          <Tag color={statusTag.color}>{statusTag.label}</Tag>
          <DecisionTag decision={review.decision} />
          {review.decidedByName && review.decidedAt && (
            <Typography.Text type="secondary">
              {review.decidedByName}, {formatDate(review.decidedAt)}
            </Typography.Text>
          )}
        </Flex>
        <Link to={promotionPaths.commission(review.waveId, target.unitId)}>
          Рейтинг кандидатов подразделения
        </Link>
      </Flex>
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
      {!isChair && (
        <Alert
          type="info"
          showIcon
          title="Итоговые оценки и решение вносит председатель комиссии"
        />
      )}
      {isChair && !canEdit && (
        <Alert
          type="info"
          showIcon
          title="Итоговые оценки и решение сейчас нельзя изменить"
        />
      )}
      <Section title="Рейтинг">
        <Descriptions
          size="small"
          column={1}
          items={[
            { label: "Рейтинг", children: formatPercent(review.rating) },
            {
              label: "Порог готовности",
              children: formatPercent(review.threshold),
            },
            {
              label: "Готовность",
              children: <ReadinessTag isReady={review.isReady} />,
            },
          ]}
        />
        {canEdit && (
          <Typography.Text type="secondary">
            Рейтинг и готовность пересчитываются после сохранения оценок.
          </Typography.Text>
        )}
      </Section>
      <Form
        form={form}
        name="promotion-review"
        layout="vertical"
        disabled={isSubmitting}
        initialValues={{
          finalScores: Object.fromEntries(
            scaled.flatMap((competency) =>
              competency.finalScore === null
                ? []
                : [[competency.competencyId, competency.finalScore]],
            ),
          ),
          decision: review.decision ?? undefined,
        }}
        onFinish={submit}
        onValuesChange={() => setIsDirty(form.isFieldsTouched())}
      >
        <Flex vertical gap="large">
          <Section title="Оценки по компетенциям">
            <Table<ReviewCompetency>
              rowKey="competencyId"
              columns={columns}
              dataSource={[...review.competencies]}
              pagination={false}
              scroll={{ x: "max-content" }}
            />
          </Section>
          {canEdit && (
            <Section title="Решение">
              <Form.Item name="decision" style={{ margin: 0 }}>
                <Radio.Group
                  orientation="vertical"
                  options={DECISION_OPTIONS}
                />
              </Form.Item>
              <Flex>
                <Button type="primary" htmlType="submit" loading={isSubmitting}>
                  Сохранить оценки и решение
                </Button>
              </Flex>
            </Section>
          )}
        </Flex>
      </Form>
      <Section title="Артефакты">
        <ArtifactsView
          competencies={review.competencies.map((competency) => ({
            competencyId: competency.competencyId,
            competencyName: competency.name,
            text: competency.artifact?.text,
            links: competency.artifact?.links,
          }))}
          mandatoryIds={review.competencies
            .filter((competency) => competency.isArtifactMandatory)
            .map((competency) => competency.competencyId)}
          isEditable={false}
          onSubmit={savesNothing}
        />
      </Section>
      <Section title="Формальная проверка">
        <Descriptions
          size="small"
          column={1}
          items={[
            {
              label: "Дата приёма или последнего повышения",
              children: formal.hiredOrPromotedOn
                ? formatDate(formal.hiredOrPromotedOn)
                : NO_VALUE,
            },
            {
              label: "Образование",
              children: review.education
                ? EDUCATION_LABELS[review.education]
                : NO_VALUE,
            },
            ...(formal.ignoreInterval
              ? [
                  {
                    label: "Проверка интервала",
                    children: `Отключена${formal.ignoreIntervalByName ? `: ${formal.ignoreIntervalByName}` : ""}`,
                  },
                ]
              : []),
            {
              label: "Результат",
              children:
                review.formalFailures.length === 0
                  ? "Формальные признаки пройдены"
                  : review.formalFailures
                      .map((failure) => FORMAL_FAILURE_TEXTS[failure])
                      .join("; "),
            },
          ]}
        />
      </Section>
      <Section title="Прошлые заявки">
        <Table<HistoryItem>
          rowKey={(item) => `${item.waveId}/${item.target.levelId}`}
          columns={historyColumns}
          dataSource={[...review.history]}
          pagination={false}
          scroll={{ x: "max-content" }}
          locale={{ emptyText: "Прошлых заявок нет." }}
        />
      </Section>
    </Flex>
  );
}
