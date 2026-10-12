import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { CategoriesTab } from "./CategoriesTab";
import { CompetenciesTab } from "./CompetenciesTab";
import { DirectionsTracksTab } from "./DirectionsTracksTab";
import { DraftsSection } from "./DraftsSection";
import { MatricesTab } from "./MatricesTab";
import { ScalesTab } from "./ScalesTab";

const TOTAL = 45;
const PAGE_SIZE = 20;
const FIRST_OF_SECOND_PAGE = `Запись ${PAGE_SIZE + 1}`;

const entry = (index: number) => ({
  id: `entry-${index}`,
  name: `Запись ${index}`,
  status: "Active",
  version: 1,
  canEdit: false,
  canDelete: false,
});

interface PagedTable {
  table: string;
  show: () => ReactElement;
  route: string;
  row: (index: number) => object;
  isNested?: boolean;
}

const TABLES: PagedTable[] = [
  {
    table: "directions",
    show: () => <DirectionsTracksTab />,
    route: "GET /api/v1/career/directions",
    row: entry,
  },
  {
    table: "tracks of a direction",
    show: () => <DirectionsTracksTab />,
    route: "GET /api/v1/career/tracks",
    row: (index) => ({
      ...entry(index),
      directionId: "direction",
      ownerOrgUnitId: "unit",
      ownerOrgUnitName: "Отдел",
    }),
    isNested: true,
  },
  {
    table: "categories",
    show: () => <CategoriesTab />,
    route: "GET /api/v1/career/categories",
    row: entry,
  },
  {
    table: "scales",
    show: () => <ScalesTab />,
    route: "GET /api/v1/career/scales",
    row: (index) => ({ ...entry(index), points: [], isDefault: false }),
  },
  {
    table: "competencies",
    show: () => <CompetenciesTab />,
    route: "GET /api/v1/career/competencies",
    row: (index) => ({
      ...entry(index),
      description: null,
      categoryId: "category",
      directionId: null,
      type: "Profile",
      scaleId: "scale",
      createdAt: "2026-10-01T10:00:00Z",
      versionNumber: 1,
    }),
  },
  {
    table: "matrices",
    show: () => <MatricesTab />,
    route: "GET /api/v1/career/matrices",
    row: (index) => ({
      id: `matrix-${index}`,
      orgUnitId: "unit",
      orgUnitName: "Отдел",
      directionId: "direction",
      directionName: "Разработка",
      trackId: "track",
      trackName: `Запись ${index}`,
      publishedVersion: null,
      inWorkVersion: null,
      createdAt: "2026-10-01T10:00:00Z",
    }),
  },
];

const queries = (route: string) =>
  fakeApi
    .calls(route)
    .map((call) => Object.fromEntries(new URL(call.url).searchParams));

function serveRole(role: "GlobalAdmin" | "User") {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "account", email: "who@test.local", role }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
}

const page = (items: object[]) => ({
  items,
  total: items.length,
  page: 1,
  pageSize: PAGE_SIZE,
});

function showTable(element: ReactElement) {
  render(
    <Providers>
      <MemoryRouter>{element}</MemoryRouter>
    </Providers>,
  );
  return userEvent.setup();
}

beforeEach(() => {
  queryClient.clear();
});

describe("a catalogue table pages on the server (AC-24)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it.each(TABLES)(
    "$table: 20 rows to a page, the next page asked from the server",
    async ({ show, route, row, isNested }) => {
      serveRole("GlobalAdmin");
      fakeApi.on("GET /api/v1/career/directions", () =>
        Response.json(page([{ ...entry(0), name: "Разработка" }])),
      );
      fakeApi.on(route, (request) => {
        const query = new URL(request.url).searchParams;
        const number = Number(query.get("page"));
        const size = Number(query.get("pageSize"));
        const items = Array.from({ length: TOTAL }, (_, index) =>
          row(index + 1),
        );
        return Response.json({
          items: items.slice((number - 1) * size, number * size),
          total: TOTAL,
          page: number,
          pageSize: size,
        });
      });
      const user = showTable(show());
      let scope: HTMLElement = document.body;
      if (isNested) {
        await user.click(
          await waitFor(() => {
            const expander = document.querySelector(
              ".ant-table-row-expand-icon",
            );
            expect(expander).not.toBeNull();
            return expander as HTMLElement;
          }),
        );
        await screen.findByText("Запись 1");
        scope = document.querySelector(
          ".ant-table-expanded-row .ant-table-wrapper",
        ) as HTMLElement;
      }
      expect(await within(scope).findByText("Запись 1")).toBeInTheDocument();
      expect(within(scope).queryByText(FIRST_OF_SECOND_PAGE)).toBeNull();
      expect(queries(route)[0]).toMatchObject({
        page: "1",
        pageSize: String(PAGE_SIZE),
      });

      await user.click(within(scope).getByTitle("2"));

      expect(
        await within(scope).findByText(FIRST_OF_SECOND_PAGE),
      ).toBeInTheDocument();
      expect(queries(route).at(-1)).toMatchObject({
        page: "2",
        pageSize: String(PAGE_SIZE),
      });
    },
  );
});

