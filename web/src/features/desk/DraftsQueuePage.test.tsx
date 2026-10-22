import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { DraftsQueuePage } from "./DraftsQueuePage";

const PREVIEW_ROUTE = "GET /api/v1/career/competencies/c1/publication";
const APPROVE_ROUTE = "POST /api/v1/career/competencies/c1/approve";
const NO_OTHER_DRAFTS = "Других черновых записей публикация не затронет.";
const DRAFT = {
  kind: "Competency",
  id: "c1",
  name: "Работа в команде",
  ownerOrgUnitId: null,
  ownerOrgUnitName: null,
  authorId: "author",
  authorName: "Иванов Иван Иванович",
  createdAt: "2026-10-01T10:00:00Z",
  version: 4,
};

function serve(hiddenEntryCount: number) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "hr", email: "hr@test.local", roles: ["Hr"] }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/career/drafts", () => Response.json([DRAFT]));
  fakeApi.on("GET /api/v1/career/competencies/c1/matrices", () =>
    Response.json([]),
  );
  fakeApi.on(PREVIEW_ROUTE, () =>
    Response.json({
      entries: [],
      hiddenEntryCount,
      blockers: [],
      moreCount: 0,
      cascadeTag: "tag-queue",
    }),
  );
  fakeApi.on(APPROVE_ROUTE, () => Response.json({ id: "c1" }));
}

function renderQueue() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [{ path: "/desk/drafts", element: <DraftsQueuePage /> }],
          { initialEntries: ["/desk/drafts"] },
        )}
      />
    </Providers>,
  );
  return userEvent.setup();
}

beforeEach(() => {
  queryClient.clear();
});

describe("the queue of drafts to approve shows what an approval publishes (AC-77, AC-112)", () => {
  it("counts the drafts the user may not read in the button and under the list, and approves under the tag of the whole list", async () => {
    serve(2);
    const user = renderQueue();

    const approve = await screen.findByRole("button", {
      name: "Утвердить вместе с 2 записями",
    });

    expect(screen.getByText("и ещё 2 черновые записи")).toBeInTheDocument();
    expect(screen.queryByText(NO_OTHER_DRAFTS)).toBeNull();
    await user.click(approve);
    await waitFor(() => expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1));
    const request = fakeApi.calls(APPROVE_ROUTE)[0];
    expect(
      new URL(request?.url ?? "http://x").searchParams.get("cascade"),
    ).toBe("tag-queue");
    expect(request?.headers.get("If-Match")).toBe('"4"');
  });

  it("says nothing else is published, and offers a plain approval, when the list is empty", async () => {
    serve(0);
    renderQueue();

    expect(await screen.findByText(NO_OTHER_DRAFTS)).toBeInTheDocument();

    expect(screen.getByRole("button", { name: "Утвердить" })).toBeEnabled();
    expect(screen.queryByText(/и ещё/)).toBeNull();
  });
});
