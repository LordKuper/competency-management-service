import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { NameModal } from "./NameModal";

const ROUTE = "POST /api/v1/career/directions";

const lockedControls = () =>
  screen.getByRole("textbox", { name: "Название" }).hasAttribute("disabled")
    ? ["name"]
    : [];

beforeEach(() => {
  queryClient.clear();
});

describe("name form while the entry is being saved (AC-61)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("holds the name while the save is in flight, and frees it when it is refused", async () => {
    const held = deferred<Response>();
    fakeApi.on(ROUTE, () => held.promise);
    render(
      <Providers>
        <MemoryRouter>
          <NameModal
            segment="directions"
            title="Новая область"
            onClose={vi.fn()}
          />
        </MemoryRouter>
      </Providers>,
    );
    const user = userEvent.setup();
    await user.type(
      await screen.findByRole("textbox", { name: "Название" }),
      "Разработка",
    );
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
