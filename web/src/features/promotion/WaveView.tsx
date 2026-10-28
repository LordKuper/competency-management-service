import {
  Alert,
  Button,
  Col,
  DatePicker,
  Flex,
  Form,
  Row,
  Tag,
  Typography,
} from "antd";
import dayjs, { type Dayjs } from "dayjs";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { OrgUnitTreeNode } from "../org-structure/orgStructureApi";
import type { Versioned, Wave, WaveInput } from "./promotionApi";
import {
  PUBLICATION_NOTE,
  STAGE,
  STAGE_COUNT,
  STAGE_TITLES,
  stageLabel,
} from "./promotionTexts";
import { StageSteps } from "./StageSteps";
import { earliestStart, endDatesFrom } from "./stageDates";
import { UnitCheckTree } from "./UnitCheckTree";

const DATE_FORMAT = "DD.MM.YYYY";
const ISO_DATE = "YYYY-MM-DD";

interface WaveFormValues {
  startsOn: Dayjs;
  endsOn: Dayjs[];
  unitIds: string[];
}

interface WaveViewProps {
  /** The wave being changed; omitted when a wave is announced. */
  wave?: Versioned<Wave>;
  /** The units the wave may include, with the ones it already has. */
  units: readonly OrgUnitTreeNode[];
  /** Default length of every stage in working days, which fills the end dates when the start date is entered. */
  durations: readonly number[];
  /** Whether the viewer may change the wave. */
  canEdit: boolean;
  /** Saves the wave; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: WaveInput) => Promise<unknown>;
  /** Opens the return of a started wave to a stage that has passed; the action is offered only when given. */
  onRollback?: () => void;
}

function isBeforeToday(day: Dayjs): boolean {
  return day.isBefore(dayjs(), "day");
}

function isBeforeEarliestStart(announcedOn?: string) {
  return (day: Dayjs): boolean =>
    day.isBefore(earliestStart(announcedOn), "day");
}

function initialValuesOf(wave?: Wave): Partial<WaveFormValues> {
  return {
    startsOn: wave && dayjs(wave.startsOn),
    endsOn: wave?.endsOn.map((date) => dayjs(date)) ?? [],
    unitIds: wave?.units.map((unit) => unit.id) ?? [],
  };
}

/** The dates and units of a wave, and where it stands: once it has started its start date, its units and the dates of finished stages are fixed. */
export function WaveView({
  wave,
  units,
  durations,
  canEdit,
  onSubmit,
  onRollback,
}: WaveViewProps) {
  const [form] = Form.useForm<WaveFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit({
      startsOn: values.startsOn.format(ISO_DATE),
      endsOn: values.endsOn.map((date) => date.format(ISO_DATE)),
      unitIds: values.unitIds,
    });
    setIsDirty(false);
  });
  const { confirmLeave } = useLeaveGuard({
    isUnsaved: isDirty,
    isSaving: isSubmitting,
  });

  const stage = wave?.stage ?? 0;
  const isOver = wave !== undefined && (wave.isFinished || stage > STAGE_COUNT);
  const isReadOnly = !canEdit || isOver;
  const isStarted = stage >= 1;
  const isLocked = isSubmitting || isReadOnly;
  const disabledEndDate = wave ? undefined : isBeforeToday;
  const disabledStartDate = isBeforeEarliestStart(wave?.announcedOn);
  const waveUnitIds = new Set(wave?.units.map((unit) => unit.id));

  return (
    <Row gutter={[24, 24]}>
      <Col xs={24} lg={16}>
        <Flex vertical gap="middle">
          {wave && (
            <Flex align="center" gap="small" wrap>
              <Tag color={isOver ? "default" : "processing"}>
                {stageLabel(stage)}
              </Tag>
              <Typography.Text type="secondary">
                Волну ведёт {wave.authorName}
              </Typography.Text>
              {onRollback && !isReadOnly && stage > 1 && (
                <Button
                  disabled={isSubmitting}
                  onClick={() => confirmLeave(onRollback)}
                >
                  Откатить на этап
                </Button>
              )}
            </Flex>
          )}
          {!canEdit && wave && (
            <Alert
              type="info"
              showIcon
              title="Волну можно только смотреть"
              description={`Изменения вносит ${wave.authorName}.`}
            />
          )}
          <Form
            form={form}
            name="promotion-wave"
            layout="vertical"
            disabled={isSubmitting}
            initialValues={initialValuesOf(wave)}
            onFinish={submit}
            onValuesChange={(changed) => {
              if (changed.startsOn && durations.length >= STAGE_COUNT) {
                form.setFieldValue(
                  "endsOn",
                  endDatesFrom(changed.startsOn, durations),
                );
              }
              setIsDirty(form.isFieldsTouched());
            }}
          >
            <Form.Item
              name="startsOn"
              label="Начало приёма заявок"
              rules={[{ required: true, message: "Укажите дату" }]}
              extra={isStarted && !isOver ? "Волна началась" : undefined}
            >
              <DatePicker
                format={DATE_FORMAT}
                disabledDate={disabledStartDate}
                disabled={isLocked || isStarted}
              />
            </Form.Item>
            <Row gutter={16}>
              {STAGE_TITLES.map((title, index) => (
                <Col key={title} xs={24} md={12}>
                  <Form.Item
                    name={["endsOn", index]}
                    label={`Завершение этапа «${title}»`}
                    rules={[{ required: true, message: "Укажите дату" }]}
                    extra={
                      index + 1 < stage && !isOver
                        ? "Этап завершён"
                        : index + 1 === STAGE.review
                          ? PUBLICATION_NOTE
                          : undefined
                    }
                  >
                    <DatePicker
                      format={DATE_FORMAT}
                      disabledDate={disabledEndDate}
                      disabled={isLocked || index + 1 < stage}
                    />
                  </Form.Item>
                </Col>
              ))}
            </Row>
            <Form.Item
              name="unitIds"
              label="Подразделения"
              rules={[
                {
                  type: "array",
                  min: 1,
                  message: "Выберите хотя бы одно подразделение",
                },
              ]}
              extra={isStarted && !isOver ? "Волна началась" : undefined}
            >
              <UnitCheckTree
                units={
                  isStarted
                    ? units.filter((unit) => waveUnitIds.has(unit.id))
                    : units
                }
                disabled={isLocked || isStarted}
              />
            </Form.Item>
            {!isReadOnly && (
              <Button type="primary" htmlType="submit" loading={isSubmitting}>
                {wave ? "Сохранить" : "Объявить волну"}
              </Button>
            )}
          </Form>
        </Flex>
      </Col>
      {wave && (
        <Col xs={24} lg={8}>
          <StageSteps stage={stage} />
        </Col>
      )}
    </Row>
  );
}
