import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { EmployeeForm } from "./EmployeeForm";

const ALPHA = { id: "le1", name: "ООО Альфа" };

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

const SECONDARY = {
  orgUnitId: "support",
  positionId: "operator",
  legalEntityIds: [] as string[],
};

function serve() {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "a@test.local", roles: ["GlobalAdmin"] }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () =>
    Response.json([unit("sales", "Продажи"), unit("support", "Поддержка")]),
  );
  fakeApi.on("GET /api/v1/org-units/sales/positions", () =>
    Response.json([allowed("sales", "engineer", "Инженер")]),
  );
  fakeApi.on("GET /api/v1/org-units/support/positions", () =>
    Response.json([allowed("support", "operator", "Оператор")]),
  );
  fakeApi.on("GET /api/v1/legal-entities", () =>
    Response.json({ items: [ALPHA], total: 1, page: 1, pageSize: 20 }),
  );
}

async function openForm(withSecondary: boolean) {
  serve();
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  render(
    <Providers>
      <EmployeeForm
        initialValues={{
          lastName: "Иванов",
          firstName: "Иван",
          orgUnitId: "sales",
          positionId: "engineer",
          legalEntityIds: [ALPHA.id],
          assignments: withSecondary ? [SECONDARY] : [],
        }}
        position={{ id: "engineer", name: "Инженер" }}
        saved={{
          legalEntities: [ALPHA],
          assignments: withSecondary
            ? [
                {
                  orgUnitId: "support",
                  orgUnitName: "Поддержка",
                  positionId: "operator",
                  position: "Оператор",
                  legalEntities: [],
                  isSecondary: true,
                },
              ]
            : [],
        }}
        submitLabel="Сохранить"
        onSubmit={onSubmit}
        onCancel={() => undefined}
      />
    </Providers>,
  );
  const user = userEvent.setup();
  await screen.findByText("Инженер");
  return { user, onSubmit };
}

async function save(
  user: ReturnType<typeof userEvent.setup>,
  onSubmit: ReturnType<typeof vi.fn>,
) {
  await user.click(screen.getByRole("button", { name: "Сохранить" }));
  await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
  return onSubmit.mock.calls[0]?.[0];
}

beforeEach(() => {
  queryClient.clear();
});

describe("the assignments in the employee form (AC-1, AC-2, AC-3)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("says there are no secondary assignments and saves an empty list", async () => {
    const { user, onSubmit } = await openForm(false);

    expect(screen.getByText(/Совмещений нет/)).toBeInTheDocument();

    expect(await save(user, onSubmit)).toMatchObject({
      legalEntityIds: [ALPHA.id],
      assignments: [],
    });
  });

  it("saves the secondary assignments the employee holds, unchanged", async () => {
    const { user, onSubmit } = await openForm(true);

    expect(screen.getByText("Совмещение 1")).toBeInTheDocument();

    expect(await save(user, onSubmit)).toMatchObject({
      orgUnitId: "sales",
      positionId: "engineer",
      assignments: [SECONDARY],
    });
  });

  it("removes a secondary assignment from what is saved", async () => {
    const { user, onSubmit } = await openForm(true);

    await user.click(
      screen.getByRole("button", { name: "Убрать: Совмещение 1" }),
    );

    expect(screen.queryByText("Совмещение 1")).not.toBeInTheDocument();
    expect(await save(user, onSubmit)).toMatchObject({ assignments: [] });
  });

  it("swaps a secondary assignment with the main one in one save (AC-3)", async () => {
    const { user, onSubmit } = await openForm(true);

    await user.click(
      screen.getByRole("button", { name: "Сделать основным: Совмещение 1" }),
    );

    expect(await save(user, onSubmit)).toMatchObject({
      orgUnitId: "support",
      positionId: "operator",
      legalEntityIds: [],
      assignments: [
        {
          orgUnitId: "sales",
          positionId: "engineer",
          legalEntityIds: [ALPHA.id],
        },
      ],
    });
  });

  it("adds a secondary assignment with its unit and position", async () => {
    const { user, onSubmit } = await openForm(false);

    await user.click(
      screen.getByRole("button", { name: /Добавить совмещение/ }),
    );
    const [, unitOfAssignment] = await screen.findAllByRole("combobox", {
      name: "Подразделение",
    });
    await user.click(unitOfAssignment as HTMLElement);
    await user.click(
      await screen.findByText("Поддержка", {
        selector: ".ant-select-tree-title",
      }),
    );
    const [, positionOfAssignment] = await screen.findAllByRole("combobox", {
      name: "Должность",
    });
    await user.click(positionOfAssignment as HTMLElement);
    await user.click(await screen.findByText("Оператор"));

    expect(await save(user, onSubmit)).toMatchObject({
      assignments: [SECONDARY],
    });
  });

  it("refuses a secondary assignment without a unit and a position", async () => {
    const { user, onSubmit } = await openForm(false);

    await user.click(
      screen.getByRole("button", { name: /Добавить совмещение/ }),
    );
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    expect(
      await screen.findByText("Выберите подразделение", {
        selector: ".ant-form-item-explain-error",
      }),
    ).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });
});

describe("the legal entities of an assignment (AC-1, AC-7)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("adds a legal entity named in the field and chooses it for the assignment", async () => {
    fakeApi.on("POST /api/v1/legal-entities/quick-add", () =>
      Response.json({ id: "le2", name: "ООО Бета" }),
    );
    const { user, onSubmit } = await openForm(false);

    await user.click(
      screen.getByRole("combobox", { name: "Юридические лица" }),
    );
    await user.keyboard("ООО Бета");
    await user.click(
      await screen.findByText("Добавить юридическое лицо «ООО Бета»"),
    );
    await waitFor(() =>
      expect(
        fakeApi.calls("POST /api/v1/legal-entities/quick-add"),
      ).toHaveLength(1),
    );

    expect(
      await fakeApi.calls("POST /api/v1/legal-entities/quick-add")[0]?.json(),
    ).toEqual({
      name: "ООО Бета",
    });
    expect(await save(user, onSubmit)).toMatchObject({
      legalEntityIds: [ALPHA.id, "le2"],
    });
  });

  it("does not offer to add a name the directory already has", async () => {
    const { user } = await openForm(false);

    await user.click(
      screen.getByRole("combobox", { name: "Юридические лица" }),
    );
    await user.keyboard("ооо альфа");

    await screen.findAllByText(ALPHA.name);
    expect(
      screen.queryByText(/Добавить юридическое лицо/),
    ).not.toBeInTheDocument();
  });
});
