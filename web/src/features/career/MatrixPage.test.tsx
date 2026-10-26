import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  createMemoryRouter,
  createRoutesFromElements,
  MemoryRouter,
  Outlet,
  Route,
  RouterProvider,
  Routes,
  useParams,
} from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { LeaveGuardProvider } from "../../app/leaveGuard";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { careerQueryKey } from "./careerApi";
import { MatrixPage } from "./MatrixPage";
import type { MatrixAction, MatrixVersion } from "./matrixApi";

const UNIT = { id: "unit", name: "Отдел" };
const VERSION_ID = "version";
const VERSION_TAG = 5;
const POINTS = [
  { score: 0, label: "Нет" },
  { score: 2, label: "Уверенно" },
  { score: 4, label: "Эксперт" },
];

function version(fields: Partial<MatrixVersion> = {}): MatrixVersion {
  return {
    id: VERSION_ID,
    matrixId: "matrix",
    number: 1,
    status: "OnReview",
    step: "Superior",
    authorId: "author",
    authorName: "Автор",
    createdAt: "2026-10-01T10:00:00Z",
    publishedAt: null,
    content: {
      levels: [{ levelId: "junior", minOverallPercent: null }],
      competencies: [],
      blockWeights: [],
      requirements: [],
    },
    entries: {
      direction: { id: "direction", name: "Разработка", status: "Active" },
      track: { id: "track", name: "Бэкенд", status: "Active" },
      scales: [
        { id: "scale", name: "Базовая", points: POINTS, status: "Active" },
      ],
      levels: [{ id: "junior", name: "Младший", status: "Active" }],
      competencies: [],
    },
    superiorApprover: { userId: "superior", name: "Руководитель" },
    hr: { userId: "hr", name: "Кадровик" },
    returnedWithChanges: false,
    actions: [],
    version: VERSION_TAG,
    ...fields,
  };
}

function serve(
  shown: MatrixVersion,
  {
    heads = false,
    published = false,
    inWork = true,
    inWorkTag = VERSION_TAG,
  } = {},
) {
  const ref = (status: MatrixVersion["status"]) => ({
    id: shown.id,
    number: shown.number,
    status,
    version: inWorkTag,
  });
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: heads ? [UNIT] : [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/career/matrices/matrix", () =>
    Response.json({
      id: "matrix",
      orgUnitId: UNIT.id,
      orgUnitName: UNIT.name,
      directionId: "direction",
      directionName: "Разработка",
      trackId: "track",
      trackName: "Бэкенд",
      publishedVersion: published ? ref("Published") : null,
      inWorkVersion: inWork ? ref(shown.status) : null,
      createdAt: "2026-10-01T10:00:00Z",
    }),
  );
  fakeApi.on("GET /api/v1/career/matrices/matrix/versions", () =>
    Response.json([
      {
        id: shown.id,
        number: shown.number,
        status: shown.status,
        authorId: shown.authorId,
        authorName: shown.authorName,
        createdAt: shown.createdAt,
        publishedAt: shown.publishedAt,
      },
    ]),
  );
  fakeApi.on(`GET /api/v1/career/matrix-versions/${shown.id}`, () =>
    Response.json(shown),
  );
  fakeApi.on(`GET /api/v1/career/matrix-versions/${shown.id}/history`, () =>
    Response.json({ variants: [], decisions: [] }),
  );
}

function DeskStub() {
  const { scenario, id } = useParams();
  return <p>{`desk:${scenario}:${id}`}</p>;
}

async function openPage({ guarded = false } = {}) {
  const routes = (
    <>
      <Route path="/career/matrices/:id" element={<MatrixPage />} />
      <Route path="/desk/:scenario/:id" element={<DeskStub />} />
    </>
  );
  render(
    <Providers>
      {guarded ? (
        <RouterProvider
          router={createMemoryRouter(
            [
              {
                element: (
                  <LeaveGuardProvider>
                    <Outlet />
                  </LeaveGuardProvider>
                ),
                children: createRoutesFromElements(routes),
              },
            ],
            { initialEntries: ["/career/matrices/matrix"] },
          )}
        />
      ) : (
        <MemoryRouter initialEntries={["/career/matrices/matrix"]}>
          <Routes>{routes}</Routes>
        </MemoryRouter>
      )}
    </Providers>,
  );
  await screen.findByRole("heading", { name: "Матрица «Бэкенд» — Отдел" });
  await screen.findByRole("combobox", { name: "Версия" });
  await waitFor(() =>
    expect(queryClient.getQueryData([...careerQueryKey, "me"])).toBeDefined(),
  );
  await act(async () => undefined);
  return userEvent.setup();
}

