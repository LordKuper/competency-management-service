import createClient, { type Middleware } from "openapi-fetch";
import type { paths } from "./schema";

let handleUnauthorized: () => void = () => undefined;

/** Registers the single reaction to a missing or expired session; the router installs it at startup. */
export function setUnauthorizedHandler(handler: () => void): void {
  handleUnauthorized = handler;
}

const notifyOnUnauthorized: Middleware = {
  onResponse({ response }) {
    if (response.status === 401) handleUnauthorized();
  },
};

/** Typed same-origin client; the session cookie travels without extra options. */
export const api = createClient<paths>({ baseUrl: window.location.origin });
api.use(notifyOnUnauthorized);