describe("the page size of a catalogue table (AC-24)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("is asked from the server as chosen, from the first page", async () => {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/categories", (request) => {
      const query = new URL(request.url).searchParams;
      return Response.json({
        items: Array.from({ length: PAGE_SIZE }, (_, index) =>
          entry(index + 1),
        ),
        total: TOTAL,
        page: Number(query.get("page")),
        pageSize: Number(query.get("pageSize")),
      });
    });
    const user = showTable(<CategoriesTab />);
    await screen.findByText("Запись 1");

    await user.click(screen.getByTitle("2"));
    await user.click(
      document.querySelector(
        ".ant-pagination-options .ant-select",
      ) as HTMLElement,
    );
    await user.click(
      await screen.findByText(/^50/, {
        selector: ".ant-select-item-option-content",
      }),
    );

    await waitFor(() =>
      expect(queries("GET /api/v1/career/categories").at(-1)).toMatchObject({
        page: "1",
        pageSize: "50",
      }),
    );
  });
});

describe("the actions of a catalogue row follow what the server says the user may do (AC-19, AC-25)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("offers «Изменить» and «Удалить» by canEdit and canDelete of each entry, whatever the roles of the user", async () => {
    serveRole("User");
    fakeApi.on("GET /api/v1/career/categories", () =>
      Response.json(
        page([
          { ...entry(1), name: "Своя", canEdit: true, canDelete: true },
          {
            ...entry(2),
            name: "Только удаление",
            canEdit: false,
            canDelete: true,
          },
          { ...entry(3), name: "Чужая", canEdit: false, canDelete: false },
        ]),
      ),
    );
    showTable(<CategoriesTab />);

    const actionsOf = async (name: string) => {
      const row = (await screen.findByText(name)).closest("tr") as HTMLElement;
      return within(row)
        .queryAllByRole("button")
        .map((button) => button.textContent);
    };

    expect(await actionsOf("Своя")).toEqual(["Изменить", "Удалить"]);
    expect(await actionsOf("Только удаление")).toEqual(["Удалить"]);
    expect(await actionsOf("Чужая")).toEqual([]);
  });
});

describe("an action taken from a list row carries the tag of the row the user saw (AC-17)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("deletes a matrix against the version in work that its row shows, and reads no version on the way", async () => {
    const route = "DELETE /api/v1/career/matrices/matrix";
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/matrices", () =>
      Response.json(
        page([
          {
            id: "matrix",
            orgUnitId: "unit",
            orgUnitName: "Отдел",
            directionId: "direction",
            directionName: "Разработка",
            trackId: "track",
            trackName: "Бэкенд",
            publishedVersion: null,
            inWorkVersion: {
              id: "version",
              number: 1,
              status: "Draft",
              version: 4,
            },
            createdAt: "2026-10-01T10:00:00Z",
          },
        ]),
      ),
    );
    fakeApi.on("GET /api/v1/career/matrix-versions/version", () =>
      Response.json({ id: "version", version: 6 }),
    );
    fakeApi.on(route, () => new Response(null, { status: 204 }));
    const user = showTable(<MatricesTab />);

    await user.click(await screen.findByRole("button", { name: "Удалить" }));
    await user.click(
      within(await screen.findByRole("dialog")).getByRole("button", {
        name: "Удалить",
      }),
    );

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(fakeApi.calls(route)[0]?.headers.get("If-Match")).toBe('"4"');
    expect(
      fakeApi.calls("GET /api/v1/career/matrix-versions/version"),
    ).toHaveLength(0);
  });

  it("approves a draft against the version its row shows, and reads no entry on the way", async () => {
    const route = "POST /api/v1/career/directions/draft/approve";
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/drafts", () =>
      Response.json([
        {
          kind: "Direction",
          id: "draft",
          name: "Новое направление",
          ownerOrgUnitId: null,
          ownerOrgUnitName: null,
          authorId: "author",
          authorName: "Автор",
          createdAt: "2026-10-01T10:00:00Z",
          version: 7,
        },
      ]),
    );
    fakeApi.on("GET /api/v1/career/directions/draft", () =>
      Response.json({ ...entry(1), id: "draft", version: 9 }),
    );
    fakeApi.on(route, () => Response.json({ ...entry(1), id: "draft" }));
    const user = showTable(<DraftsSection />);

    await user.click(
      await screen.findByRole("button", { name: "Согласовать" }),
    );

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(fakeApi.calls(route)[0]?.headers.get("If-Match")).toBe('"7"');
    expect(fakeApi.calls("GET /api/v1/career/directions/draft")).toHaveLength(
      0,
    );
  });
});

