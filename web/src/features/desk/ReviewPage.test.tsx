import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { ReviewPage } from "./ReviewPage";

const NO_DRAFTS = "Черновых записей нет: публикуется только версия.";
const VERSION = {
  id: "v1",
  matrixId: "m1",
  number: 2,
  version: 3,
  status: "OnReview",
  step: "Superior",
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
  returnedWithChanges: false,
  actions: ["Approve", "Return"],
};

function serve(hiddenEntryCount: number) {
  fakeApi.on("GET /api/v1/career/matrix-versions/v1", () =>
    Response.json(VERSION),
  );
  fakeApi.on("GET /api/v1/career/matrices/m1", () =>
    Response.json({ id: "m1", orgUnitName: "Команда", publishedVersion: null }),
  );
  fakeApi.on("GET /api/v1/career/matrix-versions/v1/completeness", () =>
    Response.json({ problems: [] }),
  );
  fakeApi.on("GET /api/v1/career/matrix-versions/v1/publication", () =>
    Response.json({
      publishingActions: ["Approve"],
      entries: [],
      hiddenEntryCount,
      blockers: [],
      moreCount: 0,
      cascadeTag: "tag-review",
    }),
  );
}

function renderReview() {
  const user = userEvent.setup();
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [{ path: "/desk/review/:versionId", element: <ReviewPage /> }],
          { initialEntries: ["/desk/review/v1"] },
        )}
      />
    </Providers>,
  );
  return user;
}

beforeEach(() => {
  queryClient.clear();
});

describe("the review of a version lists what its approval publishes (AC-65, AC-110)", () => {
  it("counts the drafts the approver may not read, and does not say the version goes alone", async () => {
    serve(2);
    renderReview();

    expect(
      await screen.findByText("и ещё 2 черновые записи"),
    ).toBeInTheDocument();

    expect(screen.queryByText(NO_DRAFTS)).toBeNull();
  });

  it("says the version goes alone when no draft goes with it", async () => {
    serve(0);
    renderReview();

    expect(await screen.findByText(NO_DRAFTS)).toBeInTheDocument();

    expect(screen.queryByText(/и ещё/)).toBeNull();
  });
});

const CHECK_ROUTE = "GET /api/v1/career/matrix-versions/v1/completeness";
const LIST_ROUTE = "GET /api/v1/career/matrix-versions/v1/publication";
const APPROVE_BUTTON = { name: /^Согласовать( и опубликовать)?$/ };
const FAILED_READS = [
  {
    read: "the completeness check",
    route: CHECK_ROUTE,
    title: "Не удалось выполнить проверку полноты",
  },
  {
    read: "the list of what the approval publishes",
    route: LIST_ROUTE,
    title: "Не удалось загрузить перечень публикации",
  },
];

describe("the review of a version whose preparation could not be read (AC-65)", () => {
  it.each(FAILED_READS)(
    "shows the failure of $read with a retry, holds the approval off with the reason, and approves once a retry reads it",
    async ({ route, title }) => {
      serve(0);
      fakeApi.on(route, () => problem(403, { title: "Нет доступа" }));
      const user = renderReview();

      expect(await screen.findByText(title)).toBeInTheDocument();

      expect(screen.getByRole("button", APPROVE_BUTTON)).toBeDisabled();
      expect(
        screen.getByText(
          "Согласовать недоступно: не загрузились проверка полноты или перечень публикации",
        ),
      ).toBeInTheDocument();
      const retry = screen.getByRole("button", { name: "Повторить" });
      serve(0);
      await user.click(retry);
      expect(await screen.findByText(NO_DRAFTS)).toBeInTheDocument();
      expect(screen.queryByText(title)).toBeNull();
      expect(screen.getByRole("button", APPROVE_BUTTON)).toBeEnabled();
    },
  );

  it("holds the approval off and says why while the list of what it publishes is being read", async () => {
    serve(0);
    const list = deferred<Response>();
    fakeApi.on(LIST_ROUTE, () => list.promise);
    renderReview();

    expect(
      await screen.findByText("Проверяем, что будет опубликовано…"),
    ).toBeInTheDocument();

    expect(screen.getByRole("button", APPROVE_BUTTON)).toBeDisabled();
  });
});
