import { Outlet } from "react-router";
import { useIsAdmin, useIsHr } from "../features/auth/useCurrentUser";
import { NotFoundPage } from "./NotFoundPage";

/**
 * Layout route for sections only global administrators use: others see the not-found page, which already says the section
 * may be unavailable to their account. The server refuses their requests regardless.
 */
export function AdminOnly() {
  return useIsAdmin() ? <Outlet /> : <NotFoundPage />;
}

/** Layout route for sections global administrators and HR use; others see the not-found page. */
export function AdminOrHrOnly() {
  const isAdmin = useIsAdmin();
  const isHr = useIsHr();
  const hasAccess = isAdmin || isHr;
  return hasAccess ? <Outlet /> : <NotFoundPage />;
}
