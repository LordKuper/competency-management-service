import { Outlet } from "react-router";
import { useIsAdmin } from "../features/auth/useCurrentUser";
import { NotFoundPage } from "./NotFoundPage";

/**
 * Layout route for sections only global administrators use: others see the not-found page, which already says the section
 * may be unavailable to their account. The server refuses their requests regardless.
 */
export function AdminOnly() {
  return useIsAdmin() ? <Outlet /> : <NotFoundPage />;
}
