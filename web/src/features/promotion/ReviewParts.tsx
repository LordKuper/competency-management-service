import { Button, Collapse, Flex, Table, Tag, Typography } from "antd";
import { type ReactNode, useState } from "react";
import type { Review } from "./promotionApi";
import {
  artifactsText,
  DECISION_TAGS,
  formatAverage,
  formatDate,
  formatDelta,
  formatPercent,
  NO_VALUE,
} from "./promotionTexts";

/** One competency of a candidate's card. */
export type ReviewCompetency = Review["competencies"][number];
type HistoryItem = Review["history"][number];

/** Anchor of the panel of earlier applications, which the card links to. */
export const HISTORY_ID = "review-history";

function isUnchanged(delta: number): boolean {
  return Math.round(delta * 10) === 0;
}

interface ChangeTextProps {
  change: ReviewCompetency["change"];
  /** Adds the earlier average after the change, as the table of a member does. */
  hasPrevious?: boolean;
}

/** How the average circular score moved against the latest earlier application with a decision, as the server worked it out. */
export function ChangeText({ change, hasPrevious = false }: ChangeTextProps) {
  if (!change)
    return <Typography.Text type="secondary">{NO_VALUE}</Typography.Text>;
  if (change.isNew) {
    return <Typography.Text type="secondary">новая</Typography.Text>;
  }
  if (change.delta === null || isUnchanged(change.delta)) {
    return <Typography.Text type="secondary">без изменений</Typography.Text>;
  }
  const isUp = change.delta > 0;
  return (
    <Flex gap="small" align="baseline" wrap>
      <Typography.Text type={isUp ? "success" : "danger"}>
        {isUp ? "▲" : "▼"} {formatDelta(change.delta)}
      </Typography.Text>
      {hasPrevious && change.previousAverage !== null && (
        <Typography.Text type="secondary">
          было {formatAverage(change.previousAverage)}
        </Typography.Text>
      )}
    </Flex>
  );
}

/** The average circular score with how many raters saw the competency, or a dash when none did. */
export function CircularText({
  circular,
  hasCount = false,
}: {
  circular: ReviewCompetency["circular"];
  hasCount?: boolean;
}) {
  if (circular.average === null) return NO_VALUE;
  const observers = circular.distribution.reduce(
    (sum, point) => sum + point.count,
    0,
  );
  return (
    <span>
      {formatAverage(circular.average)}
      {hasCount && (
        <Typography.Text type="secondary"> ({observers})</Typography.Text>
      )}
    </span>
  );
}

