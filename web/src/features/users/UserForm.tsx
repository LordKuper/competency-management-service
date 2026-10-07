import { Button, Form, Input, Select, Space } from "antd";
import { useState } from "react";
import { showFieldErrors } from "../../app/apiErrors";
import type { UserRole } from "../../app/featureContract";
import { PASSWORD_HINT } from "../auth/passwordPolicy";
import { EmployeePicker } from "./EmployeePicker";
import { ROLE_OPTIONS } from "./roles";

/** What the account form submits; `password` is present only on the create form. */
export interface UserInput {
  email: string;
  role: UserRole;
  employeeId: string | null;
  password?: string;
}

interface UserFormValues {
  email: string;
  role: UserRole;
  employeeId?: string;
  password?: string;
}

interface UserFormProps {
  /** Values to start from; omitted for a new account. */
  initialValues?: UserFormValues;
  /** The employee already bound, listed by name even when the search does not return them. */
  employee?: { id: string; name: string };
  /** Whether the form creates an account, which adds the password field. */
  isNew?: boolean;
  submitLabel: string;
  /** Saves the input; a rejection is shown on the fields it names and by the caller. */
  onSubmit: (input: UserInput) => Promise<unknown>;
  onCancel: () => void;
}

const NEW_ACCOUNT_VALUES: UserFormValues = { email: "", role: "User" };

/**
 * Account fields shared by creation and editing. The employee is optional for a user and not allowed for an administrator,
 * so that role hides the picker and submits none, mirroring the server rules so the administrator is told early; the server still decides.
 */
export function UserForm({
  initialValues = NEW_ACCOUNT_VALUES,
  employee,
  isNew = false,
  submitLabel,
  onSubmit,
  onCancel,
}: UserFormProps) {
  const [form] = Form.useForm<UserFormValues>();
  const role = Form.useWatch("role", form);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(values: UserFormValues) {
    setIsSubmitting(true);
    try {
      await onSubmit({
        ...values,
        employeeId: values.role === "User" ? (values.employeeId ?? null) : null,
      });
    } catch (error) {
      showFieldErrors(form, error);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Form
      form={form}
      name="user"
      layout="vertical"
      noValidate
      initialValues={initialValues}
      onFinish={submit}
    >
      <Form.Item
        name="email"
        label="E-mail"
        rules={[
          { required: true, whitespace: true, message: "Введите e-mail" },
        ]}
      >
        <Input type="email" autoComplete="off" placeholder="name@example.com" />
      </Form.Item>
      {isNew && (
        <Form.Item
          name="password"
          label="Пароль"
          extra={PASSWORD_HINT}
          rules={[{ required: true, message: "Введите пароль" }]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>
      )}
      <Form.Item
        name="role"
        label="Роль"
        extra={
          role === "GlobalAdmin"
            ? "Глобальный администратор не привязывается к сотруднику."
            : undefined
        }
        rules={[{ required: true, message: "Выберите роль" }]}
      >
        <Select
          options={ROLE_OPTIONS}
          onChange={(next: UserRole) => {
            if (next !== "User") form.setFieldValue("employeeId", undefined);
          }}
        />
      </Form.Item>
      {role === "User" && (
        <Form.Item
          name="employeeId"
          label="Сотрудник"
          extra="Необязательно: пользователь может быть привязан к одному работающему сотруднику или не привязан ни к кому."
        >
          <EmployeePicker current={employee} />
        </Form.Item>
      )}
      <Space>
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          {submitLabel}
        </Button>
        <Button onClick={onCancel}>Отмена</Button>
      </Space>
    </Form>
  );
}
