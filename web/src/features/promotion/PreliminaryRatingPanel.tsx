import { Flex, Tag, Typography } from "antd";
import { Panel } from "./Panel";
import type { PreliminaryRating } from "./promotionApi";
import { formatPercent, unscoredText } from "./promotionTexts";
import { ThresholdBar } from "./ThresholdBar";

/** The rating the saved self-assessment earns, as the server counts it, the threshold of the level and the share of the requirements the candidate meets; nothing is counted here. */
export function PreliminaryRatingPanel({
  rating,
}: {
  rating: PreliminaryRating | null;
}) {
  return (
    <Panel title="Предварительный рейтинг" tour="preliminary-rating" level={3}>
      {rating ? (
        <Flex vertical gap="small">
          <Flex align="baseline" gap="small" wrap>
            <Typography.Title level={1} style={{ margin: 0 }}>
              {formatPercent(rating.rating)}
            </Typography.Title>
            {rating.threshold !== null && (
              <Typography.Text type="secondary">
                нужно не ниже {formatPercent(rating.threshold)}
              </Typography.Text>
            )}
          </Flex>
          <ThresholdBar
            value={rating.rating}
            threshold={rating.threshold}
            label={
              rating.threshold === null
                ? formatPercent(rating.rating)
                : `${formatPercent(rating.rating)}, порог ${formatPercent(rating.threshold)}`
            }
          />
          <Flex vertical align="start" gap="small">
            {rating.unscoredCount > 0 && (
              <Tag color="warning">{unscoredText(rating.unscoredCount)}</Tag>
            )}
            <Tag color={rating.areCriticalMet ? "success" : "warning"}>
              {rating.areCriticalMet
                ? "Требования к критическим компетенциям выполнены"
                : "Требования к критическим компетенциям не выполнены"}
            </Tag>
          </Flex>
          <Typography.Text type="secondary">
            Рейтинг — доля от наибольшего возможного балла по компетенциям
            уровня с учётом веса блоков. Для перехода нужно набрать порог и по
            критическим компетенциям — не меньше требуемого балла.
          </Typography.Text>
          <Typography.Text type="secondary">
            Посчитан по сохранённой самооценке и обновляется при сохранении
            черновика. Итоговый рейтинг определят круговая оценка и комиссия.
          </Typography.Text>
        </Flex>
      ) : (
        <Typography.Text type="secondary">
          Рейтинг по самооценке появится после первого сохранения черновика.
        </Typography.Text>
      )}
    </Panel>
  );
}
