import {
  theme as antdTheme,
  Button,
  Col,
  ConfigProvider,
  Flex,
  Form,
  Input,
  Row,
  Space,
  Typography,
} from "antd";
import type { ReactNode } from "react";
import { useFormSubmit } from "../../app/useFormSubmit";
import { OrgUnitSelect } from "./OrgUnitSelect";
import type { EmployeeInput } from "./orgStructureApi";
import { PositionSelect } from "./PositionSelect";

type EmployeeFormValues = Partial<EmployeeInput>;

interface EmployeeFormProps {
  initialValues: EmployeeFormValues;
  /** The position the employee holds now, kept as a choice only while the unit stays the employee's; omitted for a new employee. */
  position?: { id: string; name: string };
  /** E-mail of the employee's account, shown without a way to change it; null means no account, omitted hides the row. */
  email?: string | null;
  /** Told when the position field adds a new position to the unit's list. */
  onPositionAdded?: (position: { id: string; name: string }) => void;
  /** Told whether the form holds input that was not saved, so a screen can refuse to be left. */
  onDirtyChange?: (isDirty: boolean) => void;
  submitLabel: string;
  /** Saves the input; a rejection is shown on the fields it names and by the caller. */
  onSubmit: (input: EmployeeInput) => Promise<unknown>;
  /** Leaves the form; omitted when `scenarioActions` carries the buttons. */
  onCancel?: () => void;
  /**
   * Lays the form out as a step of a scenario: short fields side by side and one row of buttons, `start` at the left and
   * `end` before the submit button at the right, in place of the cancel button.
   */
  scenarioActions?: { start?: ReactNode; end?: ReactNode };
}

/**
 * Fields of an employee, shared by creation and editing; a transfer is a change of the unit. The status is not a field: it changes through dismissal and rehiring.
 * The position is chosen from those allowed in the chosen unit, so the choice follows the unit field.
 */
export function EmployeeForm({
  initialValues,
  position,
  email,
  onPositionAdded,
  onDirtyChange,
  submitLabel,
  onSubmit,
  onCancel,
  scenarioActions,
}: EmployeeFormProps) {
  const { token } = antdTheme.useToken();
  const [form] = Form.useForm<EmployeeInput>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);
  const orgUnitId = Form.useWatch("orgUnitId", form);

  const lastName = (
    <Form.Item
      name="lastName"
      label="Фамилия"
      rules={[{ required: true, whitespace: true, message: "Введите фамилию" }]}
    >
      <Input autoComplete="off" />
    </Form.Item>
  );
  const firstName = (
    <Form.Item
      name="firstName"
      label="Имя"
      rules={[{ required: true, whitespace: true, message: "Введите имя" }]}
    >
      <Input autoComplete="off" />
    </Form.Item>
  );
  const middleName = (
    <Form.Item name="middleName" label="Отчество">
      <Input autoComplete="off" />
    </Form.Item>
  );
  const unit = (
    <Form.Item
      name="orgUnitId"
      label="Подразделение"
      extra={
        scenarioActions
          ? undefined
          : "Сотрудника можно перевести, выбрав другое подразделение."
      }
      rules={[{ required: true, message: "Выберите подразделение" }]}
    >
      <OrgUnitSelect isActiveRequired placeholder="Выберите подразделение" />
    </Form.Item>
  );
  const positionField = (
    <Form.Item
      name="positionId"
      label="Должность"
      rules={[{ required: true, message: "Выберите должность" }]}
    >
      <PositionSelect
        orgUnitId={orgUnitId}
        onAdded={onPositionAdded}
        current={orgUnitId === initialValues.orgUnitId ? position : undefined}
      />
    </Form.Item>
  );
  const submitButton = (
    <Button type="primary" htmlType="submit" loading={isSubmitting}>
      {submitLabel}
    </Button>
  );

  return (
    <Form
      form={form}
      name="employee"
      layout="vertical"
      initialValues={initialValues}
      onFinish={submit}
      onValuesChange={() => onDirtyChange?.(form.isFieldsTouched())}
    >
      {scenarioActions ? (
        <ConfigProvider
          theme={{ components: { Form: { itemMarginBottom: 0 } } }}
        >
          <Flex vertical gap="middle">
            <Row gutter={[token.margin, token.margin]}>
              <Col xs={24} md={8}>
                {lastName}
              </Col>
              <Col xs={24} md={8}>
                {firstName}
              </Col>
              <Col xs={24} md={8}>
                {middleName}
              </Col>
            </Row>
            <Row gutter={[token.margin, token.margin]}>
              <Col xs={24} md={12}>
                {unit}
              </Col>
              <Col xs={24} md={12}>
                {positionField}
              </Col>
            </Row>
            <Flex justify="space-between" align="center" gap="small" wrap>
              <Flex align="center" gap="small" wrap>
                {scenarioActions.start}
              </Flex>
              <Flex align="center" gap="small" wrap>
                {scenarioActions.end}
                {submitButton}
              </Flex>
            </Flex>
          </Flex>
        </ConfigProvider>
      ) : (
        <>
          {lastName}
          {firstName}
          {middleName}
          {unit}
          {positionField}
          {email !== undefined && (
            <Form.Item
              label="E-mail"
              extra="E-mail берётся из учётной записи сотрудника и меняется в разделе «Пользователи»."
            >
              <Typography.Text>{email ?? "Нет учётной записи"}</Typography.Text>
            </Form.Item>
          )}
          <Space>
            {submitButton}
            <Button onClick={onCancel}>Отмена</Button>
          </Space>
        </>
      )}
    </Form>
  );
}
