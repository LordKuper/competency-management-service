import { useQuery } from "@tanstack/react-query";
import { Empty, Table, type TableColumnsType, Typography } from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import {
  choiceFilter,
  type FilterOption,
  onFilterChange,
  useUnitOptions,
} from "./columnFilters";
import {
  type Approval,
  type ApprovalListParams,
  approvalsQuery,
} from "./matrixApi";
import { APPROVAL_STEP_LABELS } from "./matrixLabels";

const DATE_TIME = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeStyle: "short",
});

function columns(
  params: ApprovalListParams,
  units: FilterOption[],
): TableColumnsType<Approval> {
  return [
    {
      title: "Матрица",
      key: "matrix",
      render: (_, approval) => (
        <Link to={`/career/matrices/${approval.matrixId}`}>
          {`${approval.trackName} — Версия ${approval.number}`}
        </Link>
      ),
    },
    {
      title: "Подразделение",
      dataIndex: "orgUnitName",
      ...choiceFilter("orgUnitId", params.orgUnitId, units, true),
    },
    {
      title: "Шаг",
      dataIndex: "step",
      ...choiceFilter(
        "step",
        params.step,
        Object.entries(APPROVAL_STEP_LABELS).map(([value, text]) => ({
          text,
          value,
        })),
      ),
      render: (step: Approval["step"]) => step && APPROVAL_STEP_LABELS[step],
    },
    {
      title: "Отправлена",
      dataIndex: "submittedAt",
      render: (at: string | null) => at && DATE_TIME.format(new Date(at)),
    },
  ];
}

/** Versions of matrices that wait for the signed-in user's decision. */
export function ApprovalsSection() {
  const [params, setParams] = useState<ApprovalListParams>({});
  const units = useUnitOptions();
  const { data, error, isFetching, refetch } = useQuery(approvalsQuery(params));

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
        columns={columns(params, units)}
        dataSource={data}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
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
