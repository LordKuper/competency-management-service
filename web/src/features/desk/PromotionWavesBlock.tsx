import { useQuery } from "@tanstack/react-query";
import { useCurrentUser } from "../auth/useCurrentUser";
import {
  myApplicationsQuery,
  openWavesQuery,
  unfinishedApplicationIdByWave,
} from "../promotion/promotionApi";
import { PromotionWavesView } from "./PromotionWavesView";

/** Reads the open waves and the viewer's applications for the desk block; the block stays out of the way while loading and when either read fails. */
export function PromotionWavesBlock() {
  const { data: user } = useCurrentUser();
  const waves = useQuery(openWavesQuery);
  const mine = useQuery(myApplicationsQuery);
  if (!waves.data || mine.isPending) {
    return null;
  }
  return (
    <PromotionWavesView
      waves={waves.data}
      applicationIdByWave={unfinishedApplicationIdByWave(mine.data ?? [])}
      canApply={user?.employeeId != null}
    />
  );
}
