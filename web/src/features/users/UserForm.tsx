import { Button, Flex, Form, Input, Select, Space } from "antd";
import type { ReactNode } from "react";
import type { UserRole } from "../../app/featureContract";
import { useFormSubmit } from "../../app/useFormSubmit";
import { EmployeePicker } from "./EmployeePicker";
import { ROLE_OPTIONS } from "./roles";

/** What the account form submits. */
export interface UserInput {
  email: string;
  roles: UserRole[];
  employeeId: string | null;
}

interface UserFormValues {
  email: string;
  roles: UserRole[];
  employeeId?: string;
}

interface UserFormProps {
  /** Values to start from; omitted for a new account. */
  initialValues?: UserFormValues;
  /** The employee already bound, listed by name even when the search does not return them. */
  employee?: { id: string; name: string };
  /** Whether the form creates an account, which is then invited by e-mail to set its own password. */
  isNew?: boolean;
  /** Fixes the employee and the roles, none unless given, as when an account is created for a given employee: their fields are not shown. */
  lockedTo?: { employeeId: string; roles?: UserRole[] };
  /** The server refusal to show under the roles, which keeps the selected roles as the administrator left them. */
  rolesError?: string;
  submitLabel: string;
  /** Saves the input; a rejection is shown on the fields it names and by the caller. */
  onSubmit: (input: UserInput) => Promise<unknown>;
  /** Leaves the form; omitted when `scenarioActions` carries the buttons. */
  onCancel?: () => void;
  /** Told whether the form holds input that was not saved, so a screen can ask before it is left. */
  onDirtyChange?: (isDirty: boolean) => void;
  /** Puts the buttons of a scenario step into one row, `start` at the left and `end` before the submit button at the right, in place of the cancel button. */
  scenarioActions?: { start?: ReactNode; end?: ReactNode };
}

const NEW_ACCOUNT_VALUES: UserFormValues = { email: "", roles: [] };

/**
 * Account fields shared by creation and editing. The employee is optional; the server decides whether the binding is allowed.
 */
export function UserForm({
  initialValues = NEW_ACCOUNT_VALUES,
  employee,
  isNew = false,
  lockedTo,
  rolesError,
  submitLabel,
  onSubmit,
  onCancel,
  onDirtyChange,
  scenarioActions,
}: UserFormProps) {
  const [form] = Form.useForm<UserFormValues>();
  const { isSubmitting, submit } = useFormSubmit(form, (values) =>
    onSubmit(
      lockedTo
        ? {
            email: values.email,
            roles: lockedTo.roles ?? [],
            employeeId: lockedTo.employeeId,
          }
        : { ...values, employeeId: values.employeeId ?? null },
    ),
  );

  const emailField = (
    <Form.Item
      name="email"
      label="E-mail"
      extra={
        isNew
          ? "На этот адрес придёт приглашение со ссылкой, по которой пользователь сам задаст пароль."
          : undefined
      }
      rules={[{ required: true, whitespace: true, message: "Введите e-mail" }]}
    >
      <Input type="email" autoComplete="off" placeholder="name@example.com" />
    </Form.Item>
  );

  return (
    <Form
      form={form}
      name="user"
      layout="vertical"
      noValidate
      initialValues={initialValues}
      onFinish={submit}
      onValuesChange={() => onDirtyChange?.(form.isFieldsTouched())}
    >
      {emailField}
      {!lockedTo && (
        <>
          <Form.Item
            name="roles"
            label="Роли"
            validateStatus={rolesError ? "error" : undefined}
            help={rolesError}
          >
            <Select
              mode="multiple"
              options={ROLE_OPTIONS}
              placeholder="Пользователь без ролей"
            />
          </Form.Item>
          <Form.Item name="employeeId" label="Сотрудник">
            <EmployeePicker current={employee} />
          </Form.Item>
        </>
      )}
      {scenarioActions ? (
        <Flex justify="space-between" align="center" gap="small" wrap>
          <Flex align="center" gap="small" wrap>
            {scenarioActions.start}
          </Flex>
          <Flex
            align="center"
            gap="small"
            wrap
            style={{ marginInlineStart: "auto" }}
          >
            {scenarioActions.end}
            <Button type="primary" htmlType="submit" loading={isSubmitting}>
              {submitLabel}
            </Button>
          </Flex>
        </Flex>
      ) : (
        <Space>
          <Button type="primary" htmlType="submit" loading={isSubmitting}>
            {submitLabel}
          </Button>
          <Button onClick={() => onCancel?.()}>Отмена</Button>
        </Space>
      )}
    </Form>
  );
}
