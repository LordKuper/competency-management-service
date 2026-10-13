import { useLocation } from "react-router";

/** The scenario a screen was reached from, carried in the navigation state; the breadcrumb leads back to it. */
export interface ReturnLink {
  to: string;
  /** Name of the scenario the user came from, shown as the middle item of the breadcrumb. */
  crumb: string;
}

/** Navigation state that makes the screen opened by the link show the current screen in its breadcrumb. */
export function returnState(link: ReturnLink): { from: ReturnLink } {
  return { from: link };
}

/** The scenario the screen was opened from, if it was opened from another scenario. */
export function useReturnLink(): ReturnLink | undefined {
  const { state } = useLocation();
  const from = (state as { from?: Partial<ReturnLink> } | null)?.from;
  return typeof from?.to === "string" && typeof from.crumb === "string"
    ? { to: from.to, crumb: from.crumb }
    : undefined;
}
