import { act, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { currentUserQueryKey } from "../auth/useCurrentUser";
import { CareerPage } from "./CareerPage";
import { careerQueryKey } from "./careerApi";

const UNIT = { id: "unit", name: "Отдел" };
const REFERENCE_TABS = [
  "Шкалы",
  "Области и треки",
  "Компетенции",
  "Общие блоки",
  "Матрицы",
];
const APPROVALS_TAB = "На согласовании";

async function openAs(
  role: "GlobalAdmin" | "User",
  roles: { headedUnits?: object[]; hrUnits?: object[] } = {},
) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({
      id: "account",
      email: "user@test.local",
      roles: role === "User" ? [] : [role],
    }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [], ...roles }),
  );
  fakeApi.on("GET /api/v1/career/scales", () =>
    Response.json({ items: [], total: 0, page: 1, pageSize: 200 }),
  );
  render(
    <Providers>
      <CareerPage />
    </Providers>,
  );
  await screen.findByRole("heading", { name: "Карьерная модель" });
  await waitFor(() => {
    expect(queryClient.getQueryData(currentUserQueryKey)).toBeDefined();
    expect(queryClient.getQueryData([...careerQueryKey, "me"])).toBeDefined();
  });
  await act(async () => undefined);
}

const tabNames = () =>
  screen.getAllByRole("tab").map((tab) => tab.textContent?.trim());

beforeEach(() => {
  queryClient.clear();
});

describe("tabs of the career model (AC-24, AC-19)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("run in the order the records are created in, the approvals last, for a global administrator", async () => {
    await openAs("GlobalAdmin");

    expect(tabNames()).toEqual([...REFERENCE_TABS, APPROVALS_TAB]);
  });

  it("show the approvals to the head of a unit", async () => {
    await openAs("User", { headedUnits: [UNIT] });

    expect(tabNames()).toEqual([...REFERENCE_TABS, APPROVALS_TAB]);
  });

  it("show the approvals to the HR of a unit", async () => {
    await openAs("User", { hrUnits: [UNIT] });

    expect(tabNames()).toEqual([...REFERENCE_TABS, APPROVALS_TAB]);
  });

  it("keep the approvals from a user with no process role, who still reads the reference tabs", async () => {
    await openAs("User");

    expect(tabNames()).toEqual(REFERENCE_TABS);
  });
});
