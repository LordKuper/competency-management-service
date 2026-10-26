import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { ReworkPage } from "./ReworkPage";

const VERSION_ROUTE = "GET /api/v1/career/matrix-versions/v1";
const HISTORY_ROUTE = "GET /api/v1/career/matrix-versions/v1/history";
const CHECK_ROUTE = "GET /api/v1/career/matrix-versions/v1/completeness";
const LIST_ROUTE = "GET /api/v1/career/matrix-versions/v1/publication";
const HISTORY_TITLE = "Не удалось загрузить комментарий и правки согласующего";
const HISTORY_REASON = "Недоступно: не загрузилась история версии.";
const ACCEPT_REASON =
  "Недоступно: не загрузились история версии, проверка полноты или перечень публикации.";
const VERSION = {
  id: "v1",
  matrixId: "m1",
  number: 2,
  version: 3,
  status: "Returned",
  step: null,
  authorId: "author",
  authorName: "Иванов Иван Иванович",
  createdAt: "2026-10-01T10:00:00Z",
  publishedAt: null,
  content: {
    levels: [],
    competencies: [],
    sharedBlocks: [],
    blockWeights: [],
    requirements: [],
  },
  entries: {
    direction: null,
    track: null,
    levels: [],
    competencies: [],
    position: null,
    sharedBlocks: [],
    scales: [],
  },
  superiorApprover: null,
  hr: null,
  returnedWithChanges: true,
  actions: ["AcceptChanges", "AcceptAndRevise", "RollbackChanges"],
};
const HISTORY = {
  variants: [],
  decisions: [
    {
      sequence: 1,
      kind: "Returned",
      step: "Superior",
      actorId: null,
      actorName: "Петров Пётр Петрович",
      at: "2026-10-02T10:00:00Z",
      comment: "Нужны правки",
      variantNumber: null,
    },
  ],
};
const FAILED_READS = [
  {
    read: "the completeness check",
    route: CHECK_ROUTE,
    title: "Не удалось выполнить проверку полноты",
  },
  {
    read: "the list it publishes",
    route: LIST_ROUTE,
    title: "Не удалось загрузить перечень публикации",
  },
];

function serve() {
  fakeApi.on(VERSION_ROUTE, () => Response.json(VERSION));
  fakeApi.on("GET /api/v1/career/matrices/m1", () =>
    Response.json({ id: "m1", orgUnitName: "Команда", publishedVersion: null }),
  );
  fakeApi.on(HISTORY_ROUTE, () => Response.json(HISTORY));
  fakeApi.on(CHECK_ROUTE, () => Response.json({ problems: [] }));
  fakeApi.on(LIST_ROUTE, () =>
    Response.json({
      publishingActions: [],
      entries: [],
      hiddenEntryCount: 0,
      blockers: [],
      moreCount: 0,
      cascadeTag: "tag-rework",
    }),
  );
}

function renderRework() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [{ path: "/desk/rework/:versionId", element: <ReworkPage /> }],
          { initialEntries: ["/desk/rework/v1"] },
        )}
      />
    </Providers>,
  );
  return userEvent.setup();
}

beforeEach(() => {
  queryClient.clear();
});

describe("the rework of a returned version whose history, check or list could not be read (AC-56)", () => {
  it("shows the failure of the history with a retry, holds every decision off with the reason, and frees the two that need no check once it is read", async () => {
    serve();
    fakeApi.on(HISTORY_ROUTE, () => problem(403, { title: "Нет доступа" }));
    const user = renderRework();

    expect(await screen.findByText(HISTORY_TITLE)).toBeInTheDocument();

    expect(
      screen.getByRole("button", { name: "Принять правки" }),
    ).toBeDisabled();
    expect(
      screen.getByRole("button", { name: "Принять и доработать" }),
    ).toBeDisabled();
    expect(
      screen.getByRole("button", { name: "Откатить правки" }),
    ).toBeDisabled();
    expect(screen.getByText(ACCEPT_REASON)).toBeInTheDocument();
    expect(screen.getAllByText(HISTORY_REASON)).toHaveLength(2);
    const retry = screen.getByRole("button", { name: "Повторить" });
    serve();
    await user.click(retry);
    expect(await screen.findByText("Нужны правки")).toBeInTheDocument();
    expect(screen.queryByText(HISTORY_TITLE)).toBeNull();
    expect(screen.queryByText(HISTORY_REASON)).toBeNull();
    expect(screen.queryByText(ACCEPT_REASON)).toBeNull();
    expect(
      screen.getByRole("button", { name: "Принять и доработать" }),
    ).toBeEnabled();
    expect(
      screen.getByRole("button", { name: "Откатить правки" }),
    ).toBeEnabled();
  });

  it.each(FAILED_READS)(
    "shows the failure of $read with a retry and holds the acceptance of the changes off with the reason",
    async ({ route, title }) => {
      serve();
      fakeApi.on(route, () => problem(403, { title: "Нет доступа" }));
      const user = renderRework();

      expect(await screen.findByText(title)).toBeInTheDocument();

      expect(
        screen.getByRole("button", { name: "Принять правки" }),
      ).toBeDisabled();
      expect(screen.getByText(ACCEPT_REASON)).toBeInTheDocument();
      const retry = screen.getByRole("button", { name: "Повторить" });
      serve();
      await user.click(retry);
      await waitFor(() => expect(screen.queryByText(title)).toBeNull());
      expect(screen.queryByText(ACCEPT_REASON)).toBeNull();
    },
  );
});
