import { FlagOutlined } from "@ant-design/icons";
import { Flex, Space, Steps, Typography } from "antd";
import {
  formatShortDate,
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

interface StageStepsProps {
  /** 0 before the session starts and above six once it is over. */
  stage: number;
  /** End dates of the six stages; the steps show them when given. */
  endsOn?: readonly string[];
  orientation?: "horizontal" | "vertical";
}

/** The six stages of a career session, with the one that runs now marked, the end date of each when known, and the publication of the results, which is no stage, noted at the end of the review. */
export function StageSteps({
  stage,
  endsOn,
  orientation = "vertical",
}: StageStepsProps) {
  return (
    <Steps
      orientation={orientation}
      size="small"
      items={STAGE_TITLES.map((title, index) => {
        const endsOnStage = endsOn?.[index];
        const hasPublication = index + 1 === STAGE.review;
        return {
          title,
          status:
            stage > index + 1
              ? "finish"
              : stage === index + 1
                ? "process"
                : "wait",
          content:
            endsOnStage || hasPublication ? (
              <Flex vertical>
                {endsOnStage && (
                  <Typography.Text type="secondary">
                    {stage === index + 1 ? "сейчас, " : ""}до{" "}
                    {formatShortDate(endsOnStage)}
                  </Typography.Text>
                )}
                {hasPublication && (
                  <PublicationMilestone isPublished={stage > STAGE.review} />
                )}
              </Flex>
            ) : undefined,
        };
      })}
    />
  );
}