/** «История» of a returned version with three more actions before it: it sits behind the «…» menu, not in view. */
async function openHistoryFromMenu(user: ReturnType<typeof userEvent.setup>) {
  expect(
    screen.queryByRole("button", { name: "История" }),
  ).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Действия с версией" }));
  await user.click(await screen.findByRole("menuitem", { name: "История" }));
}

async function dialogTitled(title: string) {
  const [heading] = await screen.findAllByText(title, {
    selector: ".ant-modal-title",
  });
  return heading?.closest('[role="dialog"]') as HTMLElement;
}

const afterAction = () => version({ status: "Published", step: null });

beforeEach(() => {
  queryClient.clear();
});

describe("actions on a matrix version (AC-15, AC-16, AC-19)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it.each<[MatrixAction, string, string]>([
    ["AcceptAndRevise", "Принять и доработать", "accept-and-revise"],
    ["RollbackChanges", "Откатить правки", "rollback-changes"],
  ])(
    "%s is offered as «%s» and sends %s against the version it shows",
    async (action, label, transition) => {
      const route = `POST /api/v1/career/matrix-versions/${VERSION_ID}/${transition}`;
      serve(version({ actions: [action] }));
      fakeApi.on(route, () => {
        fakeApi.on(`GET /api/v1/career/matrix-versions/${VERSION_ID}`, () =>
          Response.json(afterAction()),
        );
        return Response.json(afterAction());
      });
      const user = await openPage();

      await user.click(screen.getByRole("button", { name: label }));

      await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
      expect(fakeApi.calls(route)[0]?.headers.get("If-Match")).toBe(
        `"${VERSION_TAG}"`,
      );
      expect(await screen.findByText("Опубликована")).toBeInTheDocument();
    },
  );

  it.each<[MatrixAction, string, string]>([
    ["Submit", "Перейти к отправке в мастере", "matrix-builder"],
    ["Approve", "Согласовать версию", "review"],
    ["AcceptChanges", "Доработать возвращённую версию", "rework"],
  ])(
    "%s is offered as «%s» and leads to the %s screen of the desk",
    async (action, label, scenario) => {
      serve(version({ actions: [action] }));
      const user = await openPage();

      await user.click(screen.getByRole("link", { name: label }));

      expect(
        await screen.findByText(`desk:${scenario}:${VERSION_ID}`),
      ).toBeInTheDocument();
    },
  );

  it("offers only the actions the server lists, and none when it lists none", async () => {
    serve(version({ actions: [] }));
    await openPage();

    for (const label of [
      "Перейти к отправке в мастере",
      "Согласовать версию",
      "Вернуть с правками",
      "Доработать возвращённую версию",
      "Принять и доработать",
      "Откатить правки",
      "Создать новую версию",
    ]) {
      expect(
        screen.queryByRole("button", { name: label }),
      ).not.toBeInTheDocument();
    }
  });

  it("returns a version with the comment that was typed, trimmed", async () => {
    const route = `POST /api/v1/career/matrix-versions/${VERSION_ID}/return`;
    serve(version({ actions: ["Return"] }));
    fakeApi.on(route, () => Response.json(version({ status: "Returned" })));
    const user = await openPage();

    await user.click(
      screen.getByRole("button", { name: "Вернуть с правками" }),
    );
    const dialog = await screen.findByRole("dialog");
    await user.type(
      within(dialog).getByLabelText("Комментарий"),
      "  Нужны примеры  ",
    );
    await user.click(
      within(dialog).getByRole("button", { name: "Вернуть с правками" }),
    );

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(await fakeApi.calls(route)[0]?.json()).toEqual({
      comment: "Нужны примеры",
    });
  });

  it("returns a version with no comment as an empty one, when the approver only changed the content", async () => {
    const route = `POST /api/v1/career/matrix-versions/${VERSION_ID}/return`;
    serve(version({ actions: ["Return"] }));
    fakeApi.on(route, () => Response.json(version({ status: "Returned" })));
    const user = await openPage();

    await user.click(
      screen.getByRole("button", { name: "Вернуть с правками" }),
    );
    const dialog = await screen.findByRole("dialog");
    await user.click(
      within(dialog).getByRole("button", { name: "Вернуть с правками" }),
    );

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(await fakeApi.calls(route)[0]?.json()).toEqual({ comment: null });
  });

  it("shows a refusal by turn in the server's words and keeps the action", async () => {
    const route = `POST /api/v1/career/matrix-versions/${VERSION_ID}/accept-and-revise`;
    serve(version({ actions: ["AcceptAndRevise"] }));
    fakeApi.on(route, () =>
      Response.json(
        {
          title: "Недостаточно прав",
          detail: "Сейчас ход другого участника согласования",
        },
        { status: 403 },
      ),
    );
    const user = await openPage();

    await user.click(
      screen.getByRole("button", { name: "Принять и доработать" }),
    );

    expect(
      await screen.findByText("Не удалось выполнить действие"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Сейчас ход другого участника согласования"),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Принять и доработать" }),
    ).toBeEnabled();
  });

  it("lists every problem that keeps a version from being handed over, as the server wrote them", async () => {
    const route = `POST /api/v1/career/matrix-versions/${VERSION_ID}/accept-and-revise`;
    serve(version({ actions: ["AcceptAndRevise"] }));
    fakeApi.on(route, () =>
      Response.json(
        {
          title: "One or more validation errors occurred.",
          errors: {
            content: [
              "У уровня «Младший» нет активной должности",
              "В матрице нет компетенций",
            ],
          },
        },
        { status: 400 },
      ),
    );
    const user = await openPage();

    await user.click(
      screen.getByRole("button", { name: "Принять и доработать" }),
    );

    expect(
      within(await screen.findByRole("alert"))
        .getAllByRole("listitem")
        .map((item) => item.textContent),
    ).toEqual([
      "У уровня «Младший» нет активной должности",
      "В матрице нет компетенций",
    ]);
  });
});

