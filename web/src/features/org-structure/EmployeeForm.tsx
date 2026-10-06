import { Button, Form, Input, Space, Switch } from "antd";
import { useState } from "react";
import { useNavigate } from "react-router";
import { showFieldErrors } from "../../app/apiErrors";
import { OrgUnitSelect } from "./OrgUnitSelect";
import type { EmployeeInput } from "./orgStructureApi";
import { paths } from "./paths";

type EmployeeFormValues = Partial<EmployeeInput>;

interface EmployeeFormProps {
  /** Values to start from; a new employee starts empty and working. */
  initialValues?: EmployeeFormValues;
  submitLabel: string;
  /** Saves the input; a rejection is shown on the fields it names and by the caller. */
  onSubmit: (input: EmployeeInput) => Promise<unknown>;
}

const NEW_EMPLOYEE_VALUES: EmployeeFormValues = { isActive: true };

/** Fields of an employee, shared by creation and editing; a transfer is a change of the unit, and leaving is switching off the status. */
export function EmployeeForm({
  initialValues = NEW_EMPLOYEE_VALUES,
  submitLabel,
  onSubmit,
}: EmployeeFormProps) {
  const [form] = Form.useForm<EmployeeInput>();
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(values: EmployeeInput) {
    setIsSubmitting(true);
    try {
      await onSubmit(values);
    } catch (error) {
      showFieldErrors(form, error);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Form
      form={form}
      name="employee"
      layout="vertical"
      initialValues={initialValues}
      onFinish={submit}
    >
      <Form.Item
        name="fullName"
        label="ФИО"
        rules={[{ required: true, whitespace: true, message: "Введите ФИО" }]}
      >
        <Input autoComplete="off" />
      </Form.Item>
      <Form.Item
        name="orgUnitId"
        label="Подразделение"
        extra="Сотрудника можно перевести, выбрав другое подразделение."
        rules={[{ required: true, message: "Выберите подразделение" }]}
      >
        <OrgUnitSelect isActiveRequired placeholder="Выберите подразделение" />
      </Form.Item>
      <Form.Item
        name="position"
        label="Должность"
        rules={[
          { required: true, whitespace: true, message: "Введите должность" },
        ]}
      >
        <Input autoComplete="off" />
      </Form.Item>
      <Form.Item
        name="isActive"
        label="Работает"
        valuePropName="checked"
        extra="Если сотрудник не работает, привязанная к нему учётная запись не сможет войти в систему."
      >
        <Switch />
      </Form.Item>
      <Space>
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          {submitLabel}
        </Button>
        <Button onClick={() => navigate(paths.employees)}>Отмена</Button>
      </Space>
    </Form>
  );
}
