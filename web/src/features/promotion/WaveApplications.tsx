import { useQuery } from "@tanstack/react-query";
import { Flex } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { FormalDialog } from "./FormalDialog";
import {
  PROMOTION_PAGE_SIZE,
  type Wave,
  waveApplicationsQuery,
} from "./promotionApi";
import { STAGE } from "./promotionTexts";
import { WaveApplicationsView } from "./WaveApplicationsView";

/** The applications of a career session the signed-in user may see, with the data for the formal check of one opened over the list. */
export function WaveApplications({ wave }: { wave: Wave }) {
  const [page, setPage] = useState(1);
  const [formalId, setFormalId] = useState<string>();
  const { data, error, isFetching, refetch } = useQuery(
    waveApplicationsQuery(wave.id, page),
  );
  const items = data?.items ?? [];
  const formalIndex = items.findIndex(
    (application) => application.id === formalId,
  );
  const formal = items[formalIndex];
  const next = formalIndex >= 0 ? items[formalIndex + 1] : undefined;
  return (
    <Flex vertical gap="middle">
      {error && (
        <ErrorAlert
          title="Не удалось загрузить заявки"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <WaveApplicationsView
        applications={items}
        total={data?.total ?? 0}
        page={page}
        pageSize={PROMOTION_PAGE_SIZE}
        isLoading={isFetching}
        formalCheckEndsOn={wave.endsOn[STAGE.formalCheck - 1]}
        onPageChange={setPage}
        onOpenFormal={setFormalId}
      />
      {formal && (
        <FormalDialog
          key={formal.id}
          application={formal}
          wave={wave}
          onNext={next && (() => setFormalId(next.id))}
          onClose={() => setFormalId(undefined)}
        />
      )}
    </Flex>
  );
}
