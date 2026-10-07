import { Button, Form, Input, Select, Space } from "antd";
import type { UserRole } from "../../app/featureContract";
import { useFormSubmit } from "../../app/useFormSubmit";
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
 * Account fields shared by creation and editing. The employee is optional for either role; the server decides whether the binding is allowed.
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
  const { isSubmitting, submit } = useFormSubmit(form, (values) =>
    onSubmit({ ...values, employeeId: values.employeeId ?? null }),
  );

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
        rules={[{ required: true, message: "Выберите роль" }]}
      >
        <Select options={ROLE_OPTIONS} />
      </Form.Item>
      <Form.Item
        name="employeeId"
        label="Сотрудник"
        extra="Необязательно: учётная запись может быть привязана к одному работающему сотруднику или не привязана ни к кому."
      >
        <EmployeePicker current={employee} />
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
