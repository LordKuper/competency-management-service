import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import type { CareerLevel } from "./catalogApi";
import { LevelModal } from "./LevelModal";

const ROUTE = "PUT /api/v1/career/levels/level";
const LEVEL: CareerLevel = {
  id: "level",
  name: "Младший",
  directionId: "direction",
  trackId: "track",
  position: 1,
  status: "Active",
  version: 3,
  canEdit: true,
  canDelete: true,
};

const lockedControls = () =>
  screen.getByRole("combobox", { name: "Название" }).hasAttribute("disabled")
    ? ["name"]
    : [];

beforeEach(() => {
  queryClient.clear();
});

describe("level form while the level is being saved (AC-61)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("holds the name while the save is in flight, and frees it when it is refused", async () => {
    const held = deferred<Response>();
    fakeApi.on(ROUTE, () => held.promise);
    fakeApi.on("GET /api/v1/career/levels", () =>
      Response.json({ items: [], total: 0, page: 1, pageSize: 200 }),
    );
    render(
      <Providers>
        <MemoryRouter>
          <LevelModal level={LEVEL} onClose={vi.fn()} />
        </MemoryRouter>
      </Providers>,
    );
    const user = userEvent.setup();
    await screen.findByDisplayValue("Младший");
    expect(lockedControls()).toEqual([]);

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(fakeApi.calls(ROUTE)).toHaveLength(1));
    expect(lockedControls()).toEqual(["name"]);
    held.resolve(problem(500, { title: "Сбой" }));
    expect(
      await screen.findByText("Не удалось сохранить запись"),
    ).toBeInTheDocument();
    expect(lockedControls()).toEqual([]);
  });
});
