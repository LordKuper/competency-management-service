import { Button, Empty, Table, type TableColumnsType, Tag } from "antd";
import type { WaveApplication } from "./promotionApi";
import { APPLICATION_STATUS_TAGS, formatDate, STAGE } from "./promotionTexts";

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

/** The applications of a wave as a table: who applied for what, in which state, and the way into the formal check. */
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
      render: (_, application) =>
        application.stage >= STAGE.formalCheck && (
          <Button
            type="link"
            aria-label={`Формальная проверка: ${application.candidateName}`}
            onClick={() => onOpenFormal(application.id)}
          >
            {application.stage === STAGE.formalCheck &&
            application.status === "Submitted"
              ? "Внести дату"
              : "Формальная проверка"}
          </Button>
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
