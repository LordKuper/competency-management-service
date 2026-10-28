import {
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Drawer,
  Flex,
  Form,
  Row,
  Tag,
  Typography,
  theme,
} from "antd";
import dayjs, { type Dayjs } from "dayjs";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { OrgUnitTreeNode } from "../org-structure/orgStructureApi";
import { Panel } from "./Panel";
import type { Versioned, Wave, WaveInput } from "./promotionApi";
import {
  formatDate,
  PUBLICATION_NOTE,
  STAGE,
  STAGE_COUNT,
  STAGE_TITLES,
  stageLabel,
} from "./promotionTexts";
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
  /** The session being changed; omitted when a session is announced. */
  wave?: Versioned<Wave>;
  /** The units the session may include, with the ones it already has. */
  units: readonly OrgUnitTreeNode[];
  /** Default length of every stage in working days, which fills the end dates when the start date is entered. */
  durations: readonly number[];
  /** Whether the viewer may change the session. */
  canEdit: boolean;
  /** Saves the session; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: WaveInput) => Promise<unknown>;
  /** Opens the return of a started session to a stage that has passed; the action is offered only when given. */
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

interface StageCardProps {
  index: number;
  stage: number;
  endsOn?: string;
  isLocked: boolean;
  isOver: boolean;
  disabledDate?: (day: Dayjs) => boolean;
}

function StageCard({
  index,
  stage,
  endsOn,
  isLocked,
  isOver,
  disabledDate,
}: StageCardProps) {
  const { token } = theme.useToken();
  const number = index + 1;
  const title = STAGE_TITLES[index];
  const isPassed = number < stage && !isOver;
  const isNow = number === stage && !isOver;
  const hasPublication = number === STAGE.review;
  return (
    <Card
      size="small"
      style={{
        height: "100%",
        borderColor: isNow ? token.colorPrimary : undefined,
        background: isNow ? token.colorInfoBg : undefined,
      }}
    >
      <Flex vertical gap="small">
        <Typography.Text strong>
          {number}. {title}
        </Typography.Text>
        {isPassed ? (
          <>
            <Typography.Text type="secondary">
              Завершён {endsOn ? formatDate(endsOn) : ""}
            </Typography.Text>
            <Form.Item name={["endsOn", index]} hidden noStyle>
              <DatePicker />
            </Form.Item>
          </>
        ) : (
          <Form.Item
            name={["endsOn", index]}
            label={hasPublication ? "Завершение и публикация" : "Завершение"}
            rules={[{ required: true, message: "Укажите дату" }]}
            extra={hasPublication ? PUBLICATION_NOTE : undefined}
            style={{ margin: 0 }}
          >
            <DatePicker
              format={DATE_FORMAT}
              aria-label={`Завершение этапа «${title}»`}
              disabledDate={disabledDate}
              disabled={isLocked}
            />
          </Form.Item>
        )}
      </Flex>
    </Card>
  );
}

/** The dates and units of a career session, and where it stands: once it has started its start date, its units and the dates of finished stages are fixed. */
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
  const [isUnitsOpen, setIsUnitsOpen] = useState(false);
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit({
      startsOn: values.startsOn.format(ISO_DATE),
      endsOn: values.endsOn.map((date) => date.format(ISO_DATE)),
      unitIds: values.unitIds,
    });
    setIsDirty(false);
    setIsUnitsOpen(false);
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
  const unitsField = (
    <Form.Item
      name="unitIds"
      label={wave ? undefined : "Подразделения"}
      rules={[
        {
          type: "array",
          min: 1,
          message: "Выберите хотя бы одно подразделение",
        },
      ]}
      extra={isStarted && !isOver ? "Сессия началась" : undefined}
    >
      <UnitCheckTree
        units={
          isStarted ? units.filter((unit) => waveUnitIds.has(unit.id)) : units
        }
        disabled={isLocked || isStarted}
      />
    </Form.Item>
  );

  return (
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
      <Flex vertical gap="large">
        <Flex justify="space-between" align="end" gap="middle" wrap>
          <Flex vertical>
            <Flex align="center" gap="middle" wrap>
              <Typography.Title level={1} style={{ margin: 0 }}>
                {wave
                  ? `Карьерная сессия с ${formatDate(wave.startsOn)}`
                  : "Новая карьерная сессия"}
              </Typography.Title>
              {wave && (
                <Tag color={isOver ? "default" : "processing"}>
                  {stageLabel(stage)}
                </Tag>
              )}
            </Flex>
            {wave && (
              <Typography.Text type="secondary">
                {wave.units.map((unit) => unit.name).join(", ")} · ведёт{" "}
                {wave.authorName}
              </Typography.Text>
            )}
          </Flex>
          {wave && (
            <Flex gap="small" wrap>
              {onRollback && !isReadOnly && stage > 1 && (
                <Button
                  disabled={isSubmitting}
                  onClick={() => confirmLeave(onRollback)}
                >
                  Откатить на этап…
                </Button>
              )}
              <Button onClick={() => setIsUnitsOpen(true)}>
                Подразделения
              </Button>
            </Flex>
          )}
        </Flex>
        {!canEdit && wave && (
          <Alert
            type="info"
            showIcon
            title="Сессию можно только смотреть"
            description={`Изменения вносит ${wave.authorName}.`}
          />
        )}
        <Panel title="Этапы">
          <Flex vertical gap="middle">
            <Typography.Text type="secondary">
              Конец этапа — срок, к которому заявки должны дойти до нужного
              статуса. Текущий этап можно продлить.
            </Typography.Text>
            <Form.Item
              name="startsOn"
              label="Начало приёма заявок"
              rules={[{ required: true, message: "Укажите дату" }]}
              hidden={isStarted}
              style={{ margin: 0 }}
            >
              <DatePicker
                format={DATE_FORMAT}
                disabledDate={disabledStartDate}
                disabled={isLocked || isStarted}
              />
            </Form.Item>
            <Row gutter={[16, 16]}>
              {STAGE_TITLES.map((title, index) => (
                <Col key={title} xs={24} md={12} xl={8}>
                  <StageCard
                    index={index}
                    stage={stage}
                    endsOn={wave?.endsOn[index]}
                    isLocked={isLocked}
                    isOver={isOver}
                    disabledDate={disabledEndDate}
                  />
                </Col>
              ))}
            </Row>
            {!wave && unitsField}
            {!isReadOnly && (
              <Flex>
                <Button type="primary" htmlType="submit" loading={isSubmitting}>
                  {wave ? "Сохранить даты" : "Объявить карьерную сессию"}
                </Button>
              </Flex>
            )}
          </Flex>
        </Panel>
      </Flex>
      {wave && (
        <Drawer
          title="Подразделения"
          open={isUnitsOpen}
          forceRender
          onClose={() => setIsUnitsOpen(false)}
          footer={
            !isReadOnly && !isStarted ? (
              <Button
                type="primary"
                loading={isSubmitting}
                onClick={() => form.submit()}
              >
                Сохранить
              </Button>
            ) : undefined
          }
        >
          {unitsField}
        </Drawer>
      )}
    </Form>
  );
}
