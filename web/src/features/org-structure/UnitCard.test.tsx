import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import type { OrgUnitTreeNode } from "./orgStructureApi";
import { UnitCard } from "./UnitCard";

const UNIT: OrgUnitTreeNode = {
  id: "sales",
  name: "Продажи",
  parentId: null,
  headEmployeeId: null,
  isActive: true,
  version: 1,
  headName: null,
  employeeCount: 3,
};

const person = (name: string, canAct?: boolean) => ({
  employeeId: name,
  name,
  orgUnitId: "sales",
  orgUnitName: "Продажи",
  ...(canAct === undefined
    ? {}
    : {
        account: {
          id: name,
          email: `${name}@test.local`,
          state: canAct ? "Active" : "Blocked",
          version: 1,
        },
      }),
});

function show(hr: { assigned: unknown; inherited: unknown }, isAdmin = false) {
  fakeApi.on("GET /api/v1/career/org-units/sales/hr", () => Response.json(hr));
  const onAction = vi.fn();
  render(
    <Providers>
      <UnitCard
        unit={UNIT}
        childCount={0}
        openness="closed"
        isSelected={false}
        needle=""
        canEdit={isAdmin}
        onToggle={vi.fn()}
        onAction={onAction}
      />
    </Providers>,
  );
  return { onAction, user: userEvent.setup() };
}

async function openMenu(user: ReturnType<typeof userEvent.setup>) {
  await user.click(
    screen.getByRole("button", { name: "Действия с подразделением Продажи" }),
  );
}

beforeEach(() => {
  queryClient.clear();
});

describe("unit card (AC-2, AC-8, AC-19)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("names the HR who answers for the unit, the unit's own before the inherited one", async () => {
    show({
      assigned: person("Иванова Анна"),
      inherited: person("Петров Пётр"),
    });

    expect(await screen.findByText("HR: Иванова Анна")).toBeInTheDocument();
    expect(screen.queryByText(/Петров/)).not.toBeInTheDocument();
  });

  it("names the inherited HR when the unit has none of its own", async () => {
    show({ assigned: null, inherited: person("Петров Пётр") });

    expect(await screen.findByText("HR: Петров Пётр")).toBeInTheDocument();
  });

  it("shows no HR line when nobody answers, also when the assigned HR cannot act", async () => {
    show({
      assigned: person("Заблокированный", false),
      inherited: person("Петров Пётр", true),
    });

    await waitFor(() =>
      expect(
        fakeApi.calls("GET /api/v1/career/org-units/sales/hr"),
      ).toHaveLength(1),
    );
    await screen.findByText("Руководитель: не назначен");
    expect(screen.queryByText(/^HR:/)).not.toBeInTheDocument();
  });

  it("offers everyone the positions and the HR of the unit, and nothing else", async () => {
    const { onAction, user } = show({ assigned: null, inherited: null });

    await openMenu(user);
    const items = (await screen.findAllByRole("menuitem")).map(
      (item) => item.textContent,
    );
    await user.click(screen.getByRole("menuitem", { name: /Должности/ }));

    expect(items).toEqual(["Должности", "HR"]);
    expect(onAction).toHaveBeenCalledWith("positions", UNIT);
  });

  it("offers an administrator the other actions too, with the positions and the HR among them", async () => {
    const { onAction, user } = show({ assigned: null, inherited: null }, true);

    await openMenu(user);
    const items = (await screen.findAllByRole("menuitem")).map((item) =>
      item.textContent?.replace(/^\S+\s/, ""),
    );
    await user.click(screen.getByRole("menuitem", { name: /HR/ }));

    expect(items).toEqual(
      expect.arrayContaining(["Должности", "HR", "Изменить", "Деактивировать"]),
    );
    expect(onAction).toHaveBeenCalledWith("hr", UNIT);
  });

  it("offers an administrator «Удалить» and passes the unit to the action (AC-33)", async () => {
    const { onAction, user } = show({ assigned: null, inherited: null }, true);

    await openMenu(user);
    await user.click(await screen.findByRole("menuitem", { name: /Удалить/ }));

    expect(onAction).toHaveBeenCalledWith("delete", UNIT);
  });

  it("does not offer «Удалить» to a user who cannot change the structure (AC-33)", async () => {
    const { user } = show({ assigned: null, inherited: null }, false);

    await openMenu(user);
    await screen.findAllByRole("menuitem");

    expect(
      screen.queryByRole("menuitem", { name: /Удалить/ }),
    ).not.toBeInTheDocument();
  });
});
