import {
  App,
  Button,
  Col,
  Flex,
  Form,
  InputNumber,
  Row,
  Typography,
} from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { PromotionSettings, SettingsInput } from "./promotionApi";
import { STAGE_TITLES } from "./promotionTexts";

/** Largest interval in months and stage length in days the fields offer, as the server accepts them; the OpenAPI document does not carry the limits. */
const MAX_INTERVAL_MONTHS = 120;
const MAX_STAGE_DAYS = 365;

interface SettingsViewProps {
  settings: PromotionSettings;
  /** Saves the settings; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: SettingsInput) => Promise<unknown>;
}

/** The form of the promotion settings: the interval between promotions and the default length of every stage. */
export function SettingsView({ settings, onSubmit }: SettingsViewProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<SettingsInput>();
  const [isDirty, setIsDirty] = useState(false);
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit(values);
    setIsDirty(false);
    message.success("Настройки сохранены");
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });

  return (
    <Form
      form={form}
      name="promotion-settings"
      layout="vertical"
      disabled={isSubmitting}
      initialValues={settings}
      onFinish={submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      <Form.Item
        name="minIntervalMonths"
        label="Минимальный интервал между повышениями, месяцев"
        rules={[{ required: true, message: "Укажите число месяцев" }]}
      >
        <InputNumber min={0} max={MAX_INTERVAL_MONTHS} precision={0} />
      </Form.Item>
      <Typography.Title level={2}>Длительность этапов, дней</Typography.Title>
      <Row gutter={[16, 0]}>
        {STAGE_TITLES.map((title, index) => (
          <Col key={title} xs={24} md={12} lg={8}>
            <Form.Item
              name={["stageDurationsDays", index]}
              label={`${index + 1}. ${title}`}
              rules={[{ required: true, message: "Укажите число дней" }]}
            >
              <InputNumber min={1} max={MAX_STAGE_DAYS} precision={0} />
            </Form.Item>
          </Col>
        ))}
      </Row>
      <Flex>
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          Сохранить
        </Button>
      </Flex>
    </Form>
  );
}
