import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import type { OrgUnit } from "./orgStructureApi";
import { UnitPositionsModal } from "./UnitPositionsModal";

const UNIT: OrgUnit = {
  id: "sales",
  name: "Продажи",
  parentId: "company",
  headEmployeeId: null,
  isActive: true,
  version: 1,
};

function entry(
  positionId: string,
  name: string,
  orgUnitId: string,
  orgUnitName: string,
  isActive = true,
) {
  return { positionId, name, isActive, orgUnitId, orgUnitName };
}

const LIST = [
  entry("engineer", "Инженер", "sales", "Продажи"),
  entry("cashier", "Кассир", "sales", "Продажи", false),
  entry("manager", "Менеджер", "company", "Компания"),
];

function serve(role: "GlobalAdmin" | "User", list = LIST) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "a@test.local", role }),
  );
  fakeApi.on("GET /api/v1/org-units/sales/positions", () =>
    Response.json(list),
  );
  fakeApi.on("GET /api/v1/positions", (request) => {
    const q = new URL(request.url).searchParams.get("q");
    const items = [
      { id: "engineer", name: "Инженер", isActive: true, version: 1 },
      { id: "buyer", name: "Закупщик", isActive: true, version: 1 },
      { id: "auditor", name: "Аудитор", isActive: true, version: 1 },
    ].filter((item) => !q || item.name.toLowerCase().includes(q));
    return Response.json({ items, total: items.length, page: 1, pageSize: 20 });
  });
}

async function openModal() {
  render(
    <Providers>
      <UnitPositionsModal unit={UNIT} onClose={() => undefined} />
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

describe("positions of a unit (AC-2, AC-4)", () => {
  it("lists the unit's own positions apart from the inherited ones, each inherited one with the unit it comes from", async () => {
    serve("GlobalAdmin");
    await openModal();

    const dialog = await dialogTitled("Должности подразделения «Продажи»");
    expect(await within(dialog).findByText("Инженер")).toBeInTheDocument();
    const own = within(dialog)
      .getByRole("heading", { name: "Собственные" })
      .closest("section") as HTMLElement;
    const inherited = within(dialog)
      .getByRole("heading", { name: "Унаследованные" })
      .closest("section") as HTMLElement;
    expect(within(own).getByText("Инженер")).toBeInTheDocument();
    expect(within(own).getByText("Кассир")).toBeInTheDocument();
    expect(within(own).getByText("Неактивна")).toBeInTheDocument();
    expect(within(inherited).getByText("Менеджер")).toBeInTheDocument();
    expect(within(inherited).getByText("из «Компания»")).toBeInTheDocument();
    expect(within(inherited).queryByText("Инженер")).not.toBeInTheDocument();
  });

  it("says that a list is empty in words", async () => {
    serve("GlobalAdmin", []);
    await openModal();

    expect(
      await screen.findByText("Собственных должностей нет"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Унаследованных должностей нет"),
    ).toBeInTheDocument();
  });

  it("offers an ordinary user the lists to read, without exclusion or adding", async () => {
    serve("User");
    await openModal();

    await screen.findByText("Инженер");
    expect(
      screen.queryByRole("button", { name: /Исключить/ }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText("Должность из справочника"),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Добавить" }),
    ).not.toBeInTheDocument();
  });

  it("shows how many employees keep a position before it is taken off the list, focuses «Отмена», and takes it off on confirmation", async () => {
    serve("GlobalAdmin");
    fakeApi.on("GET /api/v1/org-units/sales/positions/engineer/impact", () =>
      Response.json({ employeeCount: 12 }),
    );
    fakeApi.on(
      "DELETE /api/v1/org-units/sales/positions/engineer",
      () => new Response(null, { status: 204 }),
    );
    const user = await openModal();
    await screen.findByText("Инженер");

    await user.click(
      screen.getByRole("button", { name: "Исключить «Инженер» из перечня" }),
    );

    const confirm = await dialogTitled("Исключить «Инженер» из перечня?");
    expect(
      within(confirm).getByText(
        "Сотрудников подразделения и дочерних с этой должностью: 12. Их должность не изменится.",
      ),
    ).toBeInTheDocument();
    expect(
      within(confirm).getByRole("button", { name: "Отмена" }),
    ).toHaveFocus();
    await user.click(
      within(confirm).getByRole("button", { name: "Исключить" }),
    );
    await waitFor(() =>
      expect(
        fakeApi.calls("DELETE /api/v1/org-units/sales/positions/engineer"),
      ).toHaveLength(1),
    );
  });

  it("adds a catalogue position to the unit's own list, leaving out those the list already holds", async () => {
    serve("GlobalAdmin");
    fakeApi.on(
      "POST /api/v1/org-units/sales/positions",
      () => new Response(null, { status: 204 }),
    );
    const user = await openModal();
    await screen.findByText("Инженер");

    await user.click(
      screen.getByRole("combobox", { name: "Должность из справочника" }),
    );
    expect(await screen.findByText("Закупщик")).toBeInTheDocument();
    expect(
      screen.queryByText("Инженер", {
        selector: ".ant-select-item-option-content",
      }),
    ).not.toBeInTheDocument();
    await user.click(screen.getByText("Закупщик"));
    await user.click(screen.getByRole("button", { name: "Добавить" }));

    await waitFor(() =>
      expect(
        fakeApi.calls("POST /api/v1/org-units/sales/positions"),
      ).toHaveLength(1),
    );
    expect(
      await fakeApi.calls("POST /api/v1/org-units/sales/positions")[0]?.json(),
    ).toEqual({ positionId: "buyer" });
  });

  it("asks for a position before adding and shows the server's refusal under the field", async () => {
    serve("GlobalAdmin");
    fakeApi.on("POST /api/v1/org-units/sales/positions", () =>
      problem(400, { errors: { positionId: ["Должность неактивна"] } }),
    );
    const user = await openModal();
    await screen.findByText("Инженер");

    await user.click(screen.getByRole("button", { name: "Добавить" }));
    expect(await screen.findByText("Выберите должность")).toBeInTheDocument();
    await user.click(
      screen.getByRole("combobox", { name: "Должность из справочника" }),
    );
    await user.click(await screen.findByText("Аудитор"));
    await user.click(screen.getByRole("button", { name: "Добавить" }));

    expect(await screen.findByText("Должность неактивна")).toBeInTheDocument();
  });
});
