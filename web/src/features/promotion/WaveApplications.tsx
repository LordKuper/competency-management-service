import { useQuery } from "@tanstack/react-query";
import { Flex } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { FormalDialog } from "./FormalDialog";
import {
  PROMOTION_PAGE_SIZE,
  type Wave,
  type WaveApplication,
  type WaveApplicationFilter,
  waveApplicationsQuery,
} from "./promotionApi";
import { STAGE } from "./promotionTexts";
import {
  ALL_APPLICATIONS,
  NEEDS_FORMAL_DATA,
  WaveApplicationsView,
} from "./WaveApplicationsView";

function filterOf(button: string, search: string): WaveApplicationFilter {
  return {
    status:
      button === ALL_APPLICATIONS || button === NEEDS_FORMAL_DATA
        ? undefined
        : (button as WaveApplication["status"]),
    needsFormalData: button === NEEDS_FORMAL_DATA ? true : undefined,
    search: search || undefined,
  };
}

/** The applications of a career session the signed-in user may see, narrowed by the server, with the data for the formal check of one opened over the list. */
export function WaveApplications({ wave }: { wave: Wave }) {
  const [page, setPage] = useState(1);
  const [button, setButton] = useState(ALL_APPLICATIONS);
  const [search, setSearch] = useState("");
  const [formalId, setFormalId] = useState<string>();
  const text = useDebouncedValue(search.trim());
  const { data, error, isFetching, refetch } = useQuery(
    waveApplicationsQuery(wave.id, page, filterOf(button, text)),
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
        counts={data?.counts}
        filter={button}
        search={search}
        formalCheckEndsOn={wave.endsOn[STAGE.formalCheck - 1]}
        onFilterChange={(next) => {
          setButton(next);
          setPage(1);
        }}
        onSearchChange={(next) => {
          setSearch(next);
          setPage(1);
        }}
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
