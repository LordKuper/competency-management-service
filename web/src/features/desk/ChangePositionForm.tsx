import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Button, Flex, Form } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useFormSubmit } from "../../app/useFormSubmit";
import {
  type Employee,
  invalidateOrgStructure,
} from "../org-structure/orgStructureApi";
import { PositionSelect } from "../org-structure/PositionSelect";
import { DecisionError } from "./DecisionError";
import { ORG_FINDING_MUTATION, useIsFindingBusy } from "./findingBusy";

interface ChangePositionFormProps {
  /** The employee as the server last returned them, with the version the change is based on. */
  employee: Employee;
  /** Lays the field and the button in one row, for a table cell. */
  isInline?: boolean;
}

/** The choice of a new position from the employee's unit and the button that saves it; the other fields stay as they are. */
export function ChangePositionForm({
  employee,
  isInline = false,
}: ChangePositionFormProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const isBusy = useIsFindingBusy();
  const [form] = Form.useForm<{ positionId?: string }>();
  const change = useMutation({
    mutationKey: ORG_FINDING_MUTATION,
    mutationFn: async (positionId: string) =>
      unwrap(
        await api.PUT("/api/v1/employees/{id}", {
          params: {
            path: { id: employee.id },
            header: { "If-Match": ifMatchOf(employee.version ?? 0) },
          },
          body: {
            lastName: employee.lastName,
            firstName: employee.firstName,
            middleName: employee.middleName,
            orgUnitId: employee.orgUnitId,
            positionId,
          },
        }),
      ),
    onSuccess: () =>
      message.success(`Должность сотрудника ${employee.fullName} изменена`),
    onSettled: () => invalidateOrgStructure(queryClient),
  });
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
        name={`employee-position-${employee.id}`}
        layout={isInline ? "inline" : "vertical"}
        disabled={isBusy}
        onFinish={submit}
      >
        <Form.Item
          name="positionId"
          label={isInline ? undefined : "Новая должность"}
          rules={[{ required: true, message: "Выберите должность" }]}
          style={isInline ? { minWidth: 220 } : undefined}
        >
          <PositionSelect orgUnitId={employee.orgUnitId} />
        </Form.Item>
        <Button htmlType="submit" loading={change.isPending}>
          Сменить должность
        </Button>
      </Form>
    </Flex>
  );
}
