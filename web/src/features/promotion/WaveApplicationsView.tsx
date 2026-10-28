import {
  Button,
  Empty,
  Flex,
  Input,
  Radio,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { Link } from "react-router";
import { Panel } from "./Panel";
import type { ApplicationCounts, WaveApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  FORMAL_FAILURE_SHORT,
  formatDate,
  formatShortDate,
  REVIEWED_STATUSES,
} from "./promotionTexts";
import { promotionPaths } from "./routes";

/** The filter button that shows every application. */
export const ALL_APPLICATIONS = "All";

/** The filter button that shows the applications still waiting for the data of the formal check. */
export const NEEDS_FORMAL_DATA = "NeedsFormalData";

interface WaveApplicationsViewProps {
  applications: readonly WaveApplication[];
  total: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  /** The counts of the whole session for the filter buttons; omitted until they are read. */
  counts?: ApplicationCounts;
  /** The chosen filter button: `ALL_APPLICATIONS`, `NEEDS_FORMAL_DATA` or a status. */
  filter: string;
  search: string;
  /** The day the formal check is fixed, the end of its stage; the footer names it when known. */
  formalCheckEndsOn?: string;
  onFilterChange: (filter: string) => void;
  onSearchChange: (search: string) => void;
  onPageChange: (page: number) => void;
  /** Opens the data for the formal check of an application. */
  onOpenFormal: (applicationId: string) => void;
}

function FormalCell({ application }: { application: WaveApplication }) {
  const { status, formalCheck, needsFormalData } = application;
  if (status === "FormalRejected") {
    return (
      <Tag color="error">
        Отклонена:{" "}
        {application.formalFailures
          .map((failure) => FORMAL_FAILURE_SHORT[failure])
          .join(", ")}
      </Tag>
    );
  }
  if (needsFormalData) return <Tag color="warning">Не внесена дата</Tag>;
  if (status === "Submitted" && formalCheck) {
    return formalCheck.isPassed ? (
      <Tag color="success">Пройдёт</Tag>
    ) : (
      <Tag color="error">
        Не пройдёт:{" "}
        {formalCheck.failures
          .map((failure) => FORMAL_FAILURE_SHORT[failure])
          .join(", ")}
      </Tag>
    );
  }
  if (status === "Draft" || status === "Withdrawn") return "—";
  return <Tag color="processing">Допущена</Tag>;
}

function RatersCell({ application }: { application: WaveApplication }) {
  const counts = application.raterCounts;
  if (!counts) return "—";
  return (
    <Flex align="center" gap="small" wrap>
      <span>{counts.total}</span>
      {counts.pendingEmailCount > 0 && (
        <Tag color="warning">
          почта не подтверждена: {counts.pendingEmailCount}
        </Tag>
      )}
      {counts.missingTotal > 0 && (
        <Tag color="warning">меньше {counts.requiredTotal}</Tag>
      )}
    </Flex>
  );
}

/** The applications of a career session as a table: who applied for what and what the formal check and the raters stand at, with the search by candidate and the filter by status, the way into the data for the formal check and, for those that reached the commission, into the review. */
export function WaveApplicationsView({
  applications,
  total,
  page,
  pageSize,
  isLoading,
  counts,
  filter,
  search,
  formalCheckEndsOn,
  onFilterChange,
  onSearchChange,
  onPageChange,
  onOpenFormal,
}: WaveApplicationsViewProps) {
  const columns: TableColumnsType<WaveApplication> = [
    { title: "Кандидат", dataIndex: "candidateName" },
    {
      title: "Цель",
      key: "target",
      render: (_, { target }) =>
        `${target.trackName} · ${target.levelName} (${target.unitName})`,
    },
    {
      title: "Подана",
      dataIndex: "submittedAt",
      render: formatShortDate,
    },
    {
      title: "Формальная проверка",
      key: "formal",
      render: (_, application) => <FormalCell application={application} />,
    },
    {
      title: "Оценивающие",
      key: "raters",
      render: (_, application) => <RatersCell application={application} />,
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, application) => (
        <Flex gap="small" wrap>
          <Button
            type="link"
            aria-label={`Данные для проверки: ${application.candidateName}`}
            onClick={() => onOpenFormal(application.id)}
          >
            Данные для проверки
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
  const filters = counts
    ? [
        { value: ALL_APPLICATIONS, label: `Все · ${counts.all}` },
        ...counts.byStatus.map(({ status, count }) => ({
          value: status,
          label: `${APPLICATION_STATUS_TAGS[status].label} · ${count}`,
        })),
        {
          value: NEEDS_FORMAL_DATA,
          label: `Нужны данные · ${counts.needsFormalData}`,
        },
      ]
    : [{ value: ALL_APPLICATIONS, label: "Все" }];
  return (
    <Panel
      title="Заявки"
      extra={
        <Input.Search
          allowClear
          placeholder="Поиск по кандидату"
          aria-label="Поиск по кандидату"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
        />
      }
    >
      <Flex vertical gap="middle">
        <Radio.Group
          optionType="button"
          buttonStyle="solid"
          aria-label="Статус заявки"
          value={filter}
          onChange={(event) => onFilterChange(event.target.value)}
          options={filters}
        />
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
                description={
                  filter === ALL_APPLICATIONS && search.trim() === ""
                    ? "Заявок пока нет."
                    : "Нет заявок по выбранным условиям."
                }
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
        <Typography.Text type="secondary">
          Показано {applications.length} из {total}.
          {formalCheckEndsOn &&
            ` Итог формальной проверки фиксируется в конце этапа 2 — ${formatDate(formalCheckEndsOn)}.`}
        </Typography.Text>
      </Flex>
    </Panel>
  );
}
