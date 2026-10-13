import { useIsMutating } from "@tanstack/react-query";

/** Key shared by every change a finding of the organisation queue offers, so they cannot run side by side. */
export const ORG_FINDING_MUTATION = ["desk", "org-finding"] as const;

/** Whether some change of a finding is being saved; every action of the queue waits for it. */
export function useIsFindingBusy(): boolean {
  return useIsMutating({ mutationKey: ORG_FINDING_MUTATION }) > 0;
}
