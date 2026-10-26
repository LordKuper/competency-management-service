import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { EmployeeForm } from "./EmployeeForm";

const unit = (id: string, name: string) => ({
  id,
  name,
  parentId: null,
  headEmployeeId: null,
  isActive: true,
  version: 1,
});

const allowed = (unitId: string, positionId: string, name: string) => ({
  positionId,
  name,
  isActive: true,
  orgUnitId: unitId,
  orgUnitName: unitId,
});

const FORMER = { id: "former", name: "Кладовщик" };

function serve() {
  fakeApi.on("GET /api/v1/org-units/tree", () =>
    Response.json([unit("sales", "Продажи"), unit("support", "Поддержка")]),
  );
  fakeApi.on("GET /api/v1/org-units/sales/positions", () =>
    Response.json([allowed("sales", "engineer", "Инженер")]),
  );
  fakeApi.on("GET /api/v1/org-units/support/positions", () =>
    Response.json([
      allowed("support", "operator", "Оператор"),
      allowed("support", "engineer", "Инженер"),
    ]),
  );
}

async function openForm(
  held = FORMER,
  onSubmit = vi.fn().mockResolvedValue(undefined),
) {
  serve();
  render(
    <Providers>
      <EmployeeForm
        initialValues={{
          lastName: "Иванов",
          firstName: "Иван",
          orgUnitId: "sales",
          positionId: held.id,
        }}
        position={held}
        submitLabel="Сохранить"
        onSubmit={onSubmit}
        onCancel={() => undefined}
      />
    </Providers>,
  );
  const user = userEvent.setup();
  await screen.findByText(held.name);
  return { user, onSubmit };
}

async function transferTo(
  user: ReturnType<typeof userEvent.setup>,
  unitName: string,
) {
  await user.click(
    await screen.findByRole("combobox", { name: "Подразделение" }),
  );
  await user.click(
    await screen.findByText(unitName, { selector: ".ant-select-tree-title" }),
  );
  await waitFor(() =>
    expect(
      fakeApi.calls("GET /api/v1/org-units/support/positions"),
    ).toHaveLength(1),
  );
}

function lockedFields() {
  const fields: [string, HTMLElement][] = [
    ["surname", screen.getByLabelText("Фамилия")],
    ["name", screen.getByLabelText("Имя")],
    ["patronymic", screen.getByLabelText("Отчество")],
    ["unit", screen.getByRole("combobox", { name: "Подразделение" })],
    ["position", screen.getByRole("combobox", { name: "Должность" })],
  ];
  return fields
    .filter(([, field]) => field.hasAttribute("disabled"))
    .map(([kind]) => kind);
}

beforeEach(() => {
  queryClient.clear();
});

describe("the position after a transfer in the employee form (AC-3, AC-4)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("keeps the position the employee holds while the unit stays theirs", async () => {
    const { user, onSubmit } = await openForm();

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
    expect(onSubmit.mock.calls[0]?.[0]).toMatchObject({
      orgUnitId: "sales",
      positionId: FORMER.id,
    });
  });

  it("drops the old position once another unit is chosen, does not offer it there, and asks for a new one", async () => {
    const { user, onSubmit } = await openForm();

    await transferTo(user, "Поддержка");

    expect(await screen.findByText("Выберите должность")).toBeInTheDocument();
    await user.click(screen.getByRole("combobox", { name: "Должность" }));
    expect(await screen.findByText("Оператор")).toBeInTheDocument();
    expect(screen.queryByText(FORMER.name)).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Сохранить" }));
    expect(
      await screen.findByText("Выберите должность", {
        selector: ".ant-form-item-explain-error",
      }),
    ).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("still offers to add a position named like the old one to the new unit, and saves with the one added", async () => {
    fakeApi.on("POST /api/v1/org-units/support/positions/quick-add", () =>
      Response.json({
        id: "added",
        name: FORMER.name,
        isActive: true,
        version: 0,
      }),
    );
    const { user, onSubmit } = await openForm();
    await transferTo(user, "Поддержка");

    await user.click(screen.getByRole("combobox", { name: "Должность" }));
    await user.keyboard(FORMER.name);
    await user.click(
      await screen.findByText(`Добавить должность «${FORMER.name}»`),
    );
    await waitFor(() =>
      expect(
        fakeApi.calls("POST /api/v1/org-units/support/positions/quick-add"),
      ).toHaveLength(1),
    );
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
    expect(onSubmit.mock.calls[0]?.[0]).toMatchObject({
      orgUnitId: "support",
      positionId: "added",
    });
  });

  it("keeps the position when the unit it was transferred to allows it too", async () => {
    const { user, onSubmit } = await openForm({
      id: "engineer",
      name: "Инженер",
    });

    await transferTo(user, "Поддержка");
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
    expect(onSubmit.mock.calls[0]?.[0]).toMatchObject({
      orgUnitId: "support",
      positionId: "engineer",
    });
  });

  it("drops a position added in one unit when the form goes back to another", async () => {
    fakeApi.on("POST /api/v1/org-units/support/positions/quick-add", () =>
      Response.json({
        id: "analyst",
        name: "Аналитик",
        isActive: true,
        version: 0,
      }),
    );
    const { user } = await openForm();
    await transferTo(user, "Поддержка");
    await user.click(screen.getByRole("combobox", { name: "Должность" }));
    await user.keyboard("Аналитик");
    await user.click(await screen.findByText("Добавить должность «Аналитик»"));
    await waitFor(() =>
      expect(
        fakeApi.calls("POST /api/v1/org-units/support/positions/quick-add"),
      ).toHaveLength(1),
    );
    const position = () =>
      screen
        .getByRole("combobox", { name: "Должность" })
        .closest(".ant-select") as HTMLElement;
    await waitFor(() =>
      expect(within(position()).getByText("Аналитик")).toBeInTheDocument(),
    );

    await user.click(
      await screen.findByRole("combobox", { name: "Подразделение" }),
    );
    await user.click(
      await screen.findByText("Продажи", {
        selector: ".ant-select-tree-title",
      }),
    );

    await waitFor(() =>
      expect(
        within(position()).getByText("Выберите должность"),
      ).toBeInTheDocument(),
    );
    expect(within(position()).queryByText("Аналитик")).not.toBeInTheDocument();
  });
});

describe("the employee form is held while the employee is being saved (AC-20)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("holds every field, the unit and the position choices included, and frees them once the save is over", async () => {
    const saved = deferred<undefined>();
    const onSubmit = vi.fn(() => saved.promise);
    const { user } = await openForm(FORMER, onSubmit);
    expect(lockedFields()).toEqual([]);

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
    expect(lockedFields()).toEqual([
      "surname",
      "name",
      "patronymic",
      "unit",
      "position",
    ]);
    saved.resolve(undefined);
    await waitFor(() => expect(lockedFields()).toEqual([]));
  });
});
