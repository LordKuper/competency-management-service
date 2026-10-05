import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Breadcrumb,
  Card,
  Descriptions,
  Flex,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { Link, useParams } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { layout } from "../../app/theme";
import { ifMatchOf } from "../users/usersApi";
import { EmployeeForm } from "./EmployeeForm";
import {
  type Employee,
  type EmployeeInput,
  employeeQuery,
  invalidateOrgStructure,
} from "./orgStructureApi";
import { paths } from "./paths";
import { EmployeeStatusTag } from "./StatusTags";

/**
 * Card of one employee. The full projection, which carries the version, is shown as an editable form;
 * the restricted projection of an ordinary user is shown as plain facts without any edit control.
 */
export function EmployeeCardPage() {
  const { id = "" } = useParams();
  const {
    data: employee,
    error,
    isPending,
    refetch,
  } = useQuery(employeeQuery(id));
  if (isPending) return <Skeleton active />;
  if (!employee) {
    return (
      <ErrorAlert
        title="Не удалось загрузить сотрудника"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Breadcrumb
        items={[
          { title: <Link to={paths.tree}>Оргструктура</Link> },
          { title: <Link to={paths.employees}>Сотрудники</Link> },
          { title: employee.fullName },
        ]}
      />
      <Flex align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          {employee.fullName}
        </Typography.Title>
        {employee.isActive != null && (
          <EmployeeStatusTag isActive={employee.isActive} />
        )}
      </Flex>
      {employee.version == null ? (
        <EmployeeFacts employee={employee} />
      ) : (
        <EditableEmployee employee={employee} version={employee.version} />
      )}
    </Space>
  );
}

function EmployeeFacts({ employee }: { employee: Employee }) {
  return (
    <Card style={{ maxWidth: layout.readingMaxWidth }}>
      <Descriptions
        column={1}
        items={[
          { key: "email", label: "E-mail", children: employee.email },
          {
            key: "unit",
            label: "Подразделение",
            children: (
              <Link to={paths.unit(employee.orgUnitId)}>
                {employee.orgUnitName}
              </Link>
            ),
          },
          { key: "position", label: "Должность", children: employee.position },
        ]}
      />
    </Card>
  );
}

function EditableEmployee({
  employee,
  version,
}: {
  employee: Employee;
  version: number;
}) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const update = useMutation({
    mutationFn: async (input: EmployeeInput) =>
      unwrap(
        await api.PUT("/api/v1/employees/{id}", {
          params: {
            path: { id: employee.id },
            header: { "If-Match": ifMatchOf(version) },
          },
          body: input,
        }),
      ).data,
    onSuccess: () => message.success("Изменения сохранены"),
    onSettled: () => invalidateOrgStructure(queryClient),
  });

  return (
    <>
      {update.isError && (
        <ErrorAlert
          title="Не удалось сохранить изменения"
          error={update.error}
        />
      )}
      <Card title="Сотрудник" style={{ maxWidth: layout.readingMaxWidth }}>
        <EmployeeForm
          key={version}
          initialValues={{
            fullName: employee.fullName,
            personnelNumber: employee.personnelNumber ?? undefined,
            email: employee.email,
            orgUnitId: employee.orgUnitId,
            position: employee.position,
            isActive: employee.isActive ?? undefined,
          }}
          submitLabel="Сохранить"
          onSubmit={(input) => update.mutateAsync(input)}
        />
      </Card>
    </>
  );
}
