import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import type { OrgUnit } from "./orgStructureApi";
import { UnitHrModal } from "./UnitHrModal";

const UNIT: OrgUnit = {
  id: "sales",
  name: "Продажи",
  parentId: "company",
  headEmployeeId: null,
  isActive: true,
  version: 1,
};

const person = (
  userId: string,
  name: string,
  unit: string,
  unitName: string,
) => ({
  userId,
  name,
  orgUnitId: unit,
  orgUnitName: unitName,
});

function serve(
  role: "GlobalAdmin" | "User",
  hr: { assigned: unknown; inherited: unknown },
) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "a@test.local", role }),
  );
  fakeApi.on("GET /api/v1/career/org-units/sales/hr", () => Response.json(hr));
  fakeApi.on("GET /api/v1/users", () =>
    Response.json({
      items: [
        { id: "u1", email: "ivanova@test.local", employeeName: "Иванова Анна" },
        { id: "u2", email: "petrov@test.local", employeeName: null },
      ],
      total: 2,
      page: 1,
      pageSize: 20,
    }),
  );
}

async function openModal() {
  render(
    <Providers>
      <UnitHrModal unit={UNIT} onClose={() => undefined} />
    </Providers>,
  );
  return userEvent.setup();
}

async function dialogTitled(title: string) {
  const [heading] = await screen.findAllByText(title, {
    selector: ".ant-modal-title, .ant-modal-confirm-title",
  });
  return heading?.closest('[role="dialog"]') as HTMLElement;
}

beforeEach(() => {
  queryClient.clear();
});

describe("HR of a unit (AC-8)", () => {
  it("shows the assigned HR and the one inherited from above", async () => {
    serve("User", {
      assigned: person("u1", "Иванова Анна", "sales", "Продажи"),
      inherited: person("u2", "Петров Пётр", "company", "Компания"),
    });
    await openModal();

    const dialog = await dialogTitled("HR подразделения «Продажи»");
    expect(
      await within(dialog).findByText("Закреплён: Иванова Анна"),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByText("Унаследован из «Компания»: Петров Пётр"),
    ).toBeInTheDocument();
  });

  it("says «HR не назначен» for a unit with neither", async () => {
    serve("User", { assigned: null, inherited: null });
    await openModal();

    expect(await screen.findByText("HR не назначен")).toBeInTheDocument();
  });

  it("leaves assigning and removing to administrators", async () => {
    serve("User", {
      assigned: person("u1", "Иванова Анна", "sales", "Продажи"),
      inherited: null,
    });
    await openModal();

    await screen.findByText("Закреплён: Иванова Анна");
    expect(
      screen.queryByRole("button", { name: "Закрепить" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Снять закрепление" }),
    ).not.toBeInTheDocument();
  });

  it("assigns the chosen account, offering accounts by employee name and e-mail", async () => {
    serve("GlobalAdmin", { assigned: null, inherited: null });
    fakeApi.on(
      "PUT /api/v1/career/org-units/sales/hr",
      () => new Response(null, { status: 204 }),
    );
    const user = await openModal();
    await screen.findByText("HR не назначен");

    await user.click(screen.getByRole("combobox", { name: "Пользователь" }));
    expect(await screen.findByText("petrov@test.local")).toBeInTheDocument();
    await user.click(screen.getByText("Иванова Анна (ivanova@test.local)"));
    await user.click(screen.getByRole("button", { name: "Закрепить" }));

    await waitFor(() =>
      expect(
        fakeApi.calls("PUT /api/v1/career/org-units/sales/hr"),
      ).toHaveLength(1),
    );
    expect(
      await fakeApi.calls("PUT /api/v1/career/org-units/sales/hr")[0]?.json(),
    ).toEqual({
      userId: "u1",
    });
  });

  it("shows the refusal of an account that cannot act under the field", async () => {
    serve("GlobalAdmin", { assigned: null, inherited: null });
    fakeApi.on("PUT /api/v1/career/org-units/sales/hr", () =>
      problem(400, {
        errors: {
          userId: ["Учётная запись заблокирована или не завершила регистрацию"],
        },
      }),
    );
    const user = await openModal();
    await screen.findByText("HR не назначен");

    await user.click(screen.getByRole("combobox", { name: "Пользователь" }));
    await user.click(await screen.findByText("petrov@test.local"));
    await user.click(screen.getByRole("button", { name: "Закрепить" }));

    expect(
      await screen.findByText(
        "Учётная запись заблокирована или не завершила регистрацию",
      ),
    ).toBeInTheDocument();
  });

  it("removes the assignment after a confirmation that names what follows, focused on «Отмена»", async () => {
    serve("GlobalAdmin", {
      assigned: person("u1", "Иванова Анна", "sales", "Продажи"),
      inherited: null,
    });
    fakeApi.on(
      "DELETE /api/v1/career/org-units/sales/hr",
      () => new Response(null, { status: 204 }),
    );
    const user = await openModal();
    await screen.findByText("Закреплён: Иванова Анна");

    await user.click(screen.getByRole("button", { name: "Снять закрепление" }));

    const confirm = await dialogTitled("Снять закрепление HR?");
    expect(
      within(confirm).getByText(
        "Ответственным станет HR вышестоящего подразделения, если он назначен.",
      ),
    ).toBeInTheDocument();
    expect(
      within(confirm).getByRole("button", { name: "Отмена" }),
    ).toHaveFocus();
    await user.click(
      within(confirm).getByRole("button", { name: "Снять закрепление" }),
    );
    await waitFor(() =>
      expect(
        fakeApi.calls("DELETE /api/v1/career/org-units/sales/hr"),
      ).toHaveLength(1),
    );
  });

  it("offers no removal for a unit that only inherits its HR", async () => {
    serve("GlobalAdmin", {
      assigned: null,
      inherited: person("u2", "Петров Пётр", "company", "Компания"),
    });
    await openModal();

    await screen.findByText("Унаследован из «Компания»: Петров Пётр");
    expect(
      screen.queryByRole("button", { name: "Снять закрепление" }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Закрепить" }),
    ).toBeInTheDocument();
  });
});
