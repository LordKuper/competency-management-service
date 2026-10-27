import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  App,
  Button,
  Form,
  Select,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { useMemo } from "react";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { AppModal } from "../../app/AppModal";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useEditBase } from "../../app/useEditBase";
import { unitPositionsQuery } from "../positions/positionsApi";
import { shownAssignments } from "./EmployeeCard";
import { OrgUnitSelect } from "./OrgUnitSelect";
import {
  type Employee,
  type EmployeeAssignment,
  employeeQuery,
  invalidateOrgStructure,
  mainAssignmentOf,
  orgUnitTreeQuery,
} from "./orgStructureApi";
import { useConfirmUnitLosesHead } from "./useConfirmUnitLosesHead";

interface MoveFormValues {
  assignment: string;
  toOrgUnitId?: string;
}

interface MoveEmployeeModalProps {
  /** The employee as the card shows them; its assignments are the ones that can be moved. */
  employee: Employee;
  onClose: () => void;
}

function keyOf({ orgUnitId, positionId }: EmployeeAssignment) {
  return `${orgUnitId}:${positionId}`;
}

function describeAssignment({
  position,
  orgUnitName,
  isSecondary,
}: EmployeeAssignment) {
  return `${position}, ${orgUnitName}${isSecondary ? " (совмещение)" : ""}`;
}

/**
 * Dialog that moves one assignment of an employee, with its position and legal entities, to another active unit.
 * The employee is read when the dialog opens, so the move is based on the current version. Mount it only while it is shown.
 */
export function MoveEmployeeModal({
  employee,
  onClose,
}: MoveEmployeeModalProps) {
  return (
    <AppModal
      open
      title="Перемещение сотрудника"
      onCancel={onClose}
      footer={null}
    >
      <LoadedEmployee employee={employee} onClose={onClose} />
    </AppModal>
  );
}

function LoadedEmployee({ employee, onClose }: MoveEmployeeModalProps) {
  const {
    data: latest,
    error,
    isPending,
    refetch,
  } = useQuery(employeeQuery(employee.id));

  if (isPending) return <Skeleton active />;
  if (latest?.version == null) {
    return (
      <ErrorAlert
        title="Не удалось загрузить сотрудника"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return (
    <MoveForm
      shown={shownAssignments(employee)}
      latest={{ employee: latest, version: latest.version }}
      onClose={onClose}
    />
  );
}

/**
 * The form with its save: the move is refused when the version is stale, and the employee is reread whatever the outcome.
 * Warns before saving when the position will be added to the new unit's list; moving the head of a unit out of it is confirmed first.
 */
function MoveForm({
  shown,
  latest,
  onClose,
}: {
  shown: readonly EmployeeAssignment[];
  latest: { employee: Employee; version: number };
  onClose: () => void;
}) {
  const [form] = Form.useForm<MoveFormValues>();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const confirmUnitLosesHead = useConfirmUnitLosesHead();
  const { base = latest, noteSaveFailure } = useEditBase(latest);
  const { data: units } = useQuery(orgUnitTreeQuery);

  const assignmentKey = Form.useWatch("assignment", form);
  const toOrgUnitId = Form.useWatch("toOrgUnitId", form);
  const first = shown[0] ?? mainAssignmentOf(base.employee);
  const chosen = shown.find((one) => keyOf(one) === assignmentKey) ?? first;
  const moved =
    [mainAssignmentOf(base.employee), ...base.employee.assignments].find(
      (one) => keyOf(one) === keyOf(chosen),
    ) ?? chosen;
  const headedUnit = !moved.isSecondary
    ? units?.find(
        (unit) =>
          unit.id === moved.orgUnitId &&
          unit.headEmployeeId === base.employee.id,
      )
    : undefined;
  const targetIds = useMemo(
    () =>
      new Set(
        units
          ?.filter((unit) => unit.isActive && unit.id !== moved.orgUnitId)
          .map((unit) => unit.id),
      ),
    [units, moved.orgUnitId],
  );
  const { data: targetPositions } = useQuery({
    ...unitPositionsQuery(toOrgUnitId ?? ""),
    enabled: toOrgUnitId !== undefined,
  });
  const isAddedToList = targetPositions?.every(
    (one) => one.positionId !== moved.positionId,
  );

  const move = useMutation({
    mutationFn: async (toId: string) =>
      unwrap(
        await api.POST("/api/v1/employees/{id}/move", {
          params: {
            path: { id: base.employee.id },
            header: { "If-Match": ifMatchOf(base.version) },
          },
          body: {
            fromOrgUnitId: moved.orgUnitId,
            positionId: moved.positionId,
            toOrgUnitId: toId,
          },
        }),
      ),
    onSuccess: async () => {
      await invalidateOrgStructure(queryClient);
      message.success("Сотрудник перемещён");
      onClose();
    },
    onError: (failure) => {
      noteSaveFailure(failure);
      showFieldErrors(form, failure);
      return invalidateOrgStructure(queryClient);
    },
  });

  async function submit({ toOrgUnitId: toId }: MoveFormValues) {
    if (toId === undefined) return;
    if (headedUnit && !(await confirmUnitLosesHead(headedUnit.name))) return;
    move.mutate(toId);
  }

  return (
    <Form
      form={form}
      name="move-employee"
      layout="vertical"
      disabled={move.isPending}
      initialValues={{ assignment: keyOf(first) }}
      onFinish={submit}
    >
      <Space orientation="vertical" size="large" style={{ display: "flex" }}>
        {move.isError && (
          <ErrorAlert
            title="Не удалось переместить сотрудника"
            error={move.error}
          />
        )}
        <Typography.Text>
          Сотрудник: <strong>{base.employee.fullName}</strong>
        </Typography.Text>
        {shown.length > 1 ? (
          <Form.Item name="assignment" label="Перемещаемое назначение">
            <Select
              options={shown.map((one) => ({
                value: keyOf(one),
                label: describeAssignment(one),
              }))}
            />
          </Form.Item>
        ) : (
          <Typography.Text>
            Назначение: {describeAssignment(moved)}
          </Typography.Text>
        )}
        <Typography.Text type="secondary">
          {moved.legalEntities.length > 0
            ? `Юрлица назначения сохраняются: ${moved.legalEntities.map(({ name }) => name).join(", ")}.`
            : "У назначения нет юрлиц."}
        </Typography.Text>
        <Form.Item
          name="toOrgUnitId"
          label="Новое подразделение"
          rules={[{ required: true, message: "Выберите подразделение." }]}
        >
          <OrgUnitSelect allowedIds={targetIds} />
        </Form.Item>
        {isAddedToList && (
          <Alert
            type="warning"
            showIcon
            title={`Должность «${moved.position}» будет добавлена в перечень подразделения.`}
          />
        )}
        <Space>
          <Button type="primary" htmlType="submit" loading={move.isPending}>
            Переместить
          </Button>
          <Button onClick={onClose}>Отмена</Button>
        </Space>
      </Space>
    </Form>
  );
}