describe("a new version of a published matrix (AC-15)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  const published = () =>
    version({ status: "Published", step: null, actions: [] });

  it("is offered to the head of the unit while no version is in work, and starts a copy", async () => {
    const route = "POST /api/v1/career/matrices/matrix/versions";
    serve(published(), { heads: true, published: true, inWork: false });
    fakeApi.on(route, () =>
      Response.json(
        version({ id: "second", number: 2, status: "Draft", step: null }),
      ),
    );
    fakeApi.on("GET /api/v1/career/matrix-versions/second", () =>
      Response.json(
        version({ id: "second", number: 2, status: "Draft", step: null }),
      ),
    );
    fakeApi.on("GET /api/v1/career/matrix-versions/second/history", () =>
      Response.json({ variants: [], decisions: [] }),
    );
    const user = await openPage();

    await user.click(
      await screen.findByRole("button", { name: "Создать новую версию" }),
    );

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(await screen.findByText("Черновик")).toBeInTheDocument();
  });

  it("is not offered to anyone but the head, nor while a version is in work", async () => {
    serve(published(), { heads: false, published: true, inWork: false });
    await openPage();
    expect(
      screen.queryByRole("button", { name: "Создать новую версию" }),
    ).not.toBeInTheDocument();
  });

  it("is not offered to the head while another version is in work", async () => {
    serve(published(), { heads: true, published: true, inWork: true });
    await openPage();
    await screen.findByText("Опубликована");
    expect(
      screen.queryByRole("button", { name: "Создать новую версию" }),
    ).not.toBeInTheDocument();
  });
});

