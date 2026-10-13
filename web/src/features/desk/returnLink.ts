import { useLocation } from "react-router";

/** Where a screen reached from another scenario leads back, carried in the navigation state. */
export interface ReturnLink {
  to: string;
  /** Name of the button, such as «К построению матрицы». */
  label: string;
}

/** Navigation state that makes the screen opened by the link offer the way back to the current screen. */
export function returnState(link: ReturnLink): { from: ReturnLink } {
  return { from: link };
}

/** The way back the screen was opened with, if it was opened from another scenario. */
export function useReturnLink(): ReturnLink | undefined {
  const { state } = useLocation();
  const from = (state as { from?: Partial<ReturnLink> } | null)?.from;
  return typeof from?.to === "string" && typeof from.label === "string"
    ? { to: from.to, label: from.label }
    : undefined;
}
