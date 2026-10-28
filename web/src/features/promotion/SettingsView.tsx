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
import { Panel } from "./Panel";
import type { PromotionSettings, SettingsInput } from "./promotionApi";
import { STAGE_TITLES } from "./promotionTexts";

/** Largest interval in months and stage length in working days the fields offer, as the server accepts them; the OpenAPI document does not carry the limits. */
const MAX_INTERVAL_MONTHS = 120;
const MAX_STAGE_DAYS = 260;

interface SettingsViewProps {
  settings: PromotionSettings;
  /** Saves the settings; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: SettingsInput) => Promise<unknown>;
}

/** The default rules of career sessions: the interval between career transitions and the default length of every stage. */
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
    <Panel title="Правила по умолчанию">
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
          label="Минимальный интервал между карьерными переходами, месяцев"
          rules={[{ required: true, message: "Укажите число месяцев" }]}
        >
          <InputNumber min={0} max={MAX_INTERVAL_MONTHS} precision={0} />
        </Form.Item>
        <Typography.Text>
          Длительность этапов новой сессии, рабочих дней
        </Typography.Text>
        <Row gutter={[16, 0]}>
          {STAGE_TITLES.map((title, index) => (
            <Col key={title} xs={24} md={12} lg={8}>
              <Form.Item
                name={["stageDurationsDays", index]}
                label={`${index + 1}. ${title}`}
                rules={[
                  { required: true, message: "Укажите число рабочих дней" },
                ]}
              >
                <InputNumber min={1} max={MAX_STAGE_DAYS} precision={0} />
              </Form.Item>
            </Col>
          ))}
        </Row>
        <Typography.Paragraph type="secondary">
          Суббота и воскресенье не считаются. Результаты публикуются в конце
          этапа 5. Приём заявок начинается не раньше чем через 7 дней после
          объявления.
        </Typography.Paragraph>
        <Flex>
          <Button type="primary" htmlType="submit" loading={isSubmitting}>
            Сохранить настройки
          </Button>
        </Flex>
      </Form>
    </Panel>
  );
}
