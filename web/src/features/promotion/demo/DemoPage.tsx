import { Alert, Button, Flex, Tag, Typography } from "antd";
import { type MouseEvent, useState } from "react";
import { contentMaxWidth } from "../../../app/theme";
import { stageLabel } from "../promotionTexts";
import { DEMO_STEPS } from "./scenario";

/** Links inside a demo screen lead to real screens, so a click on one stays in the demo. */
function keepInDemo(event: MouseEvent<HTMLElement>) {
  if ((event.target as Element).closest("a")) event.preventDefault();
}

/** The cycle of a wave step by step on invented data: each step names the role that acts and shows that role's screen; nothing is sent or saved. */
export function DemoPage() {
  const [index, setIndex] = useState(0);
  const step = DEMO_STEPS[index];
  if (!step) return null;

  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Typography.Title level={1} style={{ margin: 0 }}>
        Демонстрация повышения
      </Typography.Title>
      <Typography.Text type="secondary">
        Данные условные: кнопки на экранах ничего не сохраняют и не отправляют.
      </Typography.Text>
      <Flex vertical gap="small">
        <Typography.Text type="secondary">
          Шаг {index + 1} из {DEMO_STEPS.length}
          {step.stage > 0 && `, этап «${stageLabel(step.stage)}»`}
        </Typography.Text>
        <Alert
          type="info"
          showIcon
          title={
            <Flex align="center" gap="small" wrap>
              <Typography.Text strong>Действует:</Typography.Text>
              <Tag color="processing">{step.role}</Tag>
            </Flex>
          }
          description={step.description}
        />
        <Flex gap="small">
          <Button disabled={index === 0} onClick={() => setIndex(index - 1)}>
            Назад
          </Button>
          <Button
            type="primary"
            disabled={index === DEMO_STEPS.length - 1}
            onClick={() => setIndex(index + 1)}
          >
            Далее
          </Button>
        </Flex>
      </Flex>
      <Typography.Title level={2} style={{ margin: 0 }}>
        {step.title}
      </Typography.Title>
      <div key={index} onClickCapture={keepInDemo}>
        {step.render()}
      </div>
    </Flex>
  );
}