describe("the competency table and its version history (AC-12, AC-24)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  const competency = (id: string, directionId: string | null) => ({
    ...entry(1),
    id,
    name: `Проектирование ${id}`,
    description: "Описание",
    categoryId: "category",
    directionId,
    type: "Critical",
    scaleId: "scale",
    createdAt: "2026-10-01T10:00:00Z",
    versionNumber: 2,
    version: 3,
  });

  const named = (id: string, name: string) => ({ ...entry(1), id, name });

  const version = (number: number, fields: object = {}) => ({
    number,
    name: "Проектирование",
    description: null,
    categoryId: "category",
    directionId: null,
    type: "Profile",
    scaleId: "scale",
    status: "Active",
    changedById: null,
    changedByName: "Автор правки",
    changedAt: "2026-10-01T10:00:00Z",
    ...fields,
  });

  function serveCompetencies() {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/competencies", () =>
      Response.json(
        page([competency("first", "direction"), competency("second", null)]),
      ),
    );
    fakeApi.on("GET /api/v1/career/categories/category", () =>
      Response.json(named("category", "Профессиональные")),
    );
    fakeApi.on("GET /api/v1/career/categories/old-category", () =>
      Response.json(named("old-category", "Прежняя категория")),
    );
    fakeApi.on("GET /api/v1/career/directions/direction", () =>
      Response.json(named("direction", "Разработка")),
    );
    fakeApi.on("GET /api/v1/career/scales/scale", () =>
      Response.json(named("scale", "Базовая 0–4")),
    );
  }

  it("names the category, the direction and the scale of each row by reading each once, and calls a competency of no direction «Общая»", async () => {
    serveCompetencies();
    showTable(<CompetenciesTab />);

    const first = (await screen.findByText("Проектирование first")).closest(
      "tr",
    ) as HTMLElement;
    const second = (await screen.findByText("Проектирование second")).closest(
      "tr",
    ) as HTMLElement;

    expect(await within(first).findByText("Профессиональные")).toBeVisible();
    expect(within(first).getByText("Разработка")).toBeVisible();
    expect(within(first).getByText("Базовая 0–4")).toBeVisible();
    expect(within(second).getByText("Общая")).toBeVisible();
    expect(
      fakeApi.calls("GET /api/v1/career/categories/category"),
    ).toHaveLength(1);
  });

  it("shows the fields of a saved version read-only, the newest first, and those of the one chosen in «Версия»", async () => {
    serveCompetencies();
    fakeApi.on("GET /api/v1/career/competencies/first/versions", () =>
      Response.json([
        version(2, {
          name: "Проектирование систем",
          description: "Новое описание",
          directionId: "direction",
          type: "Critical",
        }),
        version(1, { categoryId: "old-category", status: "Draft" }),
      ]),
    );
    const user = showTable(<CompetenciesTab />);

    await user.click(
      (
        await screen.findAllByRole("button", { name: "История версий" })
      )[0] as HTMLElement,
    );
    const dialog = (
      await screen.findAllByText("История версий", {
        selector: ".ant-modal-title",
      })
    )[0]?.closest('[role="dialog"]') as HTMLElement;

    expect(
      await within(dialog).findByText("Проектирование систем"),
    ).toBeInTheDocument();
    expect(within(dialog).getByText("Новое описание")).toBeInTheDocument();
    expect(
      await within(dialog).findByText("Профессиональные"),
    ).toBeInTheDocument();
    expect(within(dialog).getByText("Разработка")).toBeInTheDocument();
    expect(within(dialog).getByText("Критическая")).toBeInTheDocument();
    expect(within(dialog).getByText("Базовая 0–4")).toBeInTheDocument();
    expect(within(dialog).queryByRole("textbox")).toBeNull();

    await user.click(within(dialog).getByRole("combobox", { name: "Версия" }));
    await user.click(
      await screen.findByText(/^Версия 1 — /, {
        selector: ".ant-select-item-option-content",
      }),
    );

    expect(
      await within(dialog).findByText("Прежняя категория"),
    ).toBeInTheDocument();
    expect(within(dialog).getByText("Общая")).toBeInTheDocument();
    expect(within(dialog).getByText("Профильная")).toBeInTheDocument();
    expect(within(dialog).getByText("Черновик")).toBeInTheDocument();
    expect(within(dialog).queryByText("Новое описание")).toBeNull();
  });
});