describe("what an approver changed in a returned version (AC-16, AC-19)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  const JUNIOR_LEVEL = "junior";
  const DESIGN = "design";
  const requirement = {
    levelId: JUNIOR_LEVEL,
    competencyId: DESIGN,
    requiredScore: 2,
  } as const;

  const returned = () => {
    const shown = version({
      status: "Returned",
      step: "Superior",
      returnedWithChanges: true,
      actions: ["AcceptChanges", "AcceptAndRevise", "RollbackChanges"],
    });
    return {
      ...shown,
      content: {
        levels: [{ levelId: JUNIOR_LEVEL, minOverallPercent: null }],
        competencies: [{ competencyId: DESIGN, blockId: null }],
        blockWeights: [{ levelId: JUNIOR_LEVEL, blockId: null, percent: 100 }],
        requirements: [requirement],
      },
      entries: {
        ...shown.entries,
        competencies: [
          {
            id: DESIGN,
            name: "Проектирование",
            description: null,
            directionId: null,
            type: "Profile",
            versionNumber: 1,
            status: "Active",
            scaleId: "scale",
          },
        ],
      },
    } satisfies MatrixVersion;
  };

  const variant = (number: number, kind: "Author" | "Approver") => ({
    number,
    kind,
    createdById: "someone",
    createdByName: "Кто-то",
    createdAt: "2026-10-01T10:00:00Z",
  });

  it("marks the row and the cell the approver changed, in words and not by colour alone", async () => {
    serve(returned());
    fakeApi.on(`GET /api/v1/career/matrix-versions/${VERSION_ID}/history`, () =>
      Response.json({
        variants: [variant(1, "Author"), variant(2, "Approver")],
        decisions: [],
      }),
    );
    fakeApi.on(
      `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/2/diff`,
      () =>
        Response.json({
          number: 2,
          previousNumber: 1,
          before: returned().content,
          after: returned().content,
          changes: [
            {
              kind: "Requirement",
              type: "Changed",
              levelId: JUNIOR_LEVEL,
              blockId: null,
              competencyId: DESIGN,
            },
          ],
        }),
    );
    await openPage();

    const row = (await screen.findByText("Проектирование")).closest(
      "tr",
    ) as HTMLElement;
    await waitFor(() => expect(row).toHaveClass("matrix-changed-row"));
    const cell = row.querySelector("td.matrix-changed") as HTMLElement;
    expect(
      within(cell).getByRole("img", { name: "Изменено согласующим" }),
    ).toBeInTheDocument();
    expect(
      within(row).getAllByRole("img", { name: "Изменено согласующим" }).length,
    ).toBeGreaterThan(0);
  });

  it("marks nothing, and asks for no difference, when the last variant is the author's", async () => {
    serve(returned());
    fakeApi.on(`GET /api/v1/career/matrix-versions/${VERSION_ID}/history`, () =>
      Response.json({ variants: [variant(1, "Author")], decisions: [] }),
    );
    await openPage();

    const row = (await screen.findByText("Проектирование")).closest("tr");
    expect(screen.queryByText("Изменено согласующим")).not.toBeInTheDocument();
    expect(row).not.toHaveClass("matrix-changed-row");
    expect(
      fakeApi.calls(
        `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/1/diff`,
      ),
    ).toHaveLength(0);
  });

  it("lists in words what the approver added, removed and changed, with the value before and after, and names a competency that left the version", async () => {
    const before = {
      ...returned().content,
      levels: [{ levelId: JUNIOR_LEVEL, minOverallPercent: 50 }],
    };
    const after = {
      ...before,
      levels: [{ levelId: JUNIOR_LEVEL, minOverallPercent: 75 }],
      sharedBlocks: [{ blockId: "extra" }],
      competencies: [],
      requirements: [],
    };
    const shown = returned();
    serve({
      ...shown,
      content: after,
      entries: {
        ...shown.entries,
        competencies: [],
        sharedBlocks: [
          {
            id: "extra",
            name: "Дополнительные",
            status: "Active",
            competencyIds: [],
          },
        ],
      },
    });
    fakeApi.on(`GET /api/v1/career/matrix-versions/${VERSION_ID}/history`, () =>
      Response.json({
        variants: [variant(1, "Author"), variant(2, "Approver")],
        decisions: [],
      }),
    );
    fakeApi.on(
      `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/2/diff`,
      () =>
        Response.json({
          number: 2,
          previousNumber: 1,
          before,
          after,
          changes: [
            {
              kind: "Level",
              type: "Changed",
              levelId: JUNIOR_LEVEL,
              blockId: null,
              competencyId: null,
            },
            {
              kind: "Block",
              type: "Added",
              levelId: null,
              blockId: "extra",
              competencyId: null,
            },
            {
              kind: "Competency",
              type: "Removed",
              levelId: null,
              blockId: null,
              competencyId: DESIGN,
            },
            {
              kind: "Requirement",
              type: "Removed",
              levelId: JUNIOR_LEVEL,
              blockId: null,
              competencyId: DESIGN,
            },
          ],
        }),
    );
    fakeApi.on(`GET /api/v1/career/competencies/${DESIGN}`, () =>
      Response.json({
        id: DESIGN,
        name: "Проектирование",
        description: null,
        directionId: null,
        type: "Profile",
        scaleId: "scale",
        status: "Active",
        createdAt: "2026-10-01T10:00:00Z",
        versionNumber: 1,
        version: 1,
        canEdit: false,
        canDelete: false,
      }),
    );
    await openPage();

    expect(
      await screen.findByText("Изменено: уровень «Младший»"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Было: Минимальный общий порог: 50 %"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Стало: Минимальный общий порог: 75 %"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Добавлено: блок «Дополнительные»"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Стало: Блок «Дополнительные»"),
    ).toBeInTheDocument();
    expect(
      await screen.findByText("Удалено: компетенция «Проектирование»"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Было: Блок «Вне блоков»; Тип: Профильная"),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "Удалено: требуемый балл «Проектирование», уровень «Младший»",
      ),
    ).toBeInTheDocument();
    expect(screen.getByText("Было: Требуемый балл: 2")).toBeInTheDocument();
  });

  describe("the history of a version", () => {
    const earlier = () => {
      const shown = returned();
      return {
        ...shown,
        content: {
          ...shown.content,
          levels: [{ levelId: JUNIOR_LEVEL, minOverallPercent: 40 }],
        },
      };
    };

    function serveHistory() {
      serve(returned());
      fakeApi.on(
        `GET /api/v1/career/matrix-versions/${VERSION_ID}/history`,
        () =>
          Response.json({
            variants: [variant(1, "Author"), variant(2, "Approver")],
            decisions: [],
          }),
      );
    }

    it("opens a variant read-only, as «Вариант {n}», with what the approver changed in it", async () => {
      serveHistory();
      fakeApi.on(
        `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/1`,
        () =>
          Response.json({
            ...variant(1, "Author"),
            content: earlier().content,
          }),
      );
      const user = await openPage();

      await openHistoryFromMenu(user);
      const history = await dialogTitled("История");
      await user.click(within(history).getByRole("button", { name: /Автор/ }));

      const opened = await dialogTitled("Вариант 1");
      expect(
        await within(opened).findByText("Проектирование"),
      ).toBeInTheDocument();
      expect(
        within(opened).queryByRole("button", { name: "Сохранить" }),
      ).not.toBeInTheDocument();
      expect(within(opened).queryByRole("spinbutton")).not.toBeInTheDocument();
      expect(
        fakeApi.calls(
          `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/1/diff`,
        ),
      ).toHaveLength(0);
    });

    it("shows in an approver's variant what the approver changed in it", async () => {
      const content = (threshold: number) => ({
        ...returned().content,
        levels: [{ levelId: JUNIOR_LEVEL, minOverallPercent: threshold }],
      });
      serveHistory();
      fakeApi.on(
        `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/2`,
        () =>
          Response.json({ ...variant(2, "Approver"), content: content(60) }),
      );
      fakeApi.on(
        `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/2/diff`,
        () =>
          Response.json({
            number: 2,
            previousNumber: 1,
            before: content(40),
            after: content(60),
            changes: [
              {
                kind: "Level",
                type: "Changed",
                levelId: JUNIOR_LEVEL,
                blockId: null,
                competencyId: null,
              },
            ],
          }),
      );
      const user = await openPage();

      await openHistoryFromMenu(user);
      const history = await dialogTitled("История");
      await user.click(
        within(history).getByRole("button", { name: /Согласующий/ }),
      );

      const opened = await dialogTitled("Вариант 2");
      expect(
        await within(opened).findByText("Изменено: уровень «Младший»"),
      ).toBeInTheDocument();
      expect(
        within(opened).getByText("Было: Минимальный общий порог: 40 %"),
      ).toBeInTheDocument();
      expect(
        within(opened).getByText("Стало: Минимальный общий порог: 60 %"),
      ).toBeInTheDocument();
    });

    it("says when a variant cannot be loaded", async () => {
      serveHistory();
      fakeApi.on(
        `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/1`,
        () => problem(500, { title: "Сбой" }),
      );
      const user = await openPage();

      await openHistoryFromMenu(user);
      const history = await dialogTitled("История");
      await user.click(within(history).getByRole("button", { name: /Автор/ }));

      const opened = await dialogTitled("Вариант 1");
      expect(
        await within(opened).findByText("Не удалось загрузить вариант"),
      ).toBeInTheDocument();
    });
  });
});

