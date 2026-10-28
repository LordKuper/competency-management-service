import { Button, Flex, Segmented, Tooltip, Typography } from "antd";
import { scalePointLabel } from "./promotionTexts";

/** One point of the scale of a competency. */
export interface ScalePointItem {
  score: number;
  label: string;
}

/** The answer of a rater who did not see the competency at work. */
export const NOT_OBSERVED = "notObserved";

/** A score on the scale, or «Не наблюдал» where the form allows it. */
export type ScaleAnswer = number | typeof NOT_OBSERVED;

interface ScaleOption {
  value: ScaleAnswer;
  label: string;
  title?: string;
}

const NOT_OBSERVED_OPTION: ScaleOption = {
  value: NOT_OBSERVED,
  label: "Не наблюдал",
};

const TOOLTIP_WIDTH = "calc(var(--ant-control-height) * 10)";

interface ScaleHelpProps {
  /** The competency the scale belongs to. */
  name: string;
  points: readonly ScalePointItem[];
  /** The score to mark as the current one. */
  current?: number | null;
}

/** The round «?» next to a score: shows the levels of that competency's scale with the current one marked, on hover, focus and click. */
export function ScaleHelp({ name, points, current }: ScaleHelpProps) {
  return (
    <Tooltip
      trigger={["hover", "focus", "click"]}
      placement="bottomLeft"
      styles={{ container: { width: TOOLTIP_WIDTH } }}
      title={
        <Flex vertical>
          <Typography.Text strong style={{ color: "inherit" }}>
            {name}
          </Typography.Text>
          {points.map((point) => (
            <span
              key={point.score}
              style={{ fontWeight: point.score === current ? 700 : 400 }}
            >
              {point.score === current ? "● " : ""}
              {scalePointLabel(point)}
            </span>
          ))}
        </Flex>
      }
    >
      <Button
        shape="circle"
        size="small"
        aria-label={`Шкала компетенции: ${name}`}
      >
        ?
      </Button>
    </Tooltip>
  );
}

interface ScaleChoiceProps {
  /** The competency the scale belongs to. */
  name: string;
  /** The points of that competency's scale and no others. */
  points: readonly ScalePointItem[];
  /** Offers «Не наблюдал» beside the points. */
  hasNotObserved?: boolean;
  disabled?: boolean;
  /** Supplied by the surrounding form item. */
  value?: ScaleAnswer;
  onChange?: (value: ScaleAnswer) => void;
}

/** The choice of a score as a segmented control limited to the scale points of one competency, with the «?» that explains them. */
export function ScaleChoice({
  name,
  points,
  hasNotObserved = false,
  disabled,
  value,
  onChange,
}: ScaleChoiceProps) {
  const options: ScaleOption[] = [
    ...points.map((point) => ({
      value: point.score,
      label: String(point.score),
      title: scalePointLabel(point),
    })),
    ...(hasNotObserved ? [NOT_OBSERVED_OPTION] : []),
  ];
  return (
    <Flex align="center" gap="small">
      <Segmented<ScaleAnswer>
        aria-label={name}
        disabled={disabled}
        value={value}
        options={options}
        onChange={onChange}
      />
      <ScaleHelp
        name={name}
        points={points}
        current={typeof value === "number" ? value : null}
      />
    </Flex>
  );
}

interface ScaleValueProps {
  name: string;
  points: readonly ScalePointItem[];
  score: number | null;
}

/** A score already given, written «3 из 4» against the top of the scale, with the «?» that explains the levels. */
export function ScaleValue({ name, points, score }: ScaleValueProps) {
  const top = points.at(-1)?.score;
  return (
    <Flex align="center" gap="small">
      <span>
        {score === null
          ? "—"
          : top === undefined
            ? score
            : `${score} из ${top}`}
      </span>
      {points.length > 0 && (
        <ScaleHelp name={name} points={points} current={score} />
      )}
    </Flex>
  );
}
