import { render, screen, waitFor, within } from "@testing-library/react";
import { RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../api/queryClient";
import { paths } from "../features/org-structure/paths";
import { fakeApi } from "../test/fakeApi";
import type { UserRole } from "./featureContract";
import { navItemsFor } from "./features";
import { Providers } from "./Providers";
import { router } from "./router";

const ACCOUNT = {
  id: "account",
  email: "user@test.local",
  employeeId: null,
  employeeName: null,
  employeeLastName: null,
  employeeFirstName: null,
  employeeMiddleName: null,
};

async function openAs(roles: UserRole[], path: string = paths.tree) {
  fakeApi.on("GET /api/v1/auth/me", () => Response.json({ ...ACCOUNT, roles }));
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  await router.navigate(path);
  render(
    <Providers>
      <RouterProvider router={router} />
    </Providers>,
  );
  return screen.findByRole("menu");
}

function topLevelItems(menu: HTMLElement) {
  return within(menu)
    .getAllByRole("menuitem")
    .map((item) => item.textContent)
    .filter((text) => text);
}

beforeEach(() => {
  queryClient.clear();
});

describe("main menu (AC-19)", () => {
  it("offers an administrator the desk and «Справочники» first, then one «Администрирование» entry for users, positions and the journal", async () => {
    const menu = await openAs(["GlobalAdmin"]);

    expect(topLevelItems(menu)).toEqual([
      "Рабочий стол",
      "Справочники",
      "Администрирование",
    ]);
  });

  it("offers an ordinary user only the desk and «Справочники», with no «Администрирование»", async () => {
    const menu = await openAs([]);

    expect(topLevelItems(menu)).toEqual(["Рабочий стол", "Справочники"]);
    expect(screen.queryByText("Администрирование")).not.toBeInTheDocument();
  });
});

describe("navigation entries by role (AC-19)", () => {
  it("lists the administrator group only for a global administrator, in the order of the menu", () => {
    const labels = (roles: UserRole[]) =>
      navItemsFor(roles).map((item) => `${item.group ?? "top"}:${item.label}`);

    expect(labels(["GlobalAdmin"])).toEqual([
      "top:Рабочий стол",
      "reference:Оргструктура",
      "reference:Карьерная модель",
      "admin:Пользователи",
      "admin:Должности",
      "admin:Журнал",
    ]);
    expect(labels([])).toEqual([
      "top:Рабочий стол",
      "reference:Оргструктура",
      "reference:Карьерная модель",
    ]);
    expect(labels(["Hr"])).toContain("reference:Должности");
    expect(
      labels(["Hr", "GlobalAdmin"]).filter((l) => l.endsWith(":Должности")),
    ).toEqual(["admin:Должности"]);
  });
});

describe("the position catalogue address (AC-1, AC-19)", () => {
  it("opens the catalogue for a global administrator", async () => {
    fakeApi.on("GET /api/v1/positions", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 20 }),
    );

    await openAs(["GlobalAdmin"], "/positions");

    expect(
      await screen.findByRole("heading", { name: "Должности" }),
    ).toBeInTheDocument();
  });

  it("opens the catalogue for an HR account and asks for the positions", async () => {
    fakeApi.on("GET /api/v1/positions", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 20 }),
    );

    await openAs(["Hr"], "/positions");

    expect(
      await screen.findByRole("heading", { name: "Должности" }),
    ).toBeInTheDocument();
    await waitFor(() =>
      expect(fakeApi.calls("GET /api/v1/positions")).toHaveLength(1),
    );
  });

  it("gives an ordinary user the not-found page and requests no positions", async () => {
    await openAs([], "/positions");

    expect(
      await screen.findByRole("heading", { name: "Страница не найдена" }),
    ).toBeInTheDocument();
    expect(fakeApi.calls("GET /api/v1/positions")).toHaveLength(0);
  });
});
