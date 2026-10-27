import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Button, Flex, Form } from "antd";
import { useState } from "react";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import {
  type Employee,
  type EmployeeInput,
  employeeInputOf,
  invalidateOrgStructure,
} from "../org-structure/orgStructureApi";
import { PositionSelect } from "../org-structure/PositionSelect";
import { DecisionError } from "./DecisionError";
import { ORG_FINDING_MUTATION, useIsFindingBusy } from "./findingBusy";

/** An assignment of an employee, told apart from the others by its unit and position. */
export interface AssignmentRef {
  orgUnitId: string;
  positionId: string;
}

interface ChangePositionFormProps {
  /** The employee as the server last returned them, with the version the change is based on. */
  employee: Employee;
  /** The assignment whose position changes; the main one when omitted. */
  assignment?: AssignmentRef;
  /** Lays the field and the button in one row, for a table cell. */
  isInline?: boolean;
}

/** The employee's input with the position of one assignment replaced, the main one or a secondary one; everything else stays as it is. */
function withPosition(
  employee: Employee,
  target: AssignmentRef,
  positionId: string,
): EmployeeInput {
  const input = employeeInputOf(employee);
  const isTarget = (held: AssignmentRef) =>
    held.orgUnitId === target.orgUnitId &&
    held.positionId === target.positionId;
  return isTarget(employee)
    ? { ...input, positionId }
    : {
        ...input,
        assignments: input.assignments.map((held) =>
          isTarget(held) ? { ...held, positionId } : held,
        ),
      };
}

/** The choice of a new position from the unit of the assignment and the button that saves it; the other fields and assignments stay as they are. */
export function ChangePositionForm({
  employee,
  assignment = employee,
  isInline = false,
}: ChangePositionFormProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const isBusy = useIsFindingBusy();
  const [form] = Form.useForm<{ positionId?: string }>();
  const [isDirty, setIsDirty] = useState(false);
  const change = useMutation({
    mutationKey: ORG_FINDING_MUTATION,
    mutationFn: async (positionId: string) =>
      unwrap(
        await api.PUT("/api/v1/employees/{id}", {
          params: {
            path: { id: employee.id },
            header: { "If-Match": ifMatchOf(employee.version ?? 0) },
          },
          body: withPosition(employee, assignment, positionId),
        }),
      ),
    onSuccess: () => {
      setIsDirty(false);
      message.success(`Должность сотрудника ${employee.fullName} изменена`);
    },
    onSettled: () => invalidateOrgStructure(queryClient),
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: change.isPending });
  const { submit } = useFormSubmit(form, async (values) => {
    if (values.positionId) await change.mutateAsync(values.positionId);
  });

  return (
    <Flex vertical gap="small">
      {change.isError && (
        <DecisionError title="Должность не изменена" error={change.error} />
      )}
      <Form
        key={employee.version}
        form={form}
        name={`employee-position-${employee.id}-${assignment.orgUnitId}`}
        layout={isInline ? "inline" : "vertical"}
        disabled={isBusy}
        onFinish={submit}
        onValuesChange={() => setIsDirty(form.isFieldsTouched())}
      >
        <Form.Item
          name="positionId"
          label={isInline ? undefined : "Новая должность"}
          rules={[{ required: true, message: "Выберите должность" }]}
          style={
            isInline
              ? { minWidth: "calc(var(--ant-control-height) * 7)" }
              : undefined
          }
        >
          <PositionSelect orgUnitId={assignment.orgUnitId} />
        </Form.Item>
        <Button htmlType="submit" loading={change.isPending}>
          Сменить должность
        </Button>
      </Form>
    </Flex>
  );
}
