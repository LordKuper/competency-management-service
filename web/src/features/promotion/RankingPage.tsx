import { useQuery } from "@tanstack/react-query";
import { Breadcrumb, Flex, Typography } from "antd";
import { Link, useParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { rankingQuery } from "./promotionApi";
import { RankingView } from "./RankingView";
import { promotionPaths } from "./routes";

/** The candidates of the commission of a unit in a wave, ranked by the rating the chair's scores earn. */
export function RankingPage() {
  const { waveId, unitId } = useParams();
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.home}>Повышение</Link> },
          { title: "Рейтинг кандидатов" },
        ]}
      />
      {waveId && unitId && <LoadedRanking waveId={waveId} unitId={unitId} />}
    </Flex>
  );
}

function LoadedRanking({ waveId, unitId }: { waveId: string; unitId: string }) {
  const { data, error, isFetching, refetch } = useQuery(
    rankingQuery(waveId, unitId),
  );
  const unitName = data?.[0]?.target.unitName;
  return (
    <>
      <Typography.Title level={1} style={{ margin: 0 }}>
        {unitName ? `Рейтинг кандидатов: ${unitName}` : "Рейтинг кандидатов"}
      </Typography.Title>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить рейтинг"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <RankingView items={data ?? []} isLoading={isFetching} />
    </>
  );
}
