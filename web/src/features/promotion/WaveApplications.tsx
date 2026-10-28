import { useQuery } from "@tanstack/react-query";
import { Flex, Typography } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { FormalDialog } from "./FormalDialog";
import { PROMOTION_PAGE_SIZE, waveApplicationsQuery } from "./promotionApi";
import { WaveApplicationsView } from "./WaveApplicationsView";

/** The applications of a wave the signed-in user may see, with the formal check of one opened over the list. */
export function WaveApplications({ waveId }: { waveId: string }) {
  const [page, setPage] = useState(1);
  const [formalId, setFormalId] = useState<string>();
  const { data, error, isFetching, refetch } = useQuery(
    waveApplicationsQuery(waveId, page),
  );
  return (
    <Flex vertical gap="middle">
      <Typography.Title level={2} style={{ margin: 0 }}>
        Заявки
      </Typography.Title>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить заявки"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <WaveApplicationsView
        applications={data?.items ?? []}
        total={data?.total ?? 0}
        page={page}
        pageSize={PROMOTION_PAGE_SIZE}
        isLoading={isFetching}
        onPageChange={setPage}
        onOpenFormal={setFormalId}
      />
      {formalId && (
        <FormalDialog
          applicationId={formalId}
          onClose={() => setFormalId(undefined)}
        />
      )}
    </Flex>
  );
}
