import { theme as antdTheme, Flex, Table, Tag, Typography } from "antd";
import type { OpenWave } from "../promotion/promotionApi";
import { formatDate, STAGE } from "../promotion/promotionTexts";
import { promotionPaths } from "../promotion/routes";
import { DeskPanel } from "./DeskGroups";
import { LinkButton } from "./LinkButton";
import "./scenario.css";

interface PromotionWavesViewProps {
  waves: readonly OpenWave[];
  /** The unfinished application of the viewer by the wave it was filed in. */
  applicationIdByWave: ReadonlyMap<string, string>;
  /** Whether the viewer can file an application: an account without an employee cannot. */
  canApply: boolean;
}

interface WaveRow {
  wave: OpenWave;
  tracks: string[];
}

function rowsOf(waves: readonly OpenWave[]): WaveRow[] {
  return waves
    .map((wave) => ({
      wave,
      tracks: [
        ...new Set(
          wave.units.flatMap((unit) =>
            unit.targets.map((target) => target.trackName),
          ),
        ),
      ],
    }))
    .filter((row) => row.tracks.length > 0);
}

function PeriodCell({ wave }: { wave: OpenWave }) {
  const [deadline] = wave.endsOn;
  if (wave.stage === STAGE.submission) {
    return (
      <Flex vertical align="start">
        <Tag color="processing">Идёт приём заявок</Tag>
        {deadline && <span>до {formatDate(deadline)}</span>}
      </Flex>
    );
  }
  return (
    <Flex vertical align="start">
      <Tag>Скоро начнётся</Tag>
      <span>приём заявок с {formatDate(wave.startsOn)}</span>
    </Flex>
  );
}

/** The desk block of the waves one can apply to or that are announced: tracks, the deadline or the start of applications, and the way to apply. Nothing is drawn when no wave offers a track. */
export function PromotionWavesView({
  waves,
  applicationIdByWave,
  canApply,
}: PromotionWavesViewProps) {
  const { token } = antdTheme.useToken();
  const rows = rowsOf(waves);
  if (rows.length === 0) {
    return null;
  }
  return (
    <Flex vertical gap={token.margin}>
      <Typography.Title level={2} style={{ margin: 0 }}>
        Волны повышения
      </Typography.Title>
      <DeskPanel>
        <Table<WaveRow>
          rowKey={(row) => row.wave.id}
          dataSource={rows}
          pagination={false}
          tableLayout="fixed"
          className="desk-table"
          columns={[
            {
              key: "period",
              title: "Приём заявок",
              render: (_, row) => <PeriodCell wave={row.wave} />,
            },
            {
              key: "tracks",
              title: "Треки",
              render: (_, row) => row.tracks.join(", "),
            },
            {
              key: "action",
              title: "Действия",
              className: "desk-nowrap",
              render: (_, { wave }) => {
                if (wave.stage !== STAGE.submission || !canApply) {
                  return null;
                }
                const applicationId = applicationIdByWave.get(wave.id);
                return applicationId ? (
                  <LinkButton to={promotionPaths.application(applicationId)}>
                    Моя заявка
                  </LinkButton>
                ) : (
                  <LinkButton type="primary" to={promotionPaths.apply(wave.id)}>
                    Подать заявку
                  </LinkButton>
                );
              },
            },
          ]}
        />
      </DeskPanel>
    </Flex>
  );
}