/** The artifacts of one competency as a list of what was done with the link to the document. */
export function ArtifactList({
  artifact,
}: {
  artifact: ReviewCompetency["artifact"];
}) {
  return (
    <ol style={{ margin: 0 }}>
      {artifact?.links.map((link) => (
        <li key={link.url}>
          {link.description}{" "}
          <a href={link.url} target="_blank" rel="noreferrer noopener">
            {link.url.replace(/^https?:\/\//i, "")}
          </a>
        </li>
      ))}
    </ol>
  );
}

/** Which competencies have their artifacts opened under the row, and the table settings that show them there. */
export function useArtifactRows() {
  const [openIds, setOpenIds] = useState<readonly string[]>([]);
  return {
    isOpen: (id: string) => openIds.includes(id),
    toggle: (id: string) =>
      setOpenIds((current) =>
        current.includes(id)
          ? current.filter((openId) => openId !== id)
          : [...current, id],
      ),
    expandable: {
      showExpandColumn: false,
      expandedRowKeys: [...openIds],
      expandedRowRender: (competency: ReviewCompetency) => (
        <ArtifactList artifact={competency.artifact} />
      ),
    },
  };
}

interface ArtifactsToggleProps {
  competency: ReviewCompetency;
  isOpen: boolean;
  onToggle: () => void;
  /** Shows the bare number, as the narrow column of the chair does. */
  isCompact?: boolean;
}

/** The count of a competency's artifacts as the button that opens them, or a dash when there are none. */
export function ArtifactsToggle({
  competency,
  isOpen,
  onToggle,
  isCompact = false,
}: ArtifactsToggleProps) {
  const count = competency.artifact?.links.length ?? 0;
  if (count === 0) return NO_VALUE;
  return (
    <Button
      type="link"
      aria-expanded={isOpen}
      aria-label={`${artifactsText(count)}: ${competency.name}`}
      onClick={onToggle}
    >
      {isCompact ? count : artifactsText(count)}
    </Button>
  );
}

function DecisionTag({ decision }: { decision: HistoryItem["decision"] }) {
  if (!decision) return null;
  return (
    <Tag color={DECISION_TAGS[decision].color}>
      {DECISION_TAGS[decision].label}
    </Tag>
  );
}

const historyColumns = [
  {
    title: "Карьерная сессия",
    dataIndex: "waveStartsOn",
    render: (startsOn: string) => `с ${formatDate(startsOn)}`,
  },
  {
    title: "Цель",
    key: "target",
    render: (_: unknown, { target }: HistoryItem) =>
      `${target.trackName} · ${target.levelName}`,
  },
  {
    title: "Готовность",
    key: "rating",
    render: (_: unknown, { rating, threshold }: HistoryItem) =>
      `${formatPercent(rating)} из ${formatPercent(threshold)}`,
  },
  {
    title: "Критические компетенции",
    key: "failedCritical",
    render: (_: unknown, { failedCritical }: HistoryItem) =>
      failedCritical.length === 0
        ? "Выполнены"
        : `Не выполнено: ${failedCritical
            .map(
              (gap) =>
                `${gap.name} (${gap.score === null ? NO_VALUE : formatAverage(gap.score)} из ${gap.requiredScore})`,
            )
            .join(", ")}`,
  },
  {
    title: "Решение",
    dataIndex: "decision",
    render: (decision: HistoryItem["decision"]) => (
      <DecisionTag decision={decision} />
    ),
  },
];

/** The earlier applications of the candidate that got a decision, and how the readiness moved against the latest of them. */
export function HistoryPanel({ review }: { review: Review }) {
  const { history, readinessChange } = review;
  return (
    <div id={HISTORY_ID} data-tour="review-history">
      <Collapse
        defaultActiveKey={["history"]}
        items={[
          {
            key: "history",
            label: (
              <Typography.Text strong>
                Прошлые заявки — {history.length} с решением
              </Typography.Text>
            ),
            children: (
              <Flex vertical gap="middle">
                <Table<HistoryItem>
                  rowKey={(item) => `${item.waveId}/${item.target.levelId}`}
                  columns={historyColumns}
                  dataSource={[...history]}
                  pagination={false}
                  scroll={{ x: "max-content" }}
                  locale={{ emptyText: "Прошлых заявок с решением нет." }}
                />
                {readinessChange && (
                  <Typography.Text>
                    Готовность:{" "}
                    <b>
                      {formatPercent(readinessChange.previousRating)} →{" "}
                      {formatPercent(readinessChange.rating)}
                    </b>{" "}
                    ({formatDelta(readinessChange.delta)} п.&nbsp;п.) к
                    последней заявке с решением: сессия с{" "}
                    {formatDate(readinessChange.previousWaveStartsOn)},{" "}
                    {readinessChange.previousTarget.trackName} ·{" "}
                    {readinessChange.previousTarget.levelName}.
                  </Typography.Text>
                )}
                <Typography.Text type="secondary">
                  Заявки без решения (отозванные, не допущенные) в сводку не
                  входят.
                </Typography.Text>
              </Flex>
            ),
          },
        ]}
      />
    </div>
  );
}

/** What the rating of a card or a row rests on: the scores the chair saved, or the rounded circular averages that stand in until then. */
export function RatingBasis({
  isRatingFromFinalScores,
}: {
  isRatingFromFinalScores: boolean;
}): ReactNode {
  return (
    <Typography.Text type="secondary">
      {isRatingFromFinalScores
        ? "по итоговым баллам"
        : "по умолчанию: округлённая круговая оценка"}
    </Typography.Text>
  );
}

/** Scrolls to the panel of earlier applications. */
export function showHistory() {
  document.getElementById(HISTORY_ID)?.scrollIntoView({ block: "start" });
}
