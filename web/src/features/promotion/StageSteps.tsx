import { FlagOutlined } from "@ant-design/icons";
import { Space, Steps, Typography } from "antd";
import {
  PUBLICATION_NOTE,
  PUBLISHED_NOTE,
  STAGE,
  STAGE_TITLES,
} from "./promotionTexts";

function PublicationMilestone({ isPublished }: { isPublished: boolean }) {
  return (
    <Space size="small">
      <FlagOutlined aria-hidden />
      <Typography.Text type="secondary">
        {isPublished ? PUBLISHED_NOTE : PUBLICATION_NOTE}
      </Typography.Text>
    </Space>
  );
}

/** The six stages of a wave, with the one that runs now marked and the publication of the results, which is no stage, noted at the end of the review; `stage` is 0 before the wave starts and above six once it is over. */
export function StageSteps({ stage }: { stage: number }) {
  return (
    <Steps
      orientation="vertical"
      size="small"
      items={STAGE_TITLES.map((title, index) => ({
        title,
        status:
          stage > index + 1
            ? "finish"
            : stage === index + 1
              ? "process"
              : "wait",
        content:
          index + 1 === STAGE.review ? (
            <PublicationMilestone isPublished={stage > STAGE.review} />
          ) : undefined,
      }))}
    />
  );
}
