import { useQuery } from "@tanstack/react-query";
import { Empty, Table, type TableColumnsType, Typography } from "antd";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { type Approval, approvalsQuery } from "./matrixApi";
import { APPROVAL_STEP_LABELS } from "./matrixLabels";

const DATE_TIME = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeStyle: "short",
});

const COLUMNS: TableColumnsType<Approval> = [
  {
    title: "Матрица",
    key: "matrix",
    render: (_, approval) => (
      <Link to={`/career/matrices/${approval.matrixId}`}>
        {`${approval.trackName} — Версия ${approval.number}`}
      </Link>
    ),
  },
  { title: "Подразделение", dataIndex: "orgUnitName" },
  {
    title: "Шаг",
    dataIndex: "step",
    render: (step: Approval["step"]) => step && APPROVAL_STEP_LABELS[step],
  },
  {
    title: "Отправлена",
    dataIndex: "submittedAt",
    render: (at: string | null) => at && DATE_TIME.format(new Date(at)),
  },
];

/** Versions of matrices that wait for the signed-in user's decision. */
export function ApprovalsSection() {
  const { data, error, isFetching, refetch } = useQuery(approvalsQuery);

  return (
    <section>
      <Typography.Title level={2}>Ждут моего согласования</Typography.Title>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить версии на согласовании"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Approval>
        rowKey="versionId"
        columns={COLUMNS}
        dataSource={data}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Нет версий на согласовании"
            />
          ),
        }}
      />
    </section>
  );
}
