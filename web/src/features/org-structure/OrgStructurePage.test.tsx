import { focusManager } from "@tanstack/react-query";
import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { SEARCH_DEBOUNCE_MS } from "../../app/useDebouncedValue";
import { deferred, fakeApi, json } from "../../test/fakeApi";
import { OrgStructurePage } from "./OrgStructurePage";
import { employeesQueryKey } from "./orgStructureApi";
import { SEARCH_MIN_LENGTH } from "./useEmployeeSearch";

const SEARCH_LABEL = "Поиск по подразделениям и сотрудникам";
const EMPLOYEES_ROUTE = "GET /api/v1/employees";

function unit(id: string, name: string, parentId: string | null) {
  return {
    id,
    name,
    parentId,
    headEmployeeId: null,
    isActive: true,
    version: 1,
    headName: null,
    employeeCount: 0,
  };
}

const UNITS = [
  unit("company", "Компания", null),
  unit("sales", "Продажи", "company"),
  unit("north", "Север", "sales"),
  unit("support", "Поддержка", "company"),
];

function employee(id: string, fullName: string, orgUnitId: string) {
  return {
    id,
    lastName: fullName,
    firstName: "Иван",
    middleName: null,
    fullName,
    email: null,
    orgUnitId,
    orgUnitName: "",
    position: "Менеджер по продажам",
  };
}

const MATCHED = employee("matched", "Найденов", "north");
const OF_SUPPORT = employee("of-support", "Помогаев", "support");

const page = (items: unknown[]) =>
  json({ items, total: items.length, page: 1, pageSize: 200 });

const hasParam = (request: Request, name: string) =>
  new URL(request.url).searchParams.has(name);

const unitListCalls = () =>
  fakeApi.calls(EMPLOYEES_ROUTE).filter((call) => hasParam(call, "orgUnitId"));

const searchCalls = () =>
  fakeApi.calls(EMPLOYEES_ROUTE).filter((call) => hasParam(call, "q"));

function serve(searchAnswer: () => Response | Promise<Response>) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    json({ id: "account", email: "user@test.local", role: "User" }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () => json(UNITS));
  fakeApi.on(EMPLOYEES_ROUTE, (request) =>
    hasParam(request, "orgUnitId") ? page([OF_SUPPORT]) : searchAnswer(),
  );
}

async function openPage() {
  render(
    <Providers>
      <MemoryRouter>
        <OrgStructurePage />
      </MemoryRouter>
    </Providers>,
  );
  return screen.findByLabelText(SEARCH_LABEL);
}

async function searchFor(text: string) {
  const input = await openPage();
  const user = userEvent.setup();
  await user.type(input, text);
  return user;
}

beforeEach(() => {
  queryClient.clear();
});

afterEach(() => {
  vi.useRealTimers();
});

describe("search over the hierarchy (AC-17)", () => {
  it("opens a unit that matches by name and has matched employees below only to its path, without reading all its employees", async () => {
    serve(() => page([MATCHED]));

    await searchFor("продаж");

    expect(await screen.findByText(MATCHED.fullName)).toBeInTheDocument();
    expect(searchCalls()).toHaveLength(1);
    expect(unitListCalls()).toHaveLength(0);
  });

  it("reads the full employee list of a matched unit only when the user opens its card", async () => {
    serve(() => page([]));
    const user = await searchFor("поддерж");

    await user.click(await screen.findByRole("button", { name: "Поддержка" }));

    expect(await screen.findByText(OF_SUPPORT.fullName)).toBeInTheDocument();
    expect(unitListCalls().map((call) => new URL(call.url).search)).toEqual([
      expect.stringContaining("orgUnitId=support"),
    ]);
  });

  it("keeps a card the user opened while the search was still running open when the late answer turns it into a path unit", async () => {
    const answer = deferred<Response>();
    serve(() => answer.promise);
    const user = await searchFor("продаж");
    await waitFor(() => expect(searchCalls()).toHaveLength(1));
    const sales = await screen.findByRole("button", { name: "Продажи" });
    expect(sales).toHaveAttribute("aria-expanded", "false");

    await user.click(sales);
    answer.resolve(page([MATCHED]));

    expect(await screen.findByText(MATCHED.fullName)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Продажи" })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
  });

  it("lets the user close by hand a unit the search opened to its path, and the unit's matches go with it", async () => {
    serve(() => page([MATCHED]));
    const user = await searchFor("продаж");
    await screen.findByText(MATCHED.fullName);

    await user.click(screen.getByRole("button", { name: "Продажи" }));

    await waitFor(() =>
      expect(screen.queryByText(MATCHED.fullName)).not.toBeInTheDocument(),
    );
    expect(screen.getByRole("button", { name: "Продажи" })).toHaveAttribute(
      "aria-expanded",
      "false",
    );
  });

  it("explains the two-character minimum instead of «nothing found» for a one-character text no unit matches, and sends nothing to the server", async () => {
    serve(() => page([MATCHED]));
    const input = await openPage();
    vi.useFakeTimers();

    fireEvent.change(input, { target: { value: "ф" } });
    await act(() => vi.advanceTimersByTimeAsync(SEARCH_DEBOUNCE_MS));

    expect(
      screen.getByText(new RegExp(`не менее ${SEARCH_MIN_LENGTH} символов`)),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Ничего не найдено/)).not.toBeInTheDocument();
    expect(searchCalls()).toHaveLength(0);
    expect(queryClient.isFetching({ queryKey: employeesQueryKey })).toBe(0);
  });

  it("still says «nothing found» for a text of the minimum length that nothing matches", async () => {
    serve(() => page([]));

    await searchFor("фф");

    expect(await screen.findByText(/Ничего не найдено/)).toBeInTheDocument();
    expect(screen.queryByText(/символов/)).not.toBeInTheDocument();
  });

  it("does not reread the hierarchy, the search or the open units when the user returns to the browser tab", async () => {
    serve(() => page([]));
    const user = await searchFor("поддерж");
    await user.click(await screen.findByRole("button", { name: "Поддержка" }));
    await screen.findByText(OF_SUPPORT.fullName);
    await waitFor(() => expect(searchCalls()).toHaveLength(1));
    await waitFor(() => expect(queryClient.isFetching()).toBe(0));

    await act(async () => {
      focusManager.setFocused(false);
      focusManager.setFocused(true);
    });

    await waitFor(() =>
      expect(
        fakeApi.calls("GET /api/v1/auth/me"),
        "the focus event must reach the cache: the account is reread on return to the tab",
      ).toHaveLength(2),
    );
    expect(fakeApi.calls("GET /api/v1/org-units/tree")).toHaveLength(1);
    expect(searchCalls()).toHaveLength(1);
    expect(unitListCalls()).toHaveLength(1);
  });
});
