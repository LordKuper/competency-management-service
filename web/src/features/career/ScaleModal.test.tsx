import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import type { Scale } from "./catalogApi";
import { ScaleModal } from "./ScaleModal";

const EXISTING: Scale = {
  id: "scale",
  name: "Расширенная",
  points: [
    { score: 0, label: "Нет" },
    { score: 1, label: "Есть" },
  ],
  isDefault: false,
  status: "Active",
  version: 6,
  canEdit: true,
  canDelete: true,
};

function open(scale?: Scale) {
  const onClose = vi.fn();
  render(
    <Providers>
      <MemoryRouter>
        <ScaleModal scale={scale} onClose={onClose} />
      </MemoryRouter>
    </Providers>,
  );
  return { onClose, user: userEvent.setup() };
}

async function fill(
  user: ReturnType<typeof userEvent.setup>,
  field: HTMLElement,
  text: string,
) {
  await user.click(field);
  await user.paste(text);
}

const save = (user: ReturnType<typeof userEvent.setup>) =>
  user.click(screen.getByRole("button", { name: "Сохранить" }));

beforeEach(() => {
  queryClient.clear();
});

describe("scale form (AC-13)", { timeout: LONG_FLOW_TIMEOUT }, () => {
  it("creates a scale with the points that were typed", async () => {
    fakeApi.on("POST /api/v1/career/scales", () => Response.json(EXISTING));
    const { onClose, user } = open();

    await fill(user, screen.getByLabelText("Название"), "Новая");
    await fill(user, screen.getByLabelText("Балл"), "0");
    await fill(user, screen.getByLabelText("Значение"), "Не владеет");
    await user.click(screen.getByRole("button", { name: "Добавить балл" }));
    const scores = screen.getAllByLabelText("Балл");
    const labels = screen.getAllByLabelText("Значение");
    await fill(user, scores[1] as HTMLElement, "1");
    await fill(user, labels[1] as HTMLElement, "Владеет");
    await save(user);

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(
      await fakeApi.calls("POST /api/v1/career/scales")[0]?.json(),
    ).toEqual({
      name: "Новая",
      points: [
        { score: 0, label: "Не владеет" },
        { score: 1, label: "Владеет" },
      ],
    });
  });

  it("refuses two points with one score before anything is sent", async () => {
    const { user } = open(EXISTING);
    await screen.findByDisplayValue("Расширенная");
    const scores = screen.getAllByLabelText("Балл");
    await user.clear(scores[1] as HTMLElement);
    await user.type(scores[1] as HTMLElement, "0");

    await save(user);

    expect(
      await screen.findByText("Баллы не должны повторяться"),
    ).toBeInTheDocument();
    expect(fakeApi.calls("PUT /api/v1/career/scales/scale")).toHaveLength(0);
  });

  it("asks for a name, a score and a value", async () => {
    const { user } = open();

    await save(user);

    expect(await screen.findByText("Укажите название")).toBeInTheDocument();
    expect(fakeApi.calls("POST /api/v1/career/scales")).toHaveLength(0);
  });

  it("saves the changes against the version the form was opened with", async () => {
    fakeApi.on("PUT /api/v1/career/scales/scale", () =>
      Response.json({ ...EXISTING, version: 7 }),
    );
    const { onClose, user } = open(EXISTING);
    const name = await screen.findByDisplayValue("Расширенная");
    await user.clear(name);
    await user.type(name, "Подробная");

    await save(user);

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    const call = fakeApi.calls("PUT /api/v1/career/scales/scale")[0];
    expect(call?.headers.get("If-Match")).toBe('"6"');
    expect(await call?.json()).toMatchObject({ name: "Подробная" });
  });

  it("says that a scale of a published matrix cannot be changed when the server refuses it so", async () => {
    fakeApi.on("PUT /api/v1/career/scales/scale", () =>
      problem(409, { title: "Операция отклонена" }),
    );
    const { onClose, user } = open(EXISTING);
    await screen.findByDisplayValue("Расширенная");

    await save(user);

    const dialog = (
      await screen.findByText(
        "Используется в опубликованной матрице — изменить нельзя",
      )
    ).closest('[role="dialog"]') as HTMLElement;
    expect(within(dialog).getByText("Изменение шкалы")).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
