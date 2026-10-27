import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import type { OrgUnit } from "./orgStructureApi";
import { useUnitDeletion } from "./useUnitDeletion";

const DELETE_ROUTE = "DELETE /api/v1/org-units/sales";
const UNIT: OrgUnit = {
  id: "sales",
  name: "Продажи",
  parentId: "company",
  headEmployeeId: null,
  isActive: true,
  version: 4,
};

function Host({ onDeleted }: { onDeleted: (unit: OrgUnit) => void }) {
  const confirmDelete = useUnitDeletion(onDeleted);
  return (
    <button type="button" onClick={() => confirmDelete(UNIT)}>
      удалить
    </button>
  );
}

async function start(onDeleted = vi.fn()) {
  render(
    <Providers>
      <Host onDeleted={onDeleted} />
    </Providers>,
  );
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "удалить" }));
  return { user, onDeleted };
}

async function dialogTitled(title: string) {
  const [heading] = await screen.findAllByText(title, {
    selector: ".ant-modal-confirm-title",
  });
  return within(heading?.closest('[role="dialog"]') as HTMLElement);
}

beforeEach(() => {
  queryClient.clear();
});

describe("the deletion of a unit (AC-33)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("names the unit and what goes with it, and sends nothing until the user confirms", async () => {
    await start();

    const dialog = await dialogTitled("Удалить подразделение «Продажи»?");

    expect(
      dialog.getByText(
        /вместе с перечнем должностей и закреплением ответственного HR/,
      ),
    ).toBeInTheDocument();
    expect(fakeApi.calls(DELETE_ROUTE)).toHaveLength(0);
  });

  it("deletes with the version the screen holds, tells the screen which unit went and confirms", async () => {
    fakeApi.on(DELETE_ROUTE, () => new Response(null, { status: 204 }));
    const { user, onDeleted } = await start();

    const dialog = await dialogTitled("Удалить подразделение «Продажи»?");
    await user.click(dialog.getByRole("button", { name: "Удалить" }));

    await waitFor(() => expect(onDeleted).toHaveBeenCalledWith(UNIT));
    expect(fakeApi.calls(DELETE_ROUTE)[0]?.headers.get("If-Match")).toBe('"4"');
    expect(
      await screen.findByText("Подразделение удалено"),
    ).toBeInTheDocument();
  });

  it("deletes nothing when the user cancels", async () => {
    const { user, onDeleted } = await start();

    const dialog = await dialogTitled("Удалить подразделение «Продажи»?");
    await user.click(dialog.getByRole("button", { name: "Отмена" }));

    expect(fakeApi.calls(DELETE_ROUTE)).toHaveLength(0);
    expect(onDeleted).not.toHaveBeenCalled();
  });

  it("lists every obstacle of a refused deletion in a dialog, and leaves the screen's selection alone", async () => {
    fakeApi.on(DELETE_ROUTE, () =>
      problem(409, {
        title: "Операция отклонена",
        detail:
          "Нельзя удалить подразделение «Продажи»: с ним связаны другие данные.",
        obstacles: [
          "Дочерние подразделения, в том числе неактивные: 2",
          "Треки карьеры подразделения: 1",
        ],
      }),
    );
    const { user, onDeleted } = await start();

    const confirm = await dialogTitled("Удалить подразделение «Продажи»?");
    await user.click(confirm.getByRole("button", { name: "Удалить" }));

    const refusal = await dialogTitled("Не удалось удалить подразделение");
    expect(
      refusal.getByText(/с ним связаны другие данные/),
    ).toBeInTheDocument();
    expect(
      refusal.getAllByRole("listitem").map((item) => item.textContent),
    ).toEqual([
      "Дочерние подразделения, в том числе неактивные: 2",
      "Треки карьеры подразделения: 1",
    ]);
    expect(onDeleted).not.toHaveBeenCalled();
  });

  it("describes any other failure in plain words", async () => {
    fakeApi.on(DELETE_ROUTE, () => problem(412));
    const { user, onDeleted } = await start();

    const confirm = await dialogTitled("Удалить подразделение «Продажи»?");
    await user.click(confirm.getByRole("button", { name: "Удалить" }));

    const failure = await dialogTitled("Не удалось удалить подразделение");
    expect(
      failure.getByText(/изменены другим пользователем/),
    ).toBeInTheDocument();
    expect(onDeleted).not.toHaveBeenCalled();
  });
});
