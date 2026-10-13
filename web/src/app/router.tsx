import { createBrowserRouter, Navigate } from "react-router";
import { setUnauthorizedHandler } from "../api/client";
import { queryClient } from "../api/queryClient";
import { loginPathReturningTo } from "../features/auth/returnTo";
import { AppShell } from "./AppShell";
import { HOME_PATH, LOGIN_PATH } from "./featureContract";
import { features } from "./features";
import { NotFoundPage } from "./NotFoundPage";
import { RouteErrorPage } from "./RouteErrorPage";

/**
 * Application router. A feature opts in by adding `features/<name>/routes.ts` exporting `routes`
 * (rendered inside the shell), `standaloneRoutes` (rendered without it, e.g. the sign-in
 * screen, which must live at `/login`) and/or `navItems` (shell navigation entries);
 * no shared file is edited to register a feature. The root opens the home page without leaving itself in the history.
 */
export const router = createBrowserRouter([
  {
    ErrorBoundary: RouteErrorPage,
    children: [
      ...features.flatMap((feature) => feature.standaloneRoutes ?? []),
      {
        Component: AppShell,
        children: [
          {
            ErrorBoundary: RouteErrorPage,
            children: [
              { index: true, element: <Navigate to={HOME_PATH} replace /> },
              ...features.flatMap((feature) => feature.routes ?? []),
              { path: "*", Component: NotFoundPage },
            ],
          },
        ],
      },
    ],
  },
]);

setUnauthorizedHandler(() => {
  const { pathname, search } = router.state.location;
  if (pathname === LOGIN_PATH) return;
  queryClient.clear();
  void router.navigate(loginPathReturningTo(pathname + search), {
    replace: true,
  });
});
