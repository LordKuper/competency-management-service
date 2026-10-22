import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { MatrixBuilderPage } from "./MatrixBuilderPage";

const CONTENT_ROUTE = "PUT /api/v1/career/matrix-versions/v1/content";
const SUBMIT_ROUTE = "POST /api/v1/career/matrix-versions/v1/submit";
const STEP_COUNT = 6;
const VERSION = {
  id: "v1",
  matrixId: "m1",
  number: 2,
  version: 3,
  status: "Draft",
  step: null,
  builderStep: 5,
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
  actions: ["Edit", "Submit"],
};

function serve() {
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
      publishingActions: [],
      entries: [],
      hiddenEntryCount: 0,
      blockers: [],
      moreCount: 0,
      cascadeTag: "tag-builder",
    }),
  );
}

function renderBuilder() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [
            {
              path: "/desk/matrix-builder/:versionId",
              element: <MatrixBuilderPage />,
            },
            { path: "/desk", element: <p>рабочий стол</p> },
          ],
          { initialEntries: ["/desk/matrix-builder/v1?step=5"] },
        )}
      />
    </Providers>,
  );
  return userEvent.setup();
}

function lockedSteps() {
  return Array.from(document.querySelectorAll(".ant-steps-item")).filter(
    (step) => step.classList.contains("ant-steps-item-disabled"),
  );
}

async function enabledButton(name: string) {
  const button = await screen.findByRole("button", { name });
  await waitFor(() => expect(button).toBeEnabled());
  return button;
}

beforeEach(() => {
  queryClient.clear();
});

describe("the steps of the matrix wizard are held while a move to the sending is saved or the version is sent (AC-187)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("holds every step while the move from the check to the sending is being saved, and frees them once it is saved", async () => {
    serve();
    const saved = deferred<Response>();
    fakeApi.on(CONTENT_ROUTE, () => saved.promise);
    const user = renderBuilder();
    const forward = await enabledButton("Перейти к отправке");
    expect(lockedSteps()).toHaveLength(1);

    await user.click(forward);

    await waitFor(() => expect(lockedSteps()).toHaveLength(STEP_COUNT));
    saved.resolve(Response.json({ ...VERSION, version: 4, builderStep: 6 }));
    await enabledButton("Отправить на согласование");
    expect(lockedSteps()).toHaveLength(0);
  });

  it("holds every step while the version is being sent", async () => {
    serve();
    const sent = deferred<Response>();
    fakeApi.on(SUBMIT_ROUTE, () => sent.promise);
    fakeApi.on(CONTENT_ROUTE, () =>
      Response.json({ ...VERSION, version: 4, builderStep: 6 }),
    );
    const user = renderBuilder();
    await user.click(await enabledButton("Перейти к отправке"));
    const send = await enabledButton("Отправить на согласование");
    expect(lockedSteps()).toHaveLength(0);

    await user.click(send);

    await waitFor(() => expect(lockedSteps()).toHaveLength(STEP_COUNT));
    sent.resolve(Response.json({ ...VERSION, version: 5, status: "OnReview" }));
    expect(await screen.findByText("рабочий стол")).toBeInTheDocument();
  });
});
