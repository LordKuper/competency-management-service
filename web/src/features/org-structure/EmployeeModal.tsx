import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Descriptions, Modal, Skeleton, Space } from "antd";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { ifMatchOf } from "../users/usersApi";
import { EmployeeForm } from "./EmployeeForm";
import {
  type Employee,
  type EmployeeInput,
  employeeQuery,
  invalidateOrgStructure,
} from "./orgStructureApi";

interface EmployeeModalProps {
  /** Employee to open; omitted when a new employee is created. */
  employeeId?: string;
  /** Unit a new employee starts in; ignored when an employee is opened. */
  orgUnitId?: string;
  onClose: () => void;
  /** Receives the saved employee once the server has accepted it. */
  onSaved: (employee: Employee) => void;
}

/**
 * Dialog that shows one employee or creates one. The full projection, which carries the version, opens as an editable
 * form; the restricted projection of an ordinary user opens as plain facts. Mount it only while it is shown, so every
 * opening starts clean and an employee is read only when the dialog opens.
 */
export function EmployeeModal({
  employeeId,
  orgUnitId,
  onClose,
  onSaved,
}: EmployeeModalProps) {
  return (
    <Modal
      open
      title={employeeId === undefined ? "Новый сотрудник" : "Сотрудник"}
      onCancel={onClose}
      footer={null}
    >
      {employeeId === undefined ? (
        <EmployeeEditor
          orgUnitId={orgUnitId}
          onClose={onClose}
          onSaved={onSaved}
        />
      ) : (
        <OpenedEmployee
          employeeId={employeeId}
          onClose={onClose}
          onSaved={onSaved}
        />
      )}
    </Modal>
  );
}

function OpenedEmployee({
  employeeId,
  onClose,
  onSaved,
}: Pick<EmployeeModalProps, "onClose" | "onSaved"> & { employeeId: string }) {
  const {
    data: employee,
    error,
    isPending,
    refetch,
  } = useQuery(employeeQuery(employeeId));

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
  if (employee.version == null) return <EmployeeFacts employee={employee} />;
  return (
    <EmployeeEditor
      existing={{ employee, version: employee.version }}
      onClose={onClose}
      onSaved={onSaved}
    />
  );
}

function EmployeeFacts({ employee }: { employee: Employee }) {
  return (
    <Descriptions
      column={1}
      items={[
        { key: "name", label: "ФИО", children: employee.fullName },
        { key: "position", label: "Должность", children: employee.position },
        {
          key: "unit",
          label: "Подразделение",
          children: employee.orgUnitName,
        },
        {
          key: "email",
          label: "E-mail",
          children: employee.email ?? "Нет учётной записи",
        },
      ]}
    />
  );
}

interface EmployeeEditorProps
  extends Pick<EmployeeModalProps, "onClose" | "onSaved"> {
  /** The employee being changed and the version the change is based on; omitted when an employee is created. */
  existing?: { employee: Employee; version: number };
  /** Unit a new employee starts in. */
  orgUnitId?: string;
}

/** The form with its save: a change is refused when the version is stale, and the employee is reread whatever the outcome. */
function EmployeeEditor({
  existing,
  orgUnitId,
  onClose,
  onSaved,
}: EmployeeEditorProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const save = useMutation({
    mutationFn: async (input: EmployeeInput) =>
      unwrap(
        existing
          ? await api.PUT("/api/v1/employees/{id}", {
              params: {
                path: { id: existing.employee.id },
                header: { "If-Match": ifMatchOf(existing.version) },
              },
              body: input,
            })
          : await api.POST("/api/v1/employees", { body: input }),
      ).data,
    onSuccess: async (saved) => {
      await invalidateOrgStructure(queryClient);
      message.success(existing ? "Изменения сохранены" : "Сотрудник создан");
      onSaved(saved);
    },
    onError: () => invalidateOrgStructure(queryClient),
  });

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      {save.isError && (
        <ErrorAlert
          title="Не удалось сохранить сотрудника"
          error={save.error}
        />
      )}
      <EmployeeForm
        key={existing?.version}
        initialValues={
          existing
            ? {
                lastName: existing.employee.lastName,
                firstName: existing.employee.firstName,
                middleName: existing.employee.middleName ?? undefined,
                orgUnitId: existing.employee.orgUnitId,
                position: existing.employee.position,
                isActive: existing.employee.isActive ?? undefined,
              }
            : { isActive: true, orgUnitId }
        }
        email={existing?.employee.email}
        submitLabel={existing ? "Сохранить" : "Создать"}
        onSubmit={(input) => save.mutateAsync(input)}
        onCancel={onClose}
      />
    </Space>
  );
}
