import { render, screen, within } from "@testing-library/react";
import { RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../api/queryClient";
import { paths } from "../features/org-structure/paths";
import { fakeApi } from "../test/fakeApi";
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

async function openAs(role: "GlobalAdmin" | "User") {
  fakeApi.on("GET /api/v1/auth/me", () => Response.json({ ...ACCOUNT, role }));
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  await router.navigate(paths.tree);
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
  it("offers an administrator the structure and the career model first, then one «Администрирование» entry for users, positions and the journal", async () => {
    const menu = await openAs("GlobalAdmin");

    expect(topLevelItems(menu)).toEqual([
      "Оргструктура",
      "Карьерная модель",
      "Администрирование",
    ]);
  });

  it("offers an ordinary user only the structure and the career model, with no «Администрирование»", async () => {
    const menu = await openAs("User");

    expect(topLevelItems(menu)).toEqual(["Оргструктура", "Карьерная модель"]);
    expect(screen.queryByText("Администрирование")).not.toBeInTheDocument();
  });
});

describe("navigation entries by role (AC-19)", () => {
  it("lists the administrator group only for a global administrator, in the order of the menu", () => {
    const labels = (role: "GlobalAdmin" | "User") =>
      navItemsFor(role).map((item) => `${item.group ?? "top"}:${item.label}`);

    expect(labels("GlobalAdmin")).toEqual([
      "top:Оргструктура",
      "top:Карьерная модель",
      "admin:Пользователи",
      "admin:Должности",
      "admin:Журнал",
    ]);
    expect(labels("User")).toEqual([
      "top:Оргструктура",
      "top:Карьерная модель",
    ]);
  });
});
