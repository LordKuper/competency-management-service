/** Handler of one fake route; it receives the request exactly as the typed client sent it. */
type Handler = (request: Request) => Response | Promise<Response>;

const NO_HANDLER_STATUS = 599;

const handlers = new Map<string, Handler>();
const received: Request[] = [];

function routeOf(request: Request): string {
  return `${request.method} ${new URL(request.url).pathname}`;
}

/**
 * The network of the web tests. It replaces `fetch` before the typed client is created, because the client takes
 * `fetch` once, when its module loads; this module is therefore imported by the test setup file.
 * A request no handler claims is answered 599, so the screen under test reports it as a failed call.
 */
globalThis.fetch = async (input, init) => {
  const request = input instanceof Request ? input : new Request(input, init);
  received.push(request.clone());
  const handler = handlers.get(routeOf(request));
  return (
    handler?.(request) ??
    Response.json(
      { title: `No fake handler for ${routeOf(request)}` },
      { status: NO_HANDLER_STATUS },
    )
  );
};

export const fakeApi = {
  /** Answers every request of the route, written `"METHOD /path"` without the query, until the next test. */
  on(route: string, handler: Handler): void {
    handlers.set(route, handler);
  },
  /** The requests received on the route so far, in the order they arrived; their bodies are unread. */
  calls(route: string): Request[] {
    return received.filter((request) => routeOf(request) === route);
  },
  reset(): void {
    handlers.clear();
    received.length = 0;
  },
};

/** A failed answer with a problem-details body. */
export function problem(
  status: number,
  fields: Record<string, unknown> = {},
): Response {
  return Response.json({ status, ...fields }, { status });
}

/** A promise settled from outside, to hold an answer until the test releases it. */
export function deferred<TValue>() {
  let resolve: (value: TValue) => void = () => undefined;
  const promise = new Promise<TValue>((settle) => {
    resolve = settle;
  });
  return { promise, resolve };
}
