import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { PositionsPage } from "./PositionsPage";

const LIST = "GET /api/v1/positions";

function position(id: string, name: string, isActive = true, version = 3) {
  return { id, name, isActive, version };
}

const ENGINEER = position("engineer", "Инженер");
const OLD = position("old", "Кладовщик", false, 5);

function serveList(items = [ENGINEER, OLD]) {
  fakeApi.on(LIST, (request) => {
    const query = new URL(request.url).searchParams;
    const isActive = query.get("isActive");
    const shown = items.filter(
      (item) => isActive === null || String(item.isActive) === isActive,
    );
    return Response.json({
      items: shown,
      total: shown.length,
      page: 1,
      pageSize: 20,
    });
  });
}

const queries = () =>
  fakeApi
    .calls(LIST)
    .map((call) => Object.fromEntries(new URL(call.url).searchParams));

/** The dialog with the given title; the dialogs of the test environment share one label id, so they are found by their title. */
async function dialogTitled(title: string) {
  const [heading] = await screen.findAllByText(title, {
    selector: ".ant-modal-title, .ant-modal-confirm-title",
  });
  return heading?.closest('[role="dialog"]') as HTMLElement;
}

async function openPage() {
  render(
    <Providers>
      <PositionsPage />
    </Providers>,
  );
  return userEvent.setup();
}

async function chooseStatus(
  user: ReturnType<typeof userEvent.setup>,
  label: string,
) {
  await user.click(
    document.querySelector(".ant-table-filter-trigger") as Element,
  );
  await user.click(await screen.findByRole("menuitem", { name: label }));
  await user.click(screen.getByRole("button", { name: "OK" }));
}

beforeEach(() => {
  queryClient.clear();
});

