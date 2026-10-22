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

const NOTICE = "Запись уже утверждена другим пользователем или недоступна вам.";
const SECOND_DRAFT = {
  ...DRAFT,
  id: "c2",
  name: "Ответственность",
  version: 2,
};
const TRACK_DRAFT = {
  ...DRAFT,
  kind: "Track",
  id: "t1",
  name: "Инженер-аналитик",
  readiness: { hasLevels: true, levelsWithoutActivePosition: [] },
};
const HEADING_OF_FIRST = "Компетенция Работа в команде";
const HEADING_OF_SECOND = "Компетенция Ответственность";

function serveQueue() {
  let approved = false;
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "hr", email: "hr@test.local", roles: ["Hr"] }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/career/drafts", (request) => {
    const kind = new URL(request.url).searchParams.get("kind");
    if (kind === "Track") return Response.json([TRACK_DRAFT]);
    return Response.json(
      approved
        ? [SECOND_DRAFT, TRACK_DRAFT]
        : [DRAFT, SECOND_DRAFT, TRACK_DRAFT],
    );
  });
  for (const id of [DRAFT.id, SECOND_DRAFT.id]) {
    fakeApi.on(`GET /api/v1/career/competencies/${id}/matrices`, () =>
      Response.json([]),
    );
    fakeApi.on(`GET /api/v1/career/competencies/${id}/publication`, () =>
      Response.json({
        entries: [],
        hiddenEntryCount: 0,
        blockers: [],
        moreCount: 0,
        cascadeTag: `tag-${id}`,
      }),
    );
  }
  fakeApi.on(APPROVE_ROUTE, () => {
    approved = true;
    return Response.json({ id: DRAFT.id });
  });
}

function renderQueueAt(path: string) {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [
            { path: "/desk/drafts", element: <DraftsQueuePage /> },
            {
              path: "/desk/drafts/:kind/:entryId",
              element: <DraftsQueuePage />,
            },
          ],
          { initialEntries: [path] },
        )}
      />
    </Providers>,
  );
  return userEvent.setup();
}

describe("the notice that a record asked for by link is gone judges the first read of the queue only (AC-76)", () => {
  it("says nothing when the user approves the record they opened by link, and opens the next one", async () => {
    serveQueue();
    const user = renderQueueAt("/desk/drafts/competency/c1");
    await screen.findByRole("heading", { name: HEADING_OF_FIRST });

    await user.click(await screen.findByRole("button", { name: "Утвердить" }));

    expect(
      await screen.findByRole("heading", { name: HEADING_OF_SECOND }),
    ).toBeInTheDocument();
    expect(screen.queryByText(NOTICE)).toBeNull();
  });

  it("says nothing when the user narrows the queue to a kind that does not hold the record they opened by link", async () => {
    serveQueue();
    const user = renderQueueAt("/desk/drafts/competency/c1");
    await screen.findByRole("heading", { name: HEADING_OF_FIRST });

    await user.click(screen.getByRole("combobox", { name: "Вид" }));
    await user.click(
      await screen.findByText("Трек", {
        selector: ".ant-select-item-option-content",
      }),
    );

    expect(
      await screen.findByRole("heading", { name: "Трек Инженер-аналитик" }),
    ).toBeInTheDocument();
    expect(screen.queryByText(NOTICE)).toBeNull();
  });

  it("says the record is gone when the first read of the queue does not hold the record asked for", async () => {
    serveQueue();
    renderQueueAt("/desk/drafts/competency/missing");

    expect(await screen.findByText(NOTICE)).toBeInTheDocument();
    expect(
      await screen.findByRole("heading", { name: HEADING_OF_FIRST }),
    ).toBeInTheDocument();
  });
});