describe("editing the content of a draft version (AC-14, AC-15, AC-17)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  const CONTENT_ROUTE = `PUT /api/v1/career/matrix-versions/${VERSION_ID}/content`;

  const draft = () =>
    version({ status: "Draft", step: null, actions: ["Edit", "Submit"] });

  function serveEditor(shown = draft()) {
    serve(shown);
    fakeApi.on("GET /api/v1/career/levels", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 200 }),
    );
    fakeApi.on("GET /api/v1/career/competencies", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 200 }),
    );
    fakeApi.on(CONTENT_ROUTE, async (request) =>
      Response.json({
        ...shown,
        content: await request.json(),
        version: VERSION_TAG + 1,
      }),
    );
  }

  it("saves the threshold that was typed against the version the editor was opened with", async () => {
    serveEditor();
    const user = await openPage();

    const threshold = await screen.findByRole("spinbutton", {
      name: "Минимальный общий порог, %: Младший",
    });
    await user.type(threshold, "60");
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(fakeApi.calls(CONTENT_ROUTE)).toHaveLength(1));
    const call = fakeApi.calls(CONTENT_ROUTE)[0];
    expect(call?.headers.get("If-Match")).toBe(`"${VERSION_TAG}"`);
    const saved = await call?.json();
    expect(saved?.levels).toEqual([
      { levelId: "junior", minOverallPercent: 60 },
    ]);
  });

  function editorWithACompetency() {
    const shown = draft();
    const content = (minOverallPercent: number | null) => ({
      levels: [{ levelId: "junior", minOverallPercent }],
      competencies: [{ competencyId: "design", blockId: null }],
      blockWeights: [{ levelId: "junior", blockId: null, percent: 100 }],
      requirements: [
        { levelId: "junior", competencyId: "design", requiredScore: 2 },
      ],
    });
    const opened = {
      ...shown,
      content: content(null),
      entries: {
        ...shown.entries,
        competencies: [
          {
            id: "design",
            name: "Проектирование",
            description: null,
            directionId: null,
            type: "Profile" as const,
            versionNumber: 1,
            status: "Active" as const,
            scaleId: "scale",
          },
        ],
      },
    };
    const saved = {
      ...opened,
      content: content(60),
      version: VERSION_TAG + 1,
    };
    return { opened, saved };
  }

  it("marks no competency as lacking a requirement, since its cell shows the lowest score of the scale", async () => {
    const { opened } = editorWithACompetency();
    serveEditor({
      ...opened,
      content: { ...opened.content, requirements: [] },
    });
    await openPage();

    const row = (await screen.findByText("Проектирование")).closest(
      "tr",
    ) as HTMLElement;

    expect(
      within(row).queryByRole("img", { name: "Нет требований" }),
    ).not.toBeInTheDocument();
  });

  it("locks every control of the editor while the content is being saved, so that nothing typed then is lost, and frees them once it is saved", async () => {
    const held = deferred<Response>();
    const { opened, saved } = editorWithACompetency();
    serveEditor(opened);
    fakeApi.on(CONTENT_ROUTE, () => held.promise);
    const user = await openPage();
    const threshold = await screen.findByRole("spinbutton", {
      name: "Минимальный общий порог, %: Младший",
    });
    await user.type(threshold, "60");

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(threshold).toBeDisabled());
    await user.type(threshold, "7");
    const controls: [string, HTMLElement[]][] = [
      ["percent field", screen.getAllByRole("spinbutton")],
      [
        "competency type",
        [screen.getByRole("combobox", { name: /Тип компетенции/ })],
      ],
      [
        "requirement cell",
        [screen.getByRole("combobox", { name: "Проектирование, Младший" })],
      ],
      ["add level", [screen.getByRole("button", { name: /Добавить уровень/ })]],
      ["save", [screen.getByRole("button", { name: /Сохранить/ })]],
      ["delete", screen.getAllByRole("button", { name: /Удалить/ })],
    ];
    expect(
      controls.flatMap(([kind, found]) =>
        found
          .filter((control) => !control.hasAttribute("disabled"))
          .map(() => kind),
      ),
    ).toEqual([]);
    expect(threshold).toHaveValue("60");
    fakeApi.on(`GET /api/v1/career/matrix-versions/${VERSION_ID}`, () =>
      Response.json(saved),
    );
    held.resolve(Response.json(saved));

    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: /Добавить уровень/ }),
      ).toBeEnabled(),
    );
    const freed = screen.getByRole("spinbutton", {
      name: "Минимальный общий порог, %: Младший",
    });
    expect(freed).toBeEnabled();
    expect(freed).toHaveValue("60");
  });

  it("frees the editor again, with what was typed still in it, when the save is refused", async () => {
    serveEditor(editorWithACompetency().opened);
    fakeApi.on(CONTENT_ROUTE, () => problem(500, { title: "Сбой" }));
    const user = await openPage();
    const threshold = await screen.findByRole("spinbutton", {
      name: "Минимальный общий порог, %: Младший",
    });
    await user.type(threshold, "60");

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    expect(
      await screen.findByText("Не удалось сохранить запись"),
    ).toBeInTheDocument();
    await waitFor(() => expect(threshold).toBeEnabled());
    expect(threshold).toHaveValue("60");
    expect(screen.getByRole("button", { name: /Сохранить/ })).toBeEnabled();
  });

  it("offers no editing and no save for a version whose turn is not the user's", async () => {
    serve(version({ status: "OnReview", actions: [] }));
    await openPage();

    expect(
      screen.queryByRole("button", { name: "Сохранить" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Добавить уровень" }),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole("spinbutton")).not.toBeInTheDocument();
  });

  describe("while the form has edits that are not saved (AC-15, AC-17)", () => {
    const UNSAVED_REASON = "Сначала сохраните изменения";
    const leaveQuestion = () =>
      screen.queryByRole("dialog", { name: "Перейти без сохранения?" });
    const SUBMIT = "Перейти к отправке в мастере";
    const DESK = `desk:matrix-builder:${VERSION_ID}`;
    const deskLink = (label: string) =>
      screen.getByText(label).closest("a") as HTMLElement;
    const handOver = () => deskLink(SUBMIT);
    const THRESHOLD = "Минимальный общий порог, %: Младший";

    const REVIEW = () =>
      version({
        status: "OnReview",
        step: "Superior",
        actions: ["Edit", "Approve", "Return"],
      });
    const RETURN = "Вернуть с правками";

    it("keeps the link to the desk scenario enabled with unsaved edits, and following it leads there", async () => {
      serveEditor();
      const user = await openPage();
      const threshold = await screen.findByRole("spinbutton", {
        name: THRESHOLD,
      });

      await user.type(threshold, "60");

      expect(handOver()).not.toHaveAttribute("aria-disabled", "true");
      await user.click(handOver());
      expect(await screen.findByText(DESK)).toBeInTheDocument();
    });

    it("asks before another version is chosen over unsaved edits, stays on the version and the value on «Остаться», and asks no more once they are saved", async () => {
      const other = version({
        id: "other",
        number: 2,
        status: "Published",
        step: null,
        actions: [],
      });
      serveEditor();
      fakeApi.on("GET /api/v1/career/matrices/matrix/versions", () =>
        Response.json(
          [draft(), other].map(({ id, number, status }) => ({
            id,
            number,
            status,
            authorId: "author",
            authorName: "Автор",
            createdAt: "2026-10-01T10:00:00Z",
            publishedAt: null,
          })),
        ),
      );
      fakeApi.on("GET /api/v1/career/matrix-versions/other", () =>
        Response.json(other),
      );
      fakeApi.on("GET /api/v1/career/matrix-versions/other/history", () =>
        Response.json({ variants: [], decisions: [] }),
      );
      const user = await openPage({ guarded: true });
      const threshold = await screen.findByRole("spinbutton", {
        name: THRESHOLD,
      });
      await user.type(threshold, "60");
      const chooseOther = async () => {
        await user.click(screen.getByRole("combobox", { name: "Версия" }));
        await user.click(
          await screen.findByText("Версия 2", {
            selector: ".ant-select-item-option-content",
          }),
        );
      };

      await chooseOther();
      await user.click(await screen.findByRole("button", { name: "Остаться" }));

      await waitFor(() => expect(leaveQuestion()).not.toBeInTheDocument());
      expect(screen.getByText("Черновик")).toBeInTheDocument();
      expect(threshold).toHaveValue("60");

      await user.click(screen.getByRole("button", { name: "Сохранить" }));
      await waitFor(() => expect(fakeApi.calls(CONTENT_ROUTE)).toHaveLength(1));
      await waitFor(() =>
        expect(screen.queryByText(UNSAVED_REASON)).not.toBeInTheDocument(),
      );
      await chooseOther();

      expect(await screen.findByText("Опубликована")).toBeInTheDocument();
      expect(leaveQuestion()).not.toBeInTheDocument();
    });

    it("holds the actions on the saved version back while a save is in flight, even when nothing was changed", async () => {
      const held = deferred<Response>();
      serveEditor(REVIEW());
      fakeApi.on(CONTENT_ROUTE, () => held.promise);
      const user = await openPage();
      await screen.findByRole("spinbutton", { name: THRESHOLD });
      expect(screen.getByRole("button", { name: RETURN })).toBeEnabled();

      await user.click(screen.getByRole("button", { name: "Сохранить" }));

      await waitFor(() =>
        expect(screen.getByRole("button", { name: RETURN })).toBeDisabled(),
      );
      expect(screen.getByText(UNSAVED_REASON)).toBeInTheDocument();
      held.resolve(Response.json({ ...draft(), version: VERSION_TAG + 1 }));
      await waitFor(() =>
        expect(screen.getByRole("button", { name: RETURN })).toBeEnabled(),
      );
      expect(screen.queryByText(UNSAVED_REASON)).not.toBeInTheDocument();
    });

    it("holds a return back, says why, and frees it once the edits are saved", async () => {
      serveEditor(REVIEW());
      const user = await openPage();

      await user.type(
        await screen.findByRole("spinbutton", { name: THRESHOLD }),
        "60",
      );

      expect(screen.getByRole("button", { name: RETURN })).toBeDisabled();
      expect(screen.getByText(UNSAVED_REASON)).toBeInTheDocument();
      expect(deskLink("Согласовать версию")).not.toHaveAttribute(
        "aria-disabled",
        "true",
      );

      await user.click(screen.getByRole("button", { name: "Сохранить" }));

      await waitFor(() =>
        expect(screen.getByRole("button", { name: RETURN })).toBeEnabled(),
      );
      expect(screen.queryByText(UNSAVED_REASON)).not.toBeInTheDocument();
    });

    it("gives the reason only where there is a blocked button to explain, not when the user can only edit", async () => {
      serveEditor(version({ status: "Draft", step: null, actions: ["Edit"] }));
      const user = await openPage();

      await user.type(
        await screen.findByRole("spinbutton", { name: THRESHOLD }),
        "60",
      );

      expect(screen.getByRole("button", { name: "Сохранить" })).toBeEnabled();
      expect(screen.queryByText(UNSAVED_REASON)).not.toBeInTheDocument();
    });

    it("says nothing and holds nothing back for a version the user cannot edit", async () => {
      serve(version({ status: "OnReview", actions: ["Approve"] }));
      await openPage();

      expect(deskLink("Согласовать версию")).not.toHaveAttribute(
        "aria-disabled",
        "true",
      );
      expect(screen.queryByText(UNSAVED_REASON)).not.toBeInTheDocument();
    });
  });

  it("deletes the matrix against the tag of the version the page shows, not a fresher one read on the way", async () => {
    const route = "DELETE /api/v1/career/matrices/matrix";
    serve(draft(), { heads: true, inWorkTag: VERSION_TAG - 1 });
    fakeApi.on(route, () => new Response(null, { status: 204 }));
    fakeApi.on("GET /api/v1/career/matrices", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 20 }),
    );
    const user = await openPage();
    fakeApi.on(`GET /api/v1/career/matrix-versions/${VERSION_ID}`, () =>
      Response.json({ ...draft(), version: VERSION_TAG + 1 }),
    );

    await user.click(
      screen.getByRole("button", { name: "Действия с версией" }),
    );
    await user.click(await screen.findByRole("menuitem", { name: "Удалить" }));
    await user.click(
      within(await screen.findByRole("dialog")).getByRole("button", {
        name: "Удалить",
      }),
    );

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(fakeApi.calls(route)[0]?.headers.get("If-Match")).toBe(
      `"${VERSION_TAG}"`,
    );
  });
});

