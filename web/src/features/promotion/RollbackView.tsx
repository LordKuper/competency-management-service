import {
  Alert,
  Button,
  Col,
  DatePicker,
  Flex,
  Form,
  Row,
  Select,
  Typography,
} from "antd";
import dayjs, { type Dayjs } from "dayjs";
import { useState } from "react";
import { showFieldErrors } from "../../app/apiErrors";
import type { RollbackInput, RollbackPreview, Wave } from "./promotionApi";
import {
  rollbackConsequences,
  STAGE_COUNT,
  STAGE_TITLES,
} from "./promotionTexts";
import { rollbackEndDates } from "./stageDates";

const DATE_FORMAT = "DD.MM.YYYY";
const ISO_DATE = "YYYY-MM-DD";

interface RollbackFormValues {
  stage: number;
  endsOn: Dayjs[];
}

interface RollbackViewProps {
  /** The wave being sent back; it has passed at least one stage. */
  wave: Wave;
  /** Default length of every stage in days, which fills the new end dates when a stage is chosen. */
  durations: readonly number[];
  /** Finds what the return would reset; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onPreview: (input: RollbackInput) => Promise<RollbackPreview>;
  /** Makes the return; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: RollbackInput) => Promise<unknown>;
}

interface Confirmation {
  input: RollbackInput;
  preview: RollbackPreview;
}

function isBeforeToday(day: Dayjs): boolean {
  return day.isBefore(dayjs(), "day");
}

/** Sending a wave back to a stage that has passed: the stage and the new end dates, then the list of what resets and a confirmation. */
export function RollbackView({
  wave,
  durations,
  onPreview,
  onSubmit,
}: RollbackViewProps) {
  const [form] = Form.useForm<RollbackFormValues>();
  const [confirmation, setConfirmation] = useState<Confirmation>();
  const [isBusy, setIsBusy] = useState(false);
  const stage = Form.useWatch("stage", form);

  async function attempt(action: () => Promise<unknown>) {
    setIsBusy(true);
    try {
      await action();
    } catch (error) {
      setConfirmation(undefined);
      showFieldErrors(form, error);
    } finally {
      setIsBusy(false);
    }
  }

  function inputOfForm(): RollbackInput {
    return {
      stage: form.getFieldValue("stage"),
      endsOn: form
        .getFieldValue("endsOn")
        .map((date: Dayjs) => date.format(ISO_DATE)),
    };
  }

  function showConsequences() {
    const input = inputOfForm();
    return attempt(async () =>
      setConfirmation({ input, preview: await onPreview(input) }),
    );
  }

  return (
    <Flex vertical gap="middle">
      <Form
        form={form}
        name="promotion-rollback"
        layout="vertical"
        disabled={isBusy || confirmation !== undefined}
        onFinish={showConsequences}
        onValuesChange={(changed) => {
          if (changed.stage && durations.length >= STAGE_COUNT) {
            form.setFieldValue(
              "endsOn",
              rollbackEndDates(changed.stage, wave.endsOn, durations),
            );
          }
        }}
      >
        <Form.Item
          name="stage"
          label="Этап, на который откатить волну"
          rules={[{ required: true, message: "Выберите этап" }]}
        >
          <Select
            placeholder="Выберите этап"
            options={STAGE_TITLES.slice(0, wave.stage - 1).map(
              (title, index) => ({
                value: index + 1,
                label: `${index + 1}. ${title}`,
              }),
            )}
          />
        </Form.Item>
        {stage !== undefined && (
          <Row gutter={16}>
            {STAGE_TITLES.map((title, index) =>
              index + 1 < stage ? null : (
                <Col key={title} xs={24} md={12}>
                  <Form.Item
                    name={["endsOn", index]}
                    label={`Завершение этапа «${title}»`}
                    rules={[{ required: true, message: "Укажите дату" }]}
                  >
                    <DatePicker
                      format={DATE_FORMAT}
                      disabledDate={isBeforeToday}
                    />
                  </Form.Item>
                </Col>
              ),
            )}
          </Row>
        )}
        {!confirmation && (
          <Button type="primary" htmlType="submit" loading={isBusy}>
            Показать последствия
          </Button>
        )}
      </Form>
      {confirmation && (
        <>
          <Alert
            type="warning"
            showIcon
            title={`Откатить волну на этап «${STAGE_TITLES[confirmation.input.stage - 1]}»?`}
            description={
              <RollbackConsequences preview={confirmation.preview} />
            }
          />
          <Flex gap="small">
            <Button
              danger
              type="primary"
              loading={isBusy}
              onClick={() => void attempt(() => onSubmit(confirmation.input))}
            >
              Откатить
            </Button>
            <Button
              disabled={isBusy}
              onClick={() => setConfirmation(undefined)}
            >
              Назад
            </Button>
          </Flex>
        </>
      )}
    </Flex>
  );
}

/** What stays and what resets or is computed again when a wave is sent back, with the number of applications. */
function RollbackConsequences({ preview }: { preview: RollbackPreview }) {
  const lines = rollbackConsequences(preview);
  return (
    <Flex vertical gap="small">
      <Typography.Text>
        Всё введённое сохранится: заявки, оценки, артефакты, итоговые баллы и
        решения.
      </Typography.Text>
      {lines.length > 0 && (
        <ul style={{ margin: 0 }}>
          {lines.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      )}
    </Flex>
  );
}
