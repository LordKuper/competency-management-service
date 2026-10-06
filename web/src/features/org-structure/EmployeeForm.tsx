import { Button, Form, Input, Space, Switch, Typography } from "antd";
import { useState } from "react";
import { showFieldErrors } from "../../app/apiErrors";
import { OrgUnitSelect } from "./OrgUnitSelect";
import type { EmployeeInput } from "./orgStructureApi";

type EmployeeFormValues = Partial<EmployeeInput>;

interface EmployeeFormProps {
  initialValues: EmployeeFormValues;
  /** E-mail of the employee's account, shown without a way to change it; null means no account, omitted hides the row. */
  email?: string | null;
  submitLabel: string;
  /** Saves the input; a rejection is shown on the fields it names and by the caller. */
  onSubmit: (input: EmployeeInput) => Promise<unknown>;
  onCancel: () => void;
}

/** Fields of an employee, shared by creation and editing; a transfer is a change of the unit, and leaving is switching off the status. */
export function EmployeeForm({
  initialValues,
  email,
  submitLabel,
  onSubmit,
  onCancel,
}: EmployeeFormProps) {
  const [form] = Form.useForm<EmployeeInput>();
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
      {email !== undefined && (
        <Form.Item
          label="E-mail"
          extra="E-mail берётся из учётной записи сотрудника и меняется в разделе «Пользователи»."
        >
          <Typography.Text>{email ?? "Нет учётной записи"}</Typography.Text>
        </Form.Item>
      )}
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
        <Button onClick={onCancel}>Отмена</Button>
      </Space>
    </Form>
  );
}
