import { Button, Empty, Flex, Table, type TableColumnsType, Tag } from "antd";
import { Link } from "react-router";
import type { WaveApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  formatDate,
  REVIEWED_STATUSES,
  STAGE,
} from "./promotionTexts";
import { promotionPaths } from "./routes";

interface WaveApplicationsViewProps {
  applications: readonly WaveApplication[];
  total: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  onPageChange: (page: number) => void;
  /** Opens the formal check of an application. */
  onOpenFormal: (applicationId: string) => void;
}

/** The applications of a wave as a table: who applied for what, in which state, the way into the formal check and, for those that reached the commission, into the review. */
export function WaveApplicationsView({
  applications,
  total,
  page,
  pageSize,
  isLoading,
  onPageChange,
  onOpenFormal,
}: WaveApplicationsViewProps) {
  const columns: TableColumnsType<WaveApplication> = [
    { title: "Кандидат", dataIndex: "candidateName" },
    {
      title: "Цель",
      key: "target",
      render: (_, { target }) =>
        `${target.unitName}: ${target.trackName}, ${target.levelName}`,
    },
    {
      title: "Статус",
      dataIndex: "status",
      render: (status: WaveApplication["status"]) => (
        <Tag color={APPLICATION_STATUS_TAGS[status].color}>
          {APPLICATION_STATUS_TAGS[status].label}
        </Tag>
      ),
    },
    {
      title: "Подана",
      dataIndex: "submittedAt",
      render: formatDate,
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, application) => (
        <Flex gap="small" wrap>
          <Button
            type="link"
            aria-label={`Формальная проверка: ${application.candidateName}`}
            onClick={() => onOpenFormal(application.id)}
          >
            {application.stage <= STAGE.formalCheck &&
            application.status === "Submitted"
              ? "Внести дату"
              : "Формальная проверка"}
          </Button>
          {REVIEWED_STATUSES.includes(application.status) && (
            <Link
              to={promotionPaths.review(application.id)}
              aria-label={`Рассмотрение: ${application.candidateName}`}
            >
              Рассмотрение
            </Link>
          )}
        </Flex>
      ),
    },
  ];
  return (
    <Table<WaveApplication>
      rowKey="id"
      columns={columns}
      dataSource={[...applications]}
      loading={isLoading}
      scroll={{ x: "max-content" }}
      locale={{
        emptyText: (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Заявок пока нет."
          />
        ),
      }}
      pagination={{
        current: page,
        pageSize,
        total,
        showSizeChanger: false,
        onChange: onPageChange,
      }}
    />
  );
}
