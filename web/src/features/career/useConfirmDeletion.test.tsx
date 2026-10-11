import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../../api/ApiError";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { moreBlockersText, useConfirmDeletion } from "./useConfirmDeletion";

type Deletion = Parameters<ReturnType<typeof useConfirmDeletion>>[0];

function Harness({ deletion }: { deletion: Deletion }) {
  const confirmDeletion = useConfirmDeletion();
  return (
    <button type="button" onClick={() => confirmDeletion(deletion)}>
      Начать удаление
    </button>
  );
}

async function ask(deletion: Deletion) {
  render(
    <Providers>
      <Harness deletion={deletion} />
    </Providers>,
  );
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "Начать удаление" }));
  return user;
}

const refusal = (fields: object) =>
  new ApiError(409, { title: "Операция отклонена", ...fields });

beforeEach(() => {
  queryClient.clear();
});

describe("moreBlockersText (AC-25)", () => {
  it.each([
    [1, "и 1 другая связанная запись"],
    [2, "и 2 других связанных записи"],
    [3, "и 3 других связанных записи"],
    [4, "и 4 других связанных записи"],
    [22, "и 22 других связанных записи"],
    [104, "и 104 других связанных записи"],
    [5, "и 5 других связанных записей"],
    [10, "и 10 других связанных записей"],
    [11, "и 11 других связанных записей"],
    [12, "и 12 других связанных записей"],
    [13, "и 13 других связанных записей"],
    [14, "и 14 других связанных записей"],
    [15, "и 15 других связанных записей"],
    [20, "и 20 других связанных записей"],
    [100, "и 100 других связанных записей"],
    [112, "и 112 других связанных записей"],
  ])("agrees the word with %i", (count, text) => {
    expect(moreBlockersText(count)).toBe(text);
  });
});

describe("confirming a deletion (AC-25)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("asks «Удалить {вид} «{название}»?» with the extra line, danger-styled, and focuses «Отмена»", async () => {
    await ask({
      kind: "трек",
      name: "Бэкенд",
      content: "Будут удалены и его уровни",
      run: vi.fn(),
    });

    const dialog = await screen.findByRole("dialog");
    expect(dialog).toHaveAccessibleName("Удалить трек «Бэкенд»?");
    expect(
      within(dialog).getByText("Будут удалены и его уровни"),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByRole("button", { name: "Отмена" }),
    ).toHaveFocus();
    expect(within(dialog).getByRole("button", { name: "Удалить" })).toHaveClass(
      "ant-btn-dangerous",
    );
  });

  it("runs the deletion on «Удалить», reports it and closes the dialog", async () => {
    const run = vi.fn().mockResolvedValue(undefined);
    const onDeleted = vi.fn();
    const user = await ask({ kind: "шкалу", name: "Базовая", run, onDeleted });

    await user.click(await screen.findByRole("button", { name: "Удалить" }));

    await waitFor(() => expect(onDeleted).toHaveBeenCalledOnce());
    expect(run).toHaveBeenCalledOnce();
  });

  it("does nothing on «Отмена»", async () => {
    const run = vi.fn();
    const user = await ask({ kind: "шкалу", name: "Базовая", run });

    await user.click(await screen.findByRole("button", { name: "Отмена" }));

    expect(run).not.toHaveBeenCalled();
  });

  it("shows the server's reason and the blocking records, each by kind and name, with the unit of a matrix and the track of a record in brackets", async () => {
    const run = vi.fn().mockRejectedValue(
      refusal({
        detail:
          "Направление используют треки, компетенции или матрицы — удалить нельзя",
        blockers: [
          { kind: "Track", id: "t", name: "Бэкенд", context: null },
          {
            kind: "Competency",
            id: "c",
            name: "Проектирование",
            context: null,
          },
          {
            kind: "CompetencyVersion",
            id: "v",
            name: "Старое имя",
            context: null,
          },
          { kind: "Matrix", id: "m", name: "Бэкенд", context: "Отдел продаж" },
          { kind: "Track", id: "x", name: "Фронтенд", context: "Отдел 2" },
        ],
        moreCount: 3,
      }),
    );
    const user = await ask({ kind: "направление", name: "Разработка", run });

    await user.click(await screen.findByRole("button", { name: "Удалить" }));

    expect(
      (await screen.findAllByRole("listitem")).map((item) => item.textContent),
    ).toEqual([
      "Трек «Бэкенд»",
      "Компетенция «Проектирование»",
      "Версия компетенции «Старое имя»",
      "Матрица «Бэкенд» — Отдел продаж",
      "Трек «Фронтенд» (Отдел 2)",
    ]);
    expect(
      screen.getAllByText(
        "Направление используют треки, компетенции или матрицы — удалить нельзя",
      ),
    ).not.toHaveLength(0);
    expect(screen.getByText("и 3 других связанных записи")).toBeInTheDocument();
  });

  it("names no records when the server counts only records the user cannot read, and shows the number", async () => {
    const run = vi.fn().mockRejectedValue(
      refusal({
        detail: "Трек используют матрицы — удалить нельзя",
        blockers: [],
        moreCount: 1,
      }),
    );
    const user = await ask({ kind: "трек", name: "Бэкенд", run });

    await user.click(await screen.findByRole("button", { name: "Удалить" }));

    expect(
      await screen.findByText("и 1 другая связанная запись"),
    ).toBeInTheDocument();
    expect(
      screen.getAllByText("Трек используют матрицы — удалить нельзя"),
    ).not.toHaveLength(0);
    expect(screen.queryByRole("list")).not.toBeInTheDocument();
  });

  it("shows a refusal without records as a message with the server's reason, not as a list", async () => {
    const run = vi.fn().mockRejectedValue(
      refusal({
        detail: "Шкалу по умолчанию удалить нельзя",
        blockers: [],
        moreCount: 0,
      }),
    );
    const user = await ask({ kind: "шкалу", name: "Базовая", run });

    await user.click(await screen.findByRole("button", { name: "Удалить" }));

    expect(
      await screen.findByText("Шкалу по умолчанию удалить нельзя"),
    ).toBeInTheDocument();
    expect(screen.queryByRole("list")).not.toBeInTheDocument();
  });

  it("explains a failure that is not a refusal in words and keeps no blocker list", async () => {
    const run = vi
      .fn()
      .mockRejectedValue(new ApiError(412, { title: "Precondition Failed" }));
    const user = await ask({ kind: "шкалу", name: "Базовая", run });

    await user.click(await screen.findByRole("button", { name: "Удалить" }));

    expect(
      await screen.findByText(/изменены другим пользователем/),
    ).toBeInTheDocument();
    expect(screen.queryByRole("list")).not.toBeInTheDocument();
  });
});
