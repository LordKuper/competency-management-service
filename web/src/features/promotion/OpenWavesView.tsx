import { Button, Card, Descriptions, Empty, Flex, Tag, Typography } from "antd";
import { Link, useNavigate } from "react-router";
import type { OpenWave } from "./promotionApi";
import {
  formatDate,
  PUBLICATION_TITLE,
  STAGE,
  STAGE_TITLES,
  stageLabel,
} from "./promotionTexts";
import { promotionPaths } from "./routes";

interface OpenWavesViewProps {
  waves: readonly OpenWave[];
  /** The unfinished application of the viewer by the wave it was filed in. */
  applicationIdByWave: ReadonlyMap<string, string>;
  /** Whether the viewer can file an application: an account without an employee cannot. */
  canApply: boolean;
}

function stageDates(wave: OpenWave) {
  return STAGE_TITLES.flatMap((title, index) => {
    const endsOn = wave.endsOn[index];
    if (!endsOn) return [];
    const stageRow = {
      key: title,
      label: title,
      children:
        index === 0
          ? `${formatDate(wave.startsOn)} — ${formatDate(endsOn)}`
          : `до ${formatDate(endsOn)}`,
    };
    return index + 1 === STAGE.review
      ? [
          stageRow,
          {
            key: PUBLICATION_TITLE,
            label: PUBLICATION_TITLE,
            children: formatDate(endsOn),
          },
        ]
      : [stageRow];
  });
}

/** The waves that are announced or take applications: the stages, and the units with the tracks and levels one can apply for. */
export function OpenWavesView({
  waves,
  applicationIdByWave,
  canApply,
}: OpenWavesViewProps) {
  const navigate = useNavigate();
  if (waves.length === 0) {
    return (
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description="Сейчас нет волн повышения, в которые можно подать заявку. Когда HR объявит волну, она появится здесь."
      />
    );
  }
  return (
    <Flex vertical gap="middle">
      {waves.map((wave) => {
        const applicationId = applicationIdByWave.get(wave.id);
        const isOpen = wave.stage === STAGE.submission;
        return (
          <Card
            key={wave.id}
            title={
              <Flex align="center" gap="small" wrap>
                <span>Волна с {formatDate(wave.startsOn)}</span>
                <Tag color={isOpen ? "processing" : "default"}>
                  {stageLabel(wave.stage)}
                </Tag>
              </Flex>
            }
            extra={
              applicationId ? (
                <Link to={promotionPaths.application(applicationId)}>
                  Моя заявка
                </Link>
              ) : (
                isOpen &&
                canApply && (
                  <Button
                    type="primary"
                    onClick={() => void navigate(promotionPaths.apply(wave.id))}
                  >
                    Подать заявку
                  </Button>
                )
              )
            }
          >
            <Flex vertical gap="middle">
              <Descriptions
                size="small"
                column={{ xs: 1, md: 2 }}
                items={stageDates(wave)}
              />
              {wave.units.map((unit) => (
                <Flex key={unit.id} vertical>
                  <Typography.Text strong>{unit.name}</Typography.Text>
                  {unit.targets.length === 0 && (
                    <Typography.Text type="secondary">
                      Матриц, на которые можно подать заявку, пока нет.
                    </Typography.Text>
                  )}
                  {unit.targets.map((target) => (
                    <Typography.Text key={target.matrixId} type="secondary">
                      {target.trackName}:{" "}
                      {target.levels.map((level) => level.name).join(", ")}
                    </Typography.Text>
                  ))}
                </Flex>
              ))}
            </Flex>
          </Card>
        );
      })}
    </Flex>
  );
}
