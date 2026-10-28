import { Steps } from "antd";
import { STAGE_TITLES } from "./promotionTexts";

/** The seven stages of a wave, with the one that runs now marked; `stage` is 0 before the wave starts and above seven once it is over. */
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
      }))}
    />
  );
}