describe("the levels of a version in the order of the track (AC-11, AC-17)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  const level = (id: string, name: string, position: number) => ({
    id,
    name,
    status: "Active" as const,
    directionId: "direction",
    trackId: "track",
    position,
    version: 1,
    canEdit: false,
    canDelete: false,
  });

  async function levelColumns(status: MatrixVersion["status"]) {
    const shown = version({
      status,
      step: null,
      actions: [],
      content: {
        levels: [
          { levelId: "senior", minOverallPercent: null },
          { levelId: "junior", minOverallPercent: null },
        ],
        competencies: [],
        blockWeights: [],
        requirements: [],
      },
      entries: {
        ...version().entries,
        levels: [
          { id: "junior", name: "Младший", status: "Active" },
          { id: "senior", name: "Старший", status: "Active" },
        ],
      },
    });
    serve(shown, { published: true, inWork: false });
    fakeApi.on("GET /api/v1/career/levels", () =>
      Response.json({
        items: [level("senior", "Старший", 1), level("junior", "Младший", 2)],
        total: 2,
        page: 1,
        pageSize: 200,
      }),
    );
    await openPage();
    const grid = document.querySelector(".matrix-grid") as HTMLElement;
    await waitFor(() =>
      expect(within(grid).getAllByRole("columnheader")).toHaveLength(4),
    );
    return within(grid)
      .getAllByRole("columnheader")
      .map((header) => header.textContent);
  }

  it.each<[MatrixVersion["status"]]>([["Published"], ["Archived"]])(
    "shows a %s version's levels in the order they had when it was published, although the track has been put in another order since",
    async (status) => {
      expect(await levelColumns(status)).toEqual([
        "Компетенция",
        "Тип",
        "Младший",
        "Старший",
      ]);
    },
  );

  it("shows a version that is not published yet in the order the track has now", async () => {
    expect(await levelColumns("OnReview")).toEqual([
      "Компетенция",
      "Тип",
      "Старший",
      "Младший",
    ]);
  });
});
