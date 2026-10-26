import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { BlockModal } from "./BlockModal";
import type { Competency } from "./catalogApi";

const ROUTE = "POST /api/v1/career/blocks";

const competency = (id: string, name: string): Competency => ({
  id,
  name,
  description: null,
  directionId: null,
  directionName: null,
  scaleId: "scale",
  scaleName: "Базовая",
  status: "Active",
  createdAt: "2026-10-01T00:00:00Z",
  versionNumber: 1,
  version: 1,
  canEdit: true,
  canDelete: true,
});

const COMPETENCIES = [
  competency("design", "Проектирование"),
  competency("teamwork", "Работа в команде"),
];

function serve() {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({
      id: "account",
      email: "admin@test.local",
      roles: ["GlobalAdmin"],
    }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
  fakeApi.on("GET /api/v1/career/competencies", () =>
    Response.json({
      items: COMPETENCIES,
      total: COMPETENCIES.length,
      page: 1,
      pageSize: 200,
    }),
  );
}

function lockedControls() {
  const dialog = screen
    .getByText("Новый общий блок", { selector: ".ant-modal-title" })
    .closest('[role="dialog"]') as HTMLElement;
  const controls: [string, HTMLElement[]][] = [
    ["name", [within(dialog).getByRole("textbox", { name: "Название" })]],
    ["unit", [within(dialog).getByRole("combobox", { name: "Действует для" })]],
    ["search", [within(dialog).getByRole("searchbox")]],
    [
      "create competency",
      [within(dialog).getByRole("button", { name: "Создать компетенцию" })],
    ],
    [
      "select all",
      [within(dialog).getByRole("checkbox", { name: "Select all" })],
    ],
    ["ticks", within(dialog).getAllByRole("checkbox", { name: /Select row/ })],
  ];
  return controls
    .filter(([, group]) => group.every((item) => item.hasAttribute("disabled")))
    .map(([kind]) => kind);
}

beforeEach(() => {
  queryClient.clear();
});

describe("block form while the block is being saved (AC-61)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("holds the name, the unit and the composition while the save is in flight, and frees them when it is refused", async () => {
    serve();
    const held = deferred<Response>();
    fakeApi.on(ROUTE, () => held.promise);
    render(
      <Providers>
        <MemoryRouter>
          <BlockModal onClose={vi.fn()} />
        </MemoryRouter>
      </Providers>,
    );
    const user = userEvent.setup();
    await user.type(
      await screen.findByRole("textbox", { name: "Название" }),
      "Командный блок",
    );
    await screen.findByText("Работа в команде");
    expect(lockedControls()).toEqual([]);

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(fakeApi.calls(ROUTE)).toHaveLength(1));
    expect(lockedControls()).toEqual([
      "name",
      "unit",
      "search",
      "create competency",
      "select all",
      "ticks",
    ]);
    held.resolve(problem(500, { title: "Сбой" }));
    expect(
      await screen.findByText("Не удалось сохранить запись"),
    ).toBeInTheDocument();
    expect(lockedControls()).toEqual([]);
  });
});
