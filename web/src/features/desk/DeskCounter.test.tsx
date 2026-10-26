import { focusManager } from "@tanstack/react-query";
import { act, render, screen, waitFor } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { DeskCounter } from "./DeskCounter";

const COUNT_ROUTE = "GET /api/v1/desk/count";
const ONE_HOUR = 3_600_000;

function answerCount(actionCount: number) {
  fakeApi.on(COUNT_ROUTE, () => Response.json({ actionCount }));
}

function renderAt(path = "/") {
  const router = createMemoryRouter(
    [
      { path: "/", element: <DeskCounter /> },
      { path: "/other", element: <DeskCounter /> },
    ],
    { initialEntries: [path] },
  );
  render(
    <Providers>
      <RouterProvider router={router} />
    </Providers>,
  );
  return router;
}

beforeEach(() => {
  queryClient.clear();
});

afterEach(() => {
  vi.useRealTimers();
});

describe("the desk counter in the menu (AC-16…AC-21)", () => {
  it("shows the number of expected actions", async () => {
    answerCount(7);

    renderAt();

    expect(await screen.findByText("7")).toBeInTheDocument();
  });

  it("shows 99+ above ninety-nine", async () => {
    answerCount(100);

    renderAt();

    expect(await screen.findByText("99+")).toBeInTheDocument();
  });

  it("shows ninety-nine as it is", async () => {
    answerCount(99);

    renderAt();

    expect(await screen.findByText("99")).toBeInTheDocument();
  });

  it("shows nothing at zero", async () => {
    let count = 3;
    fakeApi.on(COUNT_ROUTE, () => Response.json({ actionCount: count }));
    const router = renderAt();
    expect(await screen.findByText("3")).toBeInTheDocument();
    count = 0;

    await act(() => router.navigate("/other"));

    await waitFor(() =>
      expect(screen.queryByText("3")).not.toBeInTheDocument(),
    );
    expect(document.body.textContent).toBe("");
  });

  it("shows nothing before the first answer and after a failed read", async () => {
    const held = deferred<Response>();
    fakeApi.on(COUNT_ROUTE, () => held.promise);

    const { container } = render(
      <Providers>
        <RouterProvider
          router={createMemoryRouter([{ path: "/", element: <DeskCounter /> }])}
        />
      </Providers>,
    );

    await waitFor(() => expect(fakeApi.calls(COUNT_ROUTE)).toHaveLength(1));
    expect(container.textContent).toBe("");
    held.resolve(problem(403));
    await waitFor(() =>
      expect(queryClient.getQueryState(["desk", "count"])?.status).toBe(
        "error",
      ),
    );
    expect(container.textContent).toBe("");
  });

  it("hides the number when a reread fails, rather than keeping an old one", async () => {
    let isFailing = false;
    fakeApi.on(COUNT_ROUTE, () =>
      isFailing ? problem(403) : Response.json({ actionCount: 5 }),
    );
    const router = renderAt();
    expect(await screen.findByText("5")).toBeInTheDocument();
    isFailing = true;

    await act(() => router.navigate("/other"));

    await waitFor(() =>
      expect(screen.queryByText("5")).not.toBeInTheDocument(),
    );
  });

  it("rereads the count when the address changes, and not otherwise", async () => {
    let count = 3;
    fakeApi.on(COUNT_ROUTE, () => Response.json({ actionCount: count }));
    const router = renderAt();
    expect(await screen.findByText("3")).toBeInTheDocument();
    count = 5;

    await act(() => router.navigate("/other"));

    expect(await screen.findByText("5")).toBeInTheDocument();
    expect(fakeApi.calls(COUNT_ROUTE)).toHaveLength(2);
  });

  it("rereads the count after any successful change, and leaves it alone after a failed one", async () => {
    let count = 2;
    fakeApi.on(COUNT_ROUTE, () => Response.json({ actionCount: count }));
    renderAt();
    expect(await screen.findByText("2")).toBeInTheDocument();
    count = 1;

    await act(async () => {
      await queryClient
        .getMutationCache()
        .build(queryClient, { mutationFn: async () => "done" })
        .execute(undefined)
        .then(() => undefined);
    });

    expect(await screen.findByText("1")).toBeInTheDocument();
    const readsBeforeFailure = fakeApi.calls(COUNT_ROUTE).length;
    await act(async () => {
      await queryClient
        .getMutationCache()
        .build(queryClient, {
          mutationFn: async () => {
            throw new Error("refused");
          },
        })
        .execute(undefined)
        .catch(() => undefined);
    });
    expect(fakeApi.calls(COUNT_ROUTE)).toHaveLength(readsBeforeFailure);
  });

  it("rereads the count when the window gets the focus back", async () => {
    let count = 4;
    fakeApi.on(COUNT_ROUTE, () => Response.json({ actionCount: count }));
    renderAt();
    expect(await screen.findByText("4")).toBeInTheDocument();
    count = 6;

    await act(async () => {
      focusManager.setFocused(false);
      focusManager.setFocused(true);
    });

    expect(await screen.findByText("6")).toBeInTheDocument();
  });

  it("does not poll: nothing is read again while the address and the data stay", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    answerCount(2);
    renderAt();
    expect(await screen.findByText("2")).toBeInTheDocument();

    await vi.advanceTimersByTimeAsync(ONE_HOUR);

    expect(fakeApi.calls(COUNT_ROUTE)).toHaveLength(1);
  });
});
