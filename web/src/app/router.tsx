import { createBrowserRouter } from "react-router";
import { setUnauthorizedHandler } from "../api/client";
import { queryClient } from "../api/queryClient";
import { AppShell } from "./AppShell";
import { LOGIN_PATH } from "./featureContract";
import { features } from "./features";
import { LandingPage } from "./LandingPage";
import { NotFoundPage } from "./NotFoundPage";
import { RouteErrorPage } from "./RouteErrorPage";

/**
 * Application router. A feature opts in by adding `features/<name>/routes.ts` exporting `routes`
 * (rendered inside the shell), `standaloneRoutes` (rendered without it, e.g. the sign-in
 * screen, which must live at `/login`) and/or `navItems` (shell navigation entries);
 * no shared file is edited to register a feature.
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
              { index: true, Component: LandingPage },
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
  if (router.state.location.pathname === LOGIN_PATH) return;
  queryClient.clear();
  void router.navigate(LOGIN_PATH, { replace: true });
});
