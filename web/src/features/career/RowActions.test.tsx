import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { CategoriesTab } from "./CategoriesTab";
import { CompetenciesTab } from "./CompetenciesTab";
import { DirectionsTracksTab } from "./DirectionsTracksTab";
import { type RowAction, RowActions } from "./RowActions";
import { ScalesTab } from "./ScalesTab";

const entry = (fields: object = {}) => ({
  id: "entry",
  name: "Запись",
  status: "Active",
  version: 3,
  canEdit: true,
  canDelete: true,
  ...fields,
});

const page = (items: object[]) => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 20,
});

function serveRole(role: "GlobalAdmin" | "User") {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "a@test.local", role }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
}

function show(element: ReactElement) {
  render(
    <Providers>
      <MemoryRouter>{element}</MemoryRouter>
    </Providers>,
  );
  return userEvent.setup();
}

/** The visible actions of the row, then (after opening «…», if any) those in the menu. */
async function actionsOf(
  user: ReturnType<typeof userEvent.setup>,
  name: string,
) {
  const row = (await screen.findByText(name)).closest("tr") as HTMLElement;
  const visible = within(row)
    .queryAllByRole("button")
    .filter((button) => !button.getAttribute("aria-label"))
    .map((button) => button.textContent);
  const trigger = within(row).queryByRole("button", {
    name: `Действия с «${name}»`,
  });
  if (!trigger) return { visible, menu: [] as (string | null)[] };
  await user.click(trigger);
  const menu = (await screen.findAllByRole("menuitem")).map(
    (item) => item.textContent,
  );
  return { visible, menu };
}

/** The confirmation of a deletion: antd gives every modal of the test the same title id, so it is found by its text. */
async function askedToDelete() {
  const title = await screen.findByText("Удалить категорию «Черновая»?", {
    selector: ".ant-modal-confirm-title",
  });
  return title.closest('[role="dialog"]') as HTMLElement;
}

beforeEach(() => {
  queryClient.clear();
});

describe("RowActions: two visible, the rest behind «…» (AC-19)", () => {
  const action = (key: string, fields: Partial<RowAction> = {}): RowAction => ({
    key,
    label: key,
    onClick: vi.fn(),
    ...fields,
  });

  const visibleLabels = () =>
    screen
      .queryAllByRole("button")
      .filter((button) => !button.getAttribute("aria-label"))
      .map((button) => button.textContent);

  function showActions(actions: RowAction[]) {
    render(<RowActions actions={actions} menuLabel="Действия с «Х»" />);
    return userEvent.setup();
  }

  it.each([
    [[]],
    [[action("a")]],
    [[action("a"), action("b", { danger: true })]],
  ])(
    "shows up to two actions as they are, destructive too, with no «…» (case %#)",
    (actions) => {
      showActions(actions);

      expect(
        screen.queryByRole("button", { name: "Действия с «Х»" }),
      ).toBeNull();
      expect(visibleLabels()).toEqual(actions.map((a) => a.label));
      for (const { label, danger } of actions) {
        expect(
          screen
            .getByRole("button", { name: label })
            .classList.contains("ant-btn-dangerous"),
        ).toBe(danger === true);
      }
    },
  );

  it("with more, keeps the first two that are not destructive visible and puts the rest in «…» in their order", async () => {
    const user = showActions([
      action("a"),
      action("rm", { danger: true }),
      action("b"),
      action("c"),
      action("off", { danger: true }),
    ]);

    expect(visibleLabels()).toEqual(["a", "b"]);
    expect(screen.queryByRole("menuitem")).toBeNull();

    await user.click(screen.getByRole("button", { name: "Действия с «Х»" }));

    const items = await screen.findAllByRole("menuitem");
    expect(items.map((item) => item.textContent)).toEqual(["rm", "c", "off"]);
    expect(
      items.map((item) =>
        item.classList.contains("ant-dropdown-menu-item-danger"),
      ),
    ).toEqual([true, false, true]);
  });

  it("runs the action chosen in the menu, and not a disabled one", async () => {
    const run = vi.fn();
    const blocked = vi.fn();
    const user = showActions([
      action("a"),
      action("b"),
      action("blocked", { disabled: true, onClick: blocked }),
      action("run", { onClick: run }),
    ]);
    await user.click(screen.getByRole("button", { name: "Действия с «Х»" }));

    await user.click(await screen.findByRole("menuitem", { name: "blocked" }));
    expect(blocked).not.toHaveBeenCalled();
    await user.click(await screen.findByRole("menuitem", { name: "run" }));

    expect(run).toHaveBeenCalledOnce();
  });
});

