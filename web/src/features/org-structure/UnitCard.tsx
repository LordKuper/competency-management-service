import { useQuery } from "@tanstack/react-query";
import {
  Breadcrumb,
  Button,
  Card,
  Descriptions,
  Flex,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useIsAdmin } from "../auth/useCurrentUser";
import { EmployeeList } from "./EmployeeList";
import { MoveUnitModal } from "./MoveUnitModal";
import {
  employeeQuery,
  type OrgUnit,
  orgUnitPathQuery,
  orgUnitSummaryQuery,
} from "./orgStructureApi";
import { paths } from "./paths";
import { UnitStatusTag } from "./StatusTags";
import { UnitFormModal } from "./UnitFormModal";
import { useUnitActivation } from "./useUnitActivation";

type Dialog = "edit" | "child" | "move";

function formatDate(date: string | null): string {
  if (date === null) return "Не задана";
  const [year, month, day] = date.split("-");
  return `${day}.${month}.${year}`;
}

function EmployeeLink({ id }: { id: string }) {
  const { data, error } = useQuery(employeeQuery(id));
  if (data) return <Link to={paths.employee(data.id)}>{data.fullName}</Link>;
  return error ? (
    "Не удалось загрузить"
  ) : (
    <Skeleton.Input active size="small" />
  );
}

interface UnitCardProps {
  unit: OrgUnit;
  /** Called with a unit that became the subject of the screen, such as one just created. */
  onSelect: (unitId: string) => void;
}

/**
 * Card of the selected unit: its path, head, validity, headcount and employees. Administrators also get the actions that
 * add a child, edit, move and activate or deactivate; the server refuses them for anyone else regardless.
 */
export function UnitCard({ unit, onSelect }: UnitCardProps) {
  const isAdmin = useIsAdmin();
  const [dialog, setDialog] = useState<Dialog | null>(null);
  const confirmActivation = useUnitActivation();
  const path = useQuery(orgUnitPathQuery(unit.id));
  const summary = useQuery(orgUnitSummaryQuery(unit.id));
  const failure = path.error ?? summary.error;
  const closeDialog = () => setDialog(null);

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Card>
        <Space orientation="vertical" size="middle" style={{ display: "flex" }}>
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
          <Flex align="center" gap="middle" wrap>
            <Typography.Title level={2} style={{ margin: 0 }}>
              {unit.name}
            </Typography.Title>
            <UnitStatusTag isActive={unit.isActive} />
          </Flex>
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
            column={{ xs: 1, md: 2 }}
            items={[
              {
                key: "head",
                label: "Руководитель",
                children: unit.headEmployeeId ? (
                  <EmployeeLink id={unit.headEmployeeId} />
                ) : (
                  "Не назначен"
                ),
              },
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
                label:
                  "Работающих сотрудников, включая вложенные подразделения",
                children: summary.data?.employeeCount ?? "—",
              },
              {
                key: "directEmployeeCount",
                label: "Работающих сотрудников непосредственно в подразделении",
                children: summary.data?.directEmployeeCount ?? "—",
              },
            ]}
          />
          {isAdmin && (
            <Space orientation="vertical" size="small">
              <Space wrap>
                <Button onClick={() => setDialog("edit")}>Изменить</Button>
                <Button
                  disabled={!unit.isActive}
                  onClick={() => setDialog("child")}
                >
                  Добавить подчинённое
                </Button>
                <Button onClick={() => setDialog("move")}>Перенести</Button>
                <Button
                  danger={unit.isActive}
                  onClick={() => confirmActivation(unit)}
                >
                  {unit.isActive ? "Деактивировать" : "Активировать"}
                </Button>
              </Space>
              {!unit.isActive && (
                <Typography.Text type="secondary">
                  Подразделение неактивно: подчинённые подразделения можно
                  добавлять только после активации.
                </Typography.Text>
              )}
            </Space>
          )}
        </Space>
      </Card>
      <Card title="Сотрудники подразделения">
        <EmployeeList key={unit.id} unitId={unit.id} />
      </Card>
      {dialog === "edit" && (
        <UnitFormModal
          unit={unit}
          onClose={closeDialog}
          onSaved={closeDialog}
        />
      )}
      {dialog === "child" && (
        <UnitFormModal
          parentId={unit.id}
          onClose={closeDialog}
          onSaved={(created) => {
            closeDialog();
            onSelect(created.id);
          }}
        />
      )}
      {dialog === "move" && <MoveUnitModal unit={unit} onClose={closeDialog} />}
    </Space>
  );
}
