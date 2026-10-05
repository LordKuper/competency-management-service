import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Card, Space, Typography } from "antd";
import { Link, useNavigate } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { layout } from "../../app/theme";
import { EmployeeForm } from "./EmployeeForm";
import { type EmployeeInput, invalidateOrgStructure } from "./orgStructureApi";
import { paths } from "./paths";

/** Page on which an administrator creates an employee; success opens the new employee's card. */
export function EmployeeCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const create = useMutation({
    mutationFn: async (input: EmployeeInput) =>
      unwrap(await api.POST("/api/v1/employees", { body: input })).data,
    onSuccess: async (employee) => {
      await invalidateOrgStructure(queryClient);
      message.success("Сотрудник создан");
      void navigate(paths.employee(employee.id));
    },
  });

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Breadcrumb
        items={[
          { title: <Link to={paths.tree}>Оргструктура</Link> },
          { title: <Link to={paths.employees}>Сотрудники</Link> },
          { title: "Новый сотрудник" },
        ]}
      />
      <Typography.Title level={1}>Новый сотрудник</Typography.Title>
      {create.isError && (
        <ErrorAlert
          title="Не удалось создать сотрудника"
          error={create.error}
        />
      )}
      <Card style={{ maxWidth: layout.readingMaxWidth }}>
        <EmployeeForm
          submitLabel="Создать"
          onSubmit={(input) => create.mutateAsync(input)}
        />
      </Card>
    </Space>
  );
}
