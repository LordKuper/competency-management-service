import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Modal, Skeleton, Space } from "antd";
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
  orgUnitTreeQuery,
} from "./orgStructureApi";

interface EmployeeModalProps {
  /** Employee to change; omitted when a new employee is created. */
  employeeId?: string;
  /** Unit a new employee starts in; ignored when an employee is changed. */
  orgUnitId?: string;
  onClose: () => void;
  /** Receives the saved employee once the server has accepted it. */
  onSaved: (employee: Employee) => void;
}

/**
 * Dialog that changes one employee or creates one. Only administrators reach it. An existing employee is read when the
 * dialog opens, so the change is based on the current version. Mount it only while it is shown, so every opening starts clean.
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
      title={
        employeeId === undefined ? "Новый сотрудник" : "Изменение сотрудника"
      }
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
        <ChangedEmployee
          employeeId={employeeId}
          onClose={onClose}
          onSaved={onSaved}
        />
      )}
    </Modal>
  );
}

function ChangedEmployee({
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
  if (employee?.version == null) {
    return (
      <ErrorAlert
        title="Не удалось загрузить сотрудника"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return (
    <EmployeeEditor
      existing={{ employee, version: employee.version }}
      onClose={onClose}
      onSaved={onSaved}
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

/**
 * The form with its save: a change is refused when the version is stale, and the employee is reread whatever the outcome.
 * Moving the head of a unit to another unit is confirmed first, because the server then leaves the unit without a head.
 */
function EmployeeEditor({
  existing,
  orgUnitId,
  onClose,
  onSaved,
}: EmployeeEditorProps) {
  const queryClient = useQueryClient();
  const { message, modal } = App.useApp();
  const { data: units } = useQuery(orgUnitTreeQuery);
  const headedUnit = units?.find(
    (unit) =>
      unit.id === existing?.employee.orgUnitId &&
      unit.headEmployeeId === existing.employee.id,
  );
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

  const confirmUnitLosesHead = (unitName: string) =>
    new Promise<boolean>((resolve) =>
      modal.confirm({
        title: "Перевести руководителя?",
        content: `Подразделение «${unitName}» останется без руководителя. Продолжить?`,
        okText: "Перевести",
        cancelText: "Отмена",
        onOk: () => resolve(true),
        onCancel: () => resolve(false),
      }),
    );

  async function submit(input: EmployeeInput) {
    if (
      headedUnit &&
      input.orgUnitId !== headedUnit.id &&
      !(await confirmUnitLosesHead(headedUnit.name))
    ) {
      return;
    }
    await save.mutateAsync(input);
  }

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
              }
            : { orgUnitId }
        }
        email={existing?.employee.email}
        submitLabel={existing ? "Сохранить" : "Создать"}
        onSubmit={submit}
        onCancel={onClose}
      />
    </Space>
  );
}
