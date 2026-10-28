import { useQuery } from "@tanstack/react-query";
import { Flex, Typography } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { LinkButton } from "../desk/LinkButton";
import { PROMOTION_PAGE_SIZE, wavesQuery } from "./promotionApi";
import { promotionPaths } from "./routes";
import { WaveListView } from "./WaveListView";

/** The waves the signed-in HR or administrator manages, and the way to announce another one. */
export function WavesPage() {
  const [page, setPage] = useState(1);
  const { data, error, isFetching, refetch } = useQuery(wavesQuery(page));
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Волны повышения
        </Typography.Title>
        <LinkButton type="primary" to={promotionPaths.newWave}>
          Объявить волну
        </LinkButton>
      </Flex>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить волны"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <WaveListView
        waves={data?.items ?? []}
        total={data?.total ?? 0}
        page={page}
        pageSize={PROMOTION_PAGE_SIZE}
        isLoading={isFetching}
        onPageChange={setPage}
      />
    </Flex>
  );
}
