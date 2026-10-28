import { Empty, Table, type TableColumnsType, Tag } from "antd";
import { Link } from "react-router";
import type { RankingItem } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  DECISION_TAGS,
  formatPercent,
  NO_VALUE,
} from "./promotionTexts";
import { ReadinessTag } from "./ReadinessTag";
import { promotionPaths } from "./routes";

const columns: TableColumnsType<RankingItem> = [
  {
    title: "Место",
    dataIndex: "rank",
    align: "right",
    render: (rank: number | null) => rank ?? NO_VALUE,
  },
  { title: "Кандидат", dataIndex: "candidateName" },
  {
    title: "Цель",
    key: "target",
    render: (_, { target }) => `${target.trackName}, ${target.levelName}`,
  },
  {
    title: "Статус",
    dataIndex: "status",
    render: (status: RankingItem["status"]) => (
      <Tag color={APPLICATION_STATUS_TAGS[status].color}>
        {APPLICATION_STATUS_TAGS[status].label}
      </Tag>
    ),
  },
  {
    title: "Рейтинг",
    dataIndex: "rating",
    align: "right",
    render: formatPercent,
  },
  {
    title: "Порог",
    dataIndex: "threshold",
    align: "right",
    render: formatPercent,
  },
  {
    title: "Готовность",
    dataIndex: "isReady",
    render: (isReady: boolean | null) => <ReadinessTag isReady={isReady} />,
  },
  {
    title: "Решение",
    dataIndex: "decision",
    render: (decision: RankingItem["decision"]) =>
      decision ? (
        <Tag color={DECISION_TAGS[decision].color}>
          {DECISION_TAGS[decision].label}
        </Tag>
      ) : (
        NO_VALUE
      ),
  },
  {
    title: "Действия",
    key: "actions",
    render: (_, item) => (
      <Link
        to={promotionPaths.review(item.applicationId)}
        aria-label={`Карточка кандидата: ${item.candidateName}`}
      >
        Карточка
      </Link>
    ),
  },
];

interface RankingViewProps {
  items: readonly RankingItem[];
  isLoading: boolean;
}

/** The candidates of a commission by rating, the highest first; the ones the chair has not scored yet follow without a place. */
export function RankingView({ items, isLoading }: RankingViewProps) {
  return (
    <Table<RankingItem>
      rowKey="applicationId"
      columns={columns}
      dataSource={[...items]}
      loading={isLoading}
      pagination={false}
      scroll={{ x: "max-content" }}
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