describe("the row actions of the catalogues (AC-19, AC-25)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("a draft category: «Изменить» and «Согласовать» visible, «Деактивировать» and «Удалить» in the menu", async () => {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/categories", () =>
      Response.json(page([entry({ name: "Черновая", status: "Draft" })])),
    );
    const user = show(<CategoriesTab />);

    const { visible, menu } = await actionsOf(user, "Черновая");

    expect(visible).toEqual(["Изменить", "Согласовать"]);
    expect(menu).toEqual(["Деактивировать", "Удалить"]);
  });

  it("an inactive category: «Изменить» and «Вернуть в работу» visible, «Удалить» in the menu", async () => {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/categories", () =>
      Response.json(page([entry({ name: "Старая", status: "Inactive" })])),
    );
    const user = show(<CategoriesTab />);

    const { visible, menu } = await actionsOf(user, "Старая");

    expect(visible).toEqual(["Изменить", "Вернуть в работу"]);
    expect(menu).toEqual(["Удалить"]);
  });

  it("an active scale that is not the default: «Сделать по умолчанию» and «Изменить» visible", async () => {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/scales", () =>
      Response.json(
        page([entry({ name: "Шкала", points: [], isDefault: false })]),
      ),
    );
    const user = show(<ScalesTab />);

    const { visible, menu } = await actionsOf(user, "Шкала");

    expect(visible).toEqual(["Сделать по умолчанию", "Изменить"]);
    expect(menu).toEqual(["Деактивировать", "Удалить"]);
  });

  it("a draft competency: «История версий» and «Изменить» visible, «Согласовать» in the menu", async () => {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/competencies", () =>
      Response.json(
        page([
          entry({
            name: "Проектирование",
            status: "Draft",
            description: null,
            categoryId: "c",
            directionId: null,
            type: "Profile",
            scaleId: "s",
            createdAt: "2026-10-01T10:00:00Z",
            versionNumber: 1,
          }),
        ]),
      ),
    );
    const user = show(<CompetenciesTab />);

    const { visible, menu } = await actionsOf(user, "Проектирование");

    expect(visible).toEqual(["История версий", "Изменить"]);
    expect(menu).toEqual(["Согласовать", "Деактивировать", "Удалить"]);
  });

  it("deactivates from the menu against the version of the row", async () => {
    const route = "POST /api/v1/career/categories/entry/deactivate";
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/categories", () =>
      Response.json(page([entry({ name: "Черновая", status: "Draft" })])),
    );
    fakeApi.on(route, () => Response.json(entry()));
    const user = show(<CategoriesTab />);
    await actionsOf(user, "Черновая");

    await user.click(
      await screen.findByRole("menuitem", { name: "Деактивировать" }),
    );

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(fakeApi.calls(route)[0]?.headers.get("If-Match")).toBe('"3"');
  });

  async function openDeletionFromMenu(route: string) {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/categories", () =>
      Response.json(page([entry({ name: "Черновая", status: "Draft" })])),
    );
    fakeApi.on(route, () => new Response(null, { status: 204 }));
    const user = show(<CategoriesTab />);
    await actionsOf(user, "Черновая");
    await user.click(await screen.findByRole("menuitem", { name: "Удалить" }));
    return { user, asked: await askedToDelete() };
  }

  it("«Удалить» in the menu asks first, with the focus on «Отмена», and «Отмена» deletes nothing", async () => {
    const route = "DELETE /api/v1/career/categories/entry";
    const { user, asked } = await openDeletionFromMenu(route);

    expect(within(asked).getByRole("button", { name: "Отмена" })).toHaveFocus();
    await user.click(within(asked).getByRole("button", { name: "Отмена" }));

    expect(fakeApi.calls(route)).toHaveLength(0);
  });

  it("«Удалить» in the confirmation deletes against the version of the row", async () => {
    const route = "DELETE /api/v1/career/categories/entry";
    const { user, asked } = await openDeletionFromMenu(route);

    await user.click(within(asked).getByRole("button", { name: "Удалить" }));

    await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
    expect(fakeApi.calls(route)[0]?.headers.get("If-Match")).toBe('"3"');
  });

  it("offers no «…» to a user who may do nothing with the row", async () => {
    serveRole("User");
    fakeApi.on("GET /api/v1/career/categories", () =>
      Response.json(
        page([entry({ name: "Чужая", canEdit: false, canDelete: false })]),
      ),
    );
    const user = show(<CategoriesTab />);

    const { visible, menu } = await actionsOf(user, "Чужая");

    expect(visible).toEqual([]);
    expect(menu).toEqual([]);
  });
});

describe("the empty tables of the catalogues (AC-19)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("says «Шкал пока нет» for no scales", async () => {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/scales", () => Response.json(page([])));
    show(<ScalesTab />);

    expect(await screen.findByText("Шкал пока нет")).toBeVisible();
    expect(
      Array.from(document.querySelectorAll(".ant-empty-description")).map(
        (description) => description.textContent,
      ),
    ).toEqual(["Шкал пока нет"]);
  });

  it("says «Треков пока нет» in the tracks of a direction that has none", async () => {
    serveRole("GlobalAdmin");
    fakeApi.on("GET /api/v1/career/directions", () =>
      Response.json(page([entry({ name: "Разработка" })])),
    );
    fakeApi.on("GET /api/v1/career/tracks", () => Response.json(page([])));
    const user = show(<DirectionsTracksTab />);

    await user.click(
      await waitFor(() => {
        const expander = document.querySelector(".ant-table-row-expand-icon");
        expect(expander).not.toBeNull();
        return expander as HTMLElement;
      }),
    );

    expect(await screen.findByText("Треков пока нет")).toBeVisible();
    expect(
      Array.from(document.querySelectorAll(".ant-empty-description")).map(
        (description) => description.textContent,
      ),
    ).toEqual(["Треков пока нет"]);
  });
});
