import { Steps, Tag } from "antd";

interface StepRulerProps {
  /** Names of the steps, in order. */
  steps: readonly string[];
  /** The step shown, counted from 1. */
  current: number;
  /** The furthest step opened by a click; every step up to it is passed. */
  reached: number;
  /** The steps are off while a save is in flight. */
  isLocked?: boolean;
  /** Number of remarks of the completeness check by step. */
  remarks?: ReadonlyMap<number, number>;
  onChange: (step: number) => void;
}

/** The steps of a wizard in one row: passed ones open on a click, the rest are only shown. */
export function StepRuler({
  steps,
  current,
  reached,
  isLocked = false,
  remarks,
  onChange,
}: StepRulerProps) {
  return (
    <Steps
      size="small"
      current={current - 1}
      onChange={(index) => {
        if (index + 1 !== current) onChange(index + 1);
      }}
      items={steps.map((name, index) => {
        const number = index + 1;
        const count = remarks?.get(number);
        return {
          title: count ? (
            <>
              {name} <Tag color="warning">{`⚠ Замечания: ${count}`}</Tag>
            </>
          ) : (
            name
          ),
          disabled: number > reached || isLocked,
          status:
            number === current
              ? "process"
              : number <= reached
                ? "finish"
                : "wait",
        };
      })}
    />
  );
}
