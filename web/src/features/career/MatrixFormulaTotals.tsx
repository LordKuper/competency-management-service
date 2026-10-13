import { CloseCircleOutlined } from "@ant-design/icons";
import { InputNumber, Space, Typography } from "antd";
import type { MatrixContent } from "./matrixApi";
import { setOverallMin } from "./matrixContent";

const MAX_PERCENT = 100;
const PERCENT_PRECISION = 100;

interface MatrixFormulaTotalsProps {
  content: MatrixContent;
  levelIds: readonly string[];
  levelNames: ReadonlyMap<string, string>;
  isEditable: boolean;
  onChange: (content: MatrixContent) => void;
}

/** The sum of the block coefficients of a level, which must be 100. */
function coefficientSum(content: MatrixContent, levelId: string) {
  const sum = content.blockWeights
    .filter((weight) => weight.levelId === levelId)
    .reduce((total, weight) => total + weight.percent, 0);
  return Math.round(sum * PERCENT_PRECISION) / PERCENT_PRECISION;
}

/**
 * Under the blocks: the check of each level's coefficient sum, in words as well as colour, and the minimum overall
 * threshold of each level.
 */
export function MatrixFormulaTotals({
  content,
  levelIds,
  levelNames,
  isEditable,
  onChange,
}: MatrixFormulaTotalsProps) {
  const wrong = levelIds
    .map((levelId) => ({ levelId, sum: coefficientSum(content, levelId) }))
    .filter(({ sum }) => sum !== MAX_PERCENT);
  return (
    <Space orientation="vertical" size="middle" style={{ display: "flex" }}>
      {wrong.map(({ levelId, sum }) => (
        <Typography.Text key={levelId} type="danger">
          <CloseCircleOutlined />{" "}
          {`Коэффициенты блоков уровня «${levelNames.get(levelId)}» дают в сумме ${sum} %, нужно ${MAX_PERCENT} %.`}
        </Typography.Text>
      ))}
      <Typography.Title level={2}>Минимальный общий порог, %</Typography.Title>
      <Space size="middle" wrap>
        {levelIds.map((levelId) => {
          const level = content.levels.find((item) => item.levelId === levelId);
          return (
            <Space key={levelId} orientation="vertical" size={0}>
              <label htmlFor={`overall-min-${levelId}`}>
                {levelNames.get(levelId)}
              </label>
              <InputNumber
                id={`overall-min-${levelId}`}
                min={0}
                max={MAX_PERCENT}
                disabled={!isEditable || undefined}
                value={level?.minOverallPercent ?? null}
                onChange={(value) =>
                  onChange(setOverallMin(content, levelId, value))
                }
              />
            </Space>
          );
        })}
      </Space>
    </Space>
  );
}