describe("position catalogue page (AC-1, AC-24)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("starts with the active positions, names the columns and tags each position's status in words", async () => {
    serveList();
    await openPage();

    expect(await screen.findByText("Инженер")).toBeInTheDocument();
    expect(queries()[0]).toMatchObject({
      isActive: "true",
      page: "1",
      pageSize: "20",
    });
    expect(
      screen.getByRole("heading", { name: "Должности" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("columnheader", { name: /Название/ }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("columnheader", { name: /Статус/ }),
    ).toBeInTheDocument();
    expect(screen.getByText("Активна")).toBeInTheDocument();
    expect(screen.queryByText("Кладовщик")).not.toBeInTheDocument();
  });

  it("asks the server for the status chosen in the column filter, which is the only status control on the page", async () => {
    serveList();
    const user = await openPage();
    await screen.findByText("Инженер");
    expect(
      screen.queryByRole("menuitem", { name: "Все" }),
    ).not.toBeInTheDocument();

    await chooseStatus(user, "Неактивна");

    expect(await screen.findByText("Кладовщик")).toBeInTheDocument();
    await waitFor(() =>
      expect(queries().at(-1)).toMatchObject({ isActive: "false", page: "1" }),
    );
    expect(
      screen.getByText("Неактивна", { selector: ".ant-tag" }),
    ).toBeInTheDocument();
  });

  it("searches the catalogue by the typed text and goes back to the first page", async () => {
    serveList();
    const user = await openPage();
    await screen.findByText("Инженер");

    await user.type(
      screen.getByRole("searchbox", { name: "Найти должность" }),
      "  инж  {Enter}",
    );

    await waitFor(() =>
      expect(queries().at(-1)).toMatchObject({
        q: "инж",
        page: "1",
        isActive: "true",
      }),
    );
  });

  it("says «Должностей пока нет» for an empty catalogue", async () => {
    serveList([]);
    await openPage();

    expect(await screen.findByText("Должностей пока нет")).toBeInTheDocument();
  });

  it("creates a position from «Добавить должность» and shows a taken name under the field", async () => {
    serveList();
    fakeApi.on("POST /api/v1/positions", () =>
      problem(409, {
        errors: { name: ["Должность с таким названием уже есть"] },
      }),
    );
    const user = await openPage();
    await screen.findByText("Инженер");

    await user.click(
      screen.getByRole("button", { name: "Добавить должность" }),
    );
    const dialog = await dialogTitled("Новая должность");
    await user.type(within(dialog).getByLabelText("Название"), "Инженер");
    await user.click(within(dialog).getByRole("button", { name: "Сохранить" }));

    expect(
      await within(dialog).findByText("Должность с таким названием уже есть"),
    ).toBeInTheDocument();
    expect(await fakeApi.calls("POST /api/v1/positions")[0]?.json()).toEqual({
      name: "Инженер",
    });
  });

  it("checks the name before sending it: required, and no longer than 200 characters", async () => {
    serveList();
    const user = await openPage();
    await screen.findByText("Инженер");
    await user.click(
      screen.getByRole("button", { name: "Добавить должность" }),
    );
    const dialog = await dialogTitled("Новая должность");

    await user.click(within(dialog).getByRole("button", { name: "Сохранить" }));
    expect(
      await within(dialog).findByText("Укажите название"),
    ).toBeInTheDocument();
    await user.click(within(dialog).getByLabelText("Название"));
    await user.paste("я".repeat(201));

    expect(
      await within(dialog).findByText("Не длиннее 200 символов"),
    ).toBeInTheDocument();
    expect(fakeApi.calls("POST /api/v1/positions")).toHaveLength(0);
  });

  it("renames a position against the version the list showed", async () => {
    serveList();
    fakeApi.on("PUT /api/v1/positions/engineer", () =>
      Response.json(position("engineer", "Ведущий инженер", true, 4)),
    );
    const user = await openPage();
    await screen.findByText("Инженер");

    await user.click(screen.getByRole("button", { name: "Переименовать" }));
    const dialog = await dialogTitled("Переименование должности");
    const name = within(dialog).getByLabelText("Название");
    expect(name).toHaveValue("Инженер");
    await user.clear(name);
    await user.type(name, "Ведущий инженер");
    await user.click(within(dialog).getByRole("button", { name: "Сохранить" }));

    await waitFor(() =>
      expect(fakeApi.calls("PUT /api/v1/positions/engineer")).toHaveLength(1),
    );
    const call = fakeApi.calls("PUT /api/v1/positions/engineer")[0];
    expect(call?.headers.get("If-Match")).toBe('"3"');
    expect(await call?.json()).toEqual({ name: "Ведущий инженер" });
  });

  it("shows how many working employees keep a position before deactivating it, focuses «Отмена» and deactivates on confirmation", async () => {
    serveList();
    fakeApi.on("GET /api/v1/positions/engineer/impact", () =>
      Response.json({ employeeCount: 7 }),
    );
    fakeApi.on("POST /api/v1/positions/engineer/deactivate", () =>
      Response.json(position("engineer", "Инженер", false, 4)),
    );
    const user = await openPage();
    await screen.findByText("Инженер");

    await user.click(screen.getByRole("button", { name: "Деактивировать" }));

    const dialog = await dialogTitled("Деактивировать должность?");
    expect(
      within(dialog).getByText(
        "Сотрудники, которые её занимают (7), сохранят должность. Выбрать её для других сотрудников будет нельзя.",
      ),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByRole("button", { name: "Отмена" }),
    ).toHaveFocus();
    await user.click(
      within(dialog).getByRole("button", { name: "Деактивировать" }),
    );
    await waitFor(() =>
      expect(
        fakeApi.calls("POST /api/v1/positions/engineer/deactivate"),
      ).toHaveLength(1),
    );
    expect(
      fakeApi
        .calls("POST /api/v1/positions/engineer/deactivate")[0]
        ?.headers.get("If-Match"),
    ).toBe('"3"');
  });

  it("brings an inactive position back to work without asking", async () => {
    serveList();
    fakeApi.on("POST /api/v1/positions/old/activate", () =>
      Response.json(position("old", "Кладовщик", true, 6)),
    );
    const user = await openPage();
    await screen.findByText("Инженер");
    await chooseStatus(user, "Неактивна");
    await screen.findByText("Кладовщик");

    await user.click(screen.getByRole("button", { name: "Вернуть в работу" }));

    await waitFor(() =>
      expect(fakeApi.calls("POST /api/v1/positions/old/activate")).toHaveLength(
        1,
      ),
    );
    expect(
      fakeApi
        .calls("POST /api/v1/positions/old/activate")[0]
        ?.headers.get("If-Match"),
    ).toBe('"5"');
  });
});
