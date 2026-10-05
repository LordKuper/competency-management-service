import { createBrowserRouter, type RouteObject } from "react-router";
import { setUnauthorizedHandler } from "../api/client";
import { AppShell } from "./AppShell";
import { NotFoundPage } from "./NotFoundPage";
import { RouteErrorPage } from "./RouteErrorPage";

const LOGIN_PATH = "/login";

interface FeatureRoutes {
  routes?: RouteObject[];
  standaloneRoutes?: RouteObject[];
}

const features = Object.values(
  import.meta.glob<FeatureRoutes>("../features/*/routes.ts", { eager: true }),
);

/**
 * Application router. A feature opts in by adding `features/<name>/routes.ts` exporting `routes`
 * (rendered inside the shell) and/or `standaloneRoutes` (rendered without it, e.g. the sign-in
 * screen, which must live at `/login`); no shared file is edited to register a feature.
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
  if (router.state.location.pathname !== LOGIN_PATH) {
    void router.navigate(LOGIN_PATH, { replace: true });
  }
});
