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
import { useState } from "react";
import { Link } from "react-router";
import { Panel } from "./Panel";
import type { WaveApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  formatDate,
  formatShortDate,
  REVIEWED_STATUSES,
} from "./promotionTexts";
import { promotionPaths } from "./routes";

const ALL = "All";

interface WaveApplicationsViewProps {
  applications: readonly WaveApplication[];
  total: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  /** The day the formal check is fixed, the end of its stage; the footer names it when known. */
  formalCheckEndsOn?: string;
  onPageChange: (page: number) => void;
  /** Opens the data for the formal check of an application. */
  onOpenFormal: (applicationId: string) => void;
}

/** The applications of a career session as a table: who applied for what and in which state, with the search by candidate and the filter by status of the page shown, the way into the data for the formal check and, for those that reached the commission, into the review. */
export function WaveApplicationsView({
  applications,
  total,
  page,
  pageSize,
  isLoading,
  formalCheckEndsOn,
  onPageChange,
  onOpenFormal,
}: WaveApplicationsViewProps) {
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<string>(ALL);
  const needle = search.trim().toLowerCase();
  const shown = applications.filter(
    (application) =>
      (status === ALL || application.status === status) &&
      application.candidateName.toLowerCase().includes(needle),
  );
  const statuses = [
    ...new Set(applications.map((application) => application.status)),
  ];
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
      title: "Статус",
      dataIndex: "status",
      render: (value: WaveApplication["status"]) => (
        <Tag color={APPLICATION_STATUS_TAGS[value].color}>
          {APPLICATION_STATUS_TAGS[value].label}
        </Tag>
      ),
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
  return (
    <Panel
      title="Заявки"
      extra={
        <Input.Search
          allowClear
          placeholder="Поиск по кандидату"
          aria-label="Поиск по кандидату"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
      }
    >
      <Flex vertical gap="middle">
        <Radio.Group
          optionType="button"
          buttonStyle="solid"
          aria-label="Статус заявки"
          value={status}
          onChange={(event) => setStatus(event.target.value)}
          options={[
            { value: ALL, label: `Все · ${applications.length}` },
            ...statuses.map((value) => ({
              value,
              label: `${APPLICATION_STATUS_TAGS[value].label} · ${
                applications.filter(
                  (application) => application.status === value,
                ).length
              }`,
            })),
          ]}
        />
        <Table<WaveApplication>
          rowKey="id"
          columns={columns}
          dataSource={shown}
          loading={isLoading}
          scroll={{ x: "max-content" }}
          locale={{
            emptyText: (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description={
                  applications.length === 0
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
          Показано {shown.length} из {total}.
          {formalCheckEndsOn &&
            ` Итог формальной проверки фиксируется в конце этапа 2 — ${formatDate(formalCheckEndsOn)}.`}
        </Typography.Text>
      </Flex>
    </Panel>
  );
}
