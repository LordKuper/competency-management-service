import { useQuery } from "@tanstack/react-query";
import { Breadcrumb, Descriptions, Space } from "antd";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import {
  type OrgUnit,
  orgUnitPathQuery,
  orgUnitSummaryQuery,
} from "./orgStructureApi";
import { paths } from "./paths";

function formatDate(date: string | null): string {
  if (date === null) return "Не задана";
  const [year, month, day] = date.split("-");
  return `${day}.${month}.${year}`;
}

/** Path from the root, validity and headcount of a unit; read only while the block is shown. */
export function UnitDetails({ unit }: { unit: OrgUnit }) {
  const path = useQuery(orgUnitPathQuery(unit.id));
  const summary = useQuery(orgUnitSummaryQuery(unit.id));
  const failure = path.error ?? summary.error;

  return (
    <Space
      orientation="vertical"
      size="small"
      className="org-card__details"
      style={{ display: "flex" }}
    >
      {path.data && (
        <Breadcrumb
          items={path.data.map((ancestor) => ({
            title:
              ancestor.id === unit.id ? (
                ancestor.name
              ) : (
                <Link to={paths.unit(ancestor.id)}>{ancestor.name}</Link>
              ),
          }))}
        />
      )}
      {failure && (
        <ErrorAlert
          title="Не удалось загрузить сведения о подразделении"
          error={failure}
          onRetry={() => {
            void path.refetch();
            void summary.refetch();
          }}
        />
      )}
      <Descriptions
        size="small"
        column={{ xs: 1, md: 2 }}
        items={[
          {
            key: "validFrom",
            label: "Действует с",
            children: formatDate(unit.validFrom),
          },
          {
            key: "validTo",
            label: "Действует по",
            children: formatDate(unit.validTo),
          },
          {
            key: "employeeCount",
            label: "Работающих сотрудников, включая вложенные подразделения",
            children: summary.data?.employeeCount ?? "—",
          },
          {
            key: "directEmployeeCount",
            label: "Работающих сотрудников непосредственно в подразделении",
            children: summary.data?.directEmployeeCount ?? "—",
          },
        ]}
      />
    </Space>
  );
}
