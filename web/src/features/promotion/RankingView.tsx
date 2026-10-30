import { Empty, Flex, Table, type TableColumnsType, Tag } from "antd";
import { Link } from "react-router";
import type { RankingItem } from "./promotionApi";
import { DECISION_TAGS, formatPercent } from "./promotionTexts";
import { RatingBasis } from "./ReviewParts";
import { promotionPaths } from "./routes";
import { ThresholdBar } from "./ThresholdBar";

function DecisionCell({ item }: { item: RankingItem }) {
  if (item.decision) {
    const tag = DECISION_TAGS[item.decision];
    return <Tag color={tag.color}>{tag.label}</Tag>;
  }
  return item.status === "NoDecision" ? (
    <Tag>Решение не принято</Tag>
  ) : (
    <Tag color="processing">Ждёт решения</Tag>
  );
}

interface RankingViewProps {
  items: readonly RankingItem[];
  isLoading: boolean;
  /** The candidate whose card is open, marked in the list. */
  currentId?: string;
}

/** The candidates of a commission by rating, the highest first, with how close each is to the threshold and what the commission decided. */
export function RankingView({ items, isLoading, currentId }: RankingViewProps) {
  const columns: TableColumnsType<RankingItem> = [
    { title: "Место", dataIndex: "rank", align: "right" },
    {
      title: "Кандидат",
      key: "candidate",
      render: (_, item) => (
        <Link
          to={promotionPaths.review(item.applicationId)}
          aria-current={item.applicationId === currentId || undefined}
          aria-label={`Карточка кандидата: ${item.candidateName}`}
        >
          {item.applicationId === currentId ? (
            <b>{item.candidateName}</b>
          ) : (
            item.candidateName
          )}
        </Link>
      ),
    },
    {
      title: "Цель",
      key: "target",
      render: (_, { target }) => `${target.trackName} · ${target.levelName}`,
    },
    {
      title: "Готовность",
      key: "readiness",
      render: (_, item) => (
        <Flex vertical>
          <span>
            {formatPercent(item.rating)}
            {item.threshold !== null && ` из ${formatPercent(item.threshold)}`}
          </span>
          <ThresholdBar
            value={item.rating}
            threshold={item.threshold}
            label={`${item.candidateName}: ${formatPercent(item.rating)}`}
          />
          <RatingBasis isRatingFromFinalScores={item.isRatingFromFinalScores} />
        </Flex>
      ),
    },
    {
      title: "Решение",
      key: "decision",
      render: (_, item) => <DecisionCell item={item} />,
    },
  ];
  return (
    <Table<RankingItem>
      data-tour="ranking-table"
      rowKey="applicationId"
      columns={columns}
      dataSource={[...items]}
      loading={isLoading}
      pagination={false}
      scroll={{ x: "max-content" }}
      rowClassName={(item) =>
        item.applicationId === currentId ? "ant-table-row-selected" : ""
      }
      locale={{
        emptyText: (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Заявок на рассмотрении пока нет."
          />
        ),
      }}
    />
  );
}
