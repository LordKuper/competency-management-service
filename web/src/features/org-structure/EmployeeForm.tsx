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
import { AssignmentFields } from "./AssignmentFields";
import { LegalEntitySelect } from "./LegalEntitySelect";
import { OrgUnitSelect } from "./OrgUnitSelect";
import type { Employee, EmployeeInput } from "./orgStructureApi";
import { PositionSelect } from "./PositionSelect";

type EmployeeFormValues = Partial<EmployeeInput>;

interface EmployeeFormProps {
  initialValues: EmployeeFormValues;
  /** The position the employee holds now, kept as a choice only while it stays chosen in the unit that has it; omitted for a new employee. */
  position?: { id: string; name: string };
  /** The legal entities and secondary assignments the employee holds now, so what the lists no longer offer stays a choice while unchanged; omitted for a new employee. */
  saved?: Pick<Employee, "legalEntities" | "assignments">;
  /** A position chosen before the form was opened; it stays a choice under any unit. */
  preselectedPosition?: { id: string; name: string };
  /** Help lines under the unit and position fields, for a form opened with a choice made elsewhere; also puts the focus on the surname. */
  hints?: { orgUnit?: string; position?: string };
  /** E-mail of the employee's account, shown without a way to change it; null means no account, omitted hides the row. */
  email?: string | null;
  /** Told when the position field adds a new position to the unit's list. */
  onPositionAdded?: (position: { id: string; name: string }) => void;
  /** Told whether the form holds input that was not saved, so a screen can ask before it is left. */
  onDirtyChange?: (isDirty: boolean) => void;
  submitLabel: string;
  /** Saves the input; a rejection is shown on the fields it names and by the caller. */
  onSubmit: (input: EmployeeInput) => Promise<unknown>;
  /** Leaves the form; without it, and without `scenarioActions`, the form offers only its submit button. */
  onCancel?: () => void;
  /**
   * Lays the form out as a step of a scenario: one field to a row, the three name fields as one row, and one row of buttons, `start` at the left and
   * `end` before the submit button at the right, in place of the cancel button.
   */
  scenarioActions?: { start?: ReactNode; end?: ReactNode };
}

/**
 * Fields of an employee, shared by creation and editing; a transfer is a change of the unit. The status is not a field: it changes through dismissal and rehiring.
 * The position is chosen from those allowed in the chosen unit, so the choice follows the unit field. The main assignment, its legal entities
 * and the secondary assignments are one set that a save replaces whole.
 */
export function EmployeeForm({
  initialValues,
  position,
  saved,
  preselectedPosition,
  hints,
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
  const positionId = Form.useWatch("positionId", form);
  const heldPositions = [
    ...(position && initialValues.orgUnitId
      ? [{ orgUnitId: initialValues.orgUnitId, position }]
      : []),
    ...(saved?.assignments ?? []).map((assignment) => ({
      orgUnitId: assignment.orgUnitId,
      position: { id: assignment.positionId, name: assignment.position },
    })),
  ];
  const keptPosition = (unitId?: string, chosenId?: string) =>
    heldPositions.find(
      (held) => held.orgUnitId === unitId && held.position.id === chosenId,
    )?.position;
  const knownEntities = [
    ...(saved?.legalEntities ?? []),
    ...(saved?.assignments ?? []).flatMap(({ legalEntities }) => legalEntities),
  ];

  const lastName = (
    <Form.Item
      name="lastName"
      label="Фамилия"
      rules={[{ required: true, whitespace: true, message: "Введите фамилию" }]}
    >
      <Input autoComplete="off" autoFocus={hints !== undefined} />
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
          ? hints?.orgUnit
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
      extra={hints?.position}
      rules={[{ required: true, message: "Выберите должность" }]}
    >
      <PositionSelect
        orgUnitId={orgUnitId}
        onAdded={onPositionAdded}
        preselected={preselectedPosition}
        current={keptPosition(orgUnitId, positionId)}
      />
    </Form.Item>
  );
  const legalEntities = (
    <Form.Item name="legalEntityIds" label="Юридические лица">
      <LegalEntitySelect known={knownEntities} />
    </Form.Item>
  );
  const assignments = (
    <div
      style={scenarioActions ? undefined : { marginBlockEnd: token.marginLG }}
    >
      <AssignmentFields
        keptPosition={keptPosition}
        knownEntities={knownEntities}
        onMadeMain={() => onDirtyChange?.(true)}
      />
    </div>
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
      disabled={isSubmitting}
      initialValues={{ legalEntityIds: [], assignments: [], ...initialValues }}
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
            {unit}
            {positionField}
            {legalEntities}
            {assignments}
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
          {legalEntities}
          {assignments}
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
            {onCancel && <Button onClick={onCancel}>Отмена</Button>}
          </Space>
        </>
      )}
    </Form>
  );
}
