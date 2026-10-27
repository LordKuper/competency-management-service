import { PlusOutlined } from "@ant-design/icons";
import {
  Button,
  Card,
  ConfigProvider,
  Flex,
  Form,
  Space,
  Typography,
} from "antd";
import { LegalEntitySelect } from "./LegalEntitySelect";
import { OrgUnitSelect } from "./OrgUnitSelect";
import type { EmployeeInput, LegalEntity } from "./orgStructureApi";
import { PositionSelect } from "./PositionSelect";

interface AssignmentFieldsProps {
  /** The position the employee holds now in a unit, when it is the one chosen there; it stays a choice though the unit's list no longer offers it. */
  keptPosition: (
    orgUnitId?: string,
    positionId?: string,
  ) => { id: string; name: string } | undefined;
  /** The legal entities the employee holds now, listed by name wherever the search does not return them. */
  knownEntities: readonly LegalEntity[];
  /** Told when an assignment becomes the main one, which changes the form without a field event. */
  onMadeMain: () => void;
}

interface AssignmentCardProps
  extends Pick<AssignmentFieldsProps, "keptPosition" | "knownEntities"> {
  /** Place of the assignment in the list. */
  index: number;
  onRemove: () => void;
  onMakeMain: () => void;
}

/** One secondary assignment: its unit, its position from those the unit allows, its legal entities, and the two things that can be done with it. */
function AssignmentCard({
  index,
  keptPosition,
  knownEntities,
  onRemove,
  onMakeMain,
}: AssignmentCardProps) {
  const orgUnitId: string | undefined = Form.useWatch([
    "assignments",
    index,
    "orgUnitId",
  ]);
  const positionId: string | undefined = Form.useWatch([
    "assignments",
    index,
    "positionId",
  ]);
  const label = `Совмещение ${index + 1}`;

  return (
    <Card
      size="small"
      title={label}
      extra={
        <Space>
          <Button
            type="link"
            aria-label={`Сделать основным: ${label}`}
            onClick={onMakeMain}
          >
            Сделать основным
          </Button>
          <Button
            type="link"
            aria-label={`Убрать: ${label}`}
            onClick={onRemove}
          >
            Убрать
          </Button>
        </Space>
      }
    >
      <ConfigProvider theme={{ components: { Form: { itemMarginBottom: 0 } } }}>
        <Flex vertical gap="middle">
          <Form.Item
            name={[index, "orgUnitId"]}
            label="Подразделение"
            rules={[{ required: true, message: "Выберите подразделение" }]}
          >
            <OrgUnitSelect
              isActiveRequired
              placeholder="Выберите подразделение"
            />
          </Form.Item>
          <Form.Item
            name={[index, "positionId"]}
            label="Должность"
            rules={[{ required: true, message: "Выберите должность" }]}
          >
            <PositionSelect
              orgUnitId={orgUnitId}
              current={keptPosition(orgUnitId, positionId)}
            />
          </Form.Item>
          <Form.Item name={[index, "legalEntityIds"]} label="Юридические лица">
            <LegalEntitySelect known={knownEntities} />
          </Form.Item>
        </Flex>
      </ConfigProvider>
    </Card>
  );
}

/**
 * The secondary assignments of an employee, added, changed and removed in the form. «Сделать основным» swaps an assignment with the
 * main one, so the employee keeps both and a single save moves the main assignment.
 */
export function AssignmentFields({
  keptPosition,
  knownEntities,
  onMadeMain,
}: AssignmentFieldsProps) {
  const form = Form.useFormInstance<EmployeeInput>();

  function makeMain(index: number) {
    const assignments: EmployeeInput["assignments"] =
      form.getFieldValue("assignments") ?? [];
    const chosen = assignments[index];
    if (!chosen) return;
    const main = {
      orgUnitId: form.getFieldValue("orgUnitId"),
      positionId: form.getFieldValue("positionId"),
      legalEntityIds: form.getFieldValue("legalEntityIds") ?? [],
    };
    form.setFieldsValue({
      ...chosen,
      assignments: assignments.map((assignment, place) =>
        place === index ? main : assignment,
      ),
    });
    onMadeMain();
  }

  return (
    <Flex vertical gap="small">
      <Typography.Title level={5} style={{ margin: 0 }}>
        Совмещения
      </Typography.Title>
      <Form.List name="assignments">
        {(fields, { add, remove }) => (
          <Flex vertical gap="middle">
            {fields.length === 0 && (
              <Typography.Text type="secondary">
                Совмещений нет. Добавьте подразделение и должность, если
                сотрудник работает ещё и там.
              </Typography.Text>
            )}
            {fields.map((field) => (
              <AssignmentCard
                key={field.key}
                index={field.name}
                keptPosition={keptPosition}
                knownEntities={knownEntities}
                onRemove={() => remove(field.name)}
                onMakeMain={() => makeMain(field.name)}
              />
            ))}
            <div>
              <Button
                icon={<PlusOutlined />}
                onClick={() => add({ legalEntityIds: [] })}
              >
                Добавить совмещение
              </Button>
            </div>
          </Flex>
        )}
      </Form.List>
    </Flex>
  );
}
