import { render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
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
