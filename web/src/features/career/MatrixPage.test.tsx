import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { careerQueryKey } from "./careerApi";
import { MatrixPage } from "./MatrixPage";
import type { MatrixAction, MatrixVersion } from "./matrixApi";

const UNIT = { id: "unit", name: "Отдел" };
const VERSION_ID = "version";
const VERSION_TAG = 5;

function version(fields: Partial<MatrixVersion> = {}): MatrixVersion {
  return {
    id: VERSION_ID,
    matrixId: "matrix",
    number: 1,
    status: "OnReview",
    step: "Superior",
    scaleId: "scale",
    authorId: "author",
    authorName: "Автор",
    createdAt: "2026-10-01T10:00:00Z",
    publishedAt: null,
    content: {
      levels: [{ levelId: "junior", minOverallPercent: null }],
      blocks: [],
      competencies: [],
      blockWeights: [],
      requirements: [],
    },
    entries: {
      direction: { id: "direction", name: "Разработка", status: "Active" },
      track: { id: "track", name: "Бэкенд", status: "Active" },
      scale: { id: "scale", name: "Базовая", points: [], status: "Active" },
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
  { heads = false, published = false, inWork = true } = {},
) {
  const ref = (status: MatrixVersion["status"]) => ({
    id: shown.id,
    number: shown.number,
    status,
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

async function openPage() {
  render(
    <Providers>
      <MemoryRouter initialEntries={["/career/matrices/matrix"]}>
        <Routes>
          <Route path="/career/matrices/:id" element={<MatrixPage />} />
        </Routes>
      </MemoryRouter>
    </Providers>,
  );
  await screen.findByRole("heading", { name: "Матрица «Бэкенд» — Отдел" });
  await screen.findByRole("button", { name: "История" });
  await waitFor(() =>
    expect(queryClient.getQueryData([...careerQueryKey, "me"])).toBeDefined(),
  );
  await act(async () => undefined);
  return userEvent.setup();
}

const afterAction = () => version({ status: "Published", step: null });

beforeEach(() => {
  queryClient.clear();
});

describe("actions on a matrix version (AC-15, AC-16, AC-19)", () => {
  it.each<[MatrixAction, string, string]>([
    ["Submit", "Отправить на согласование", "submit"],
    ["Approve", "Согласовать", "approve"],
    ["AcceptChanges", "Принять правки", "accept-changes"],
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

  it("offers only the actions the server lists, and none when it lists none", async () => {
    serve(version({ actions: [] }));
    await openPage();

    for (const label of [
      "Отправить на согласование",
      "Согласовать",
      "Вернуть с правками",
      "Принять правки",
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
    const route = `POST /api/v1/career/matrix-versions/${VERSION_ID}/approve`;
    serve(version({ actions: ["Approve"] }));
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

    await user.click(screen.getByRole("button", { name: "Согласовать" }));

    expect(
      await screen.findByText("Не удалось выполнить действие"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Сейчас ход другого участника согласования"),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Согласовать" })).toBeEnabled();
  });

  it("lists every problem that keeps a version from being handed over, as the server wrote them", async () => {
    const route = `POST /api/v1/career/matrix-versions/${VERSION_ID}/submit`;
    serve(version({ actions: ["Submit"] }));
    fakeApi.on(route, () =>
      Response.json(
        {
          title: "One or more validation errors occurred.",
          errors: {
            content: [
              "У уровня «Младший» нет требований",
              "У критической компетенции «Проектирование» не задан минимальный уровень",
            ],
          },
        },
        { status: 400 },
      ),
    );
    const user = await openPage();

    await user.click(
      screen.getByRole("button", { name: "Отправить на согласование" }),
    );

    expect(
      (await screen.findAllByRole("listitem")).map((item) => item.textContent),
    ).toEqual([
      "У уровня «Младший» нет требований",
      "У критической компетенции «Проектирование» не задан минимальный уровень",
    ]);
  });
});

describe("a new version of a published matrix (AC-15)", () => {
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

describe("what an approver changed in a returned version (AC-16, AC-19)", () => {
  const JUNIOR_LEVEL = "junior";
  const DESIGN = "design";
  const requirement = {
    levelId: JUNIOR_LEVEL,
    competencyId: DESIGN,
    requiredScore: 2,
    type: "Profile",
    weight: 1,
    isMandatory: false,
    expectedBehavior: null,
    evidenceExamples: null,
    validFrom: null,
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
        blocks: [{ id: "core", name: "Основные", isMandatory: true }],
        competencies: [{ competencyId: DESIGN, blockId: "core" }],
        blockWeights: [
          {
            levelId: JUNIOR_LEVEL,
            blockId: "core",
            percent: 100,
            minPercent: null,
          },
        ],
        requirements: [requirement],
      },
      entries: {
        ...shown.entries,
        competencies: [
          {
            id: DESIGN,
            name: "Проектирование",
            description: null,
            categoryId: "category",
            categoryName: "Профессиональные",
            directionId: null,
            type: "Profile",
            versionNumber: 1,
            status: "Active",
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

    const cell = await screen.findByRole("button", {
      name: "Проектирование, Младший",
    });
    expect(
      within(cell).getByRole("img", { name: "Изменено согласующим" }),
    ).toBeInTheDocument();
    const row = cell.closest("tr") as HTMLElement;
    expect(row).toHaveClass("matrix-changed-row");
    expect(
      within(row).getAllByText("Изменено согласующим").length,
    ).toBeGreaterThan(0);
  });

  it("marks nothing, and asks for no difference, when the last variant is the author's", async () => {
    serve(returned());
    fakeApi.on(`GET /api/v1/career/matrix-versions/${VERSION_ID}/history`, () =>
      Response.json({ variants: [variant(1, "Author")], decisions: [] }),
    );
    await openPage();

    const cell = await screen.findByRole("button", {
      name: "Проектирование, Младший",
    });
    expect(screen.queryByText("Изменено согласующим")).not.toBeInTheDocument();
    expect(cell.closest("tr")).not.toHaveClass("matrix-changed-row");
    expect(
      fakeApi.calls(
        `GET /api/v1/career/matrix-versions/${VERSION_ID}/variants/1/diff`,
      ),
    ).toHaveLength(0);
  });
});

describe("editing the content of a draft version (AC-14, AC-15, AC-17)", () => {
  const CONTENT_ROUTE = `PUT /api/v1/career/matrix-versions/${VERSION_ID}/content`;

  const draft = () =>
    version({ status: "Draft", step: null, actions: ["Edit", "Submit"] });

  function serveEditor() {
    serve(draft());
    fakeApi.on("GET /api/v1/career/levels", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 200 }),
    );
    fakeApi.on("GET /api/v1/career/competencies", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 200 }),
    );
    fakeApi.on(CONTENT_ROUTE, async (request) =>
      Response.json({
        ...draft(),
        content: await request.json(),
        version: VERSION_TAG + 1,
      }),
    );
  }

  it("saves the threshold that was typed against the version the editor was opened with", async () => {
    serveEditor();
    const user = await openPage();

    const threshold = await screen.findByRole("spinbutton", {
      name: "Минимальный общий порог: Младший",
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

  it("adds a named block, mandatory or not, and saves it with the content", async () => {
    serveEditor();
    const user = await openPage();

    await user.click(
      await screen.findByRole("button", { name: "Добавить блок" }),
    );
    await user.type(await screen.findByLabelText("Название блока"), "Основные");
    await user.click(
      screen.getByRole("checkbox", { name: "Обязательный блок" }),
    );
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(fakeApi.calls(CONTENT_ROUTE)).toHaveLength(1));
    const saved = await fakeApi.calls(CONTENT_ROUTE)[0]?.json();
    expect(saved.blocks).toEqual([
      { id: expect.any(String), name: "Основные", isMandatory: true },
    ]);
  });

  it("offers no editing and no save for a version whose turn is not the user's", async () => {
    serve(version({ status: "OnReview", actions: [] }));
    await openPage();

    expect(
      screen.queryByRole("button", { name: "Сохранить" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Добавить блок" }),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole("spinbutton")).not.toBeInTheDocument();
  });
});
