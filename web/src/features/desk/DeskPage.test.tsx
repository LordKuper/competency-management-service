import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { DeskPage } from "./DeskPage";
import type { Desk, DeskGroup, DeskItem, DeskKind } from "./deskApi";

const ACCOUNT = {
  id: "account",
  email: "user@test.local",
  roles: [] as string[],
  employeeId: null,
  employeeName: null,
  employeeLastName: null,
  employeeFirstName: null,
  employeeMiddleName: null,
};
const SHOW_MORE_STEP = 20;
const FIRST_ITEMS = 10;

function item(
  group: DeskGroup,
  kind: DeskKind,
  name: string,
  options: { isReference?: boolean; type?: DeskItem["object"]["type"] } = {},
): DeskItem {
  return {
    object: { type: options.type ?? "OrgUnit", id: `id-${name}`, name },
    group,
    isReference: options.isReference ?? false,
    reasons: [{ kind }],
  };
}

function manyItems(group: DeskGroup, count: number): DeskItem[] {
  return Array.from({ length: count }, (_, index) =>
    item(
      group,
      "UnitWithoutHead",
      `Отдел ${String(index + 1).padStart(4, "0")}`,
    ),
  );
}

function answerWith(desk: Desk, roles: string[] = []) {
  fakeApi.on("GET /api/v1/auth/me", () => Response.json({ ...ACCOUNT, roles }));
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/desk", () => Response.json(desk));
}

function renderDesk() {
  render(
    <Providers>
      <MemoryRouter>
        <DeskPage />
      </MemoryRouter>
    </Providers>,
  );
}

beforeEach(() => {
  queryClient.clear();
});

describe("the desk page (AC-2…AC-9)", () => {
  it("shows the groups of expected actions with their full counts, and the count of actions", async () => {
    answerWith(
      {
        actionCount: 12,
        prerequisites: [],
        groups: [
          {
            group: "UnitsToConnect",
            isReference: false,
            total: 12,
            items: manyItems("UnitsToConnect", FIRST_ITEMS),
          },
        ],
      },
      ["GlobalAdmin"],
    );

    renderDesk();

    const heading = await screen.findByRole("heading", {
      name: "Подключить подразделения",
    });
    expect(
      within(heading.parentElement as HTMLElement).getByText("12"),
    ).toBeInTheDocument();
    expect(screen.getByText("Показано 10 из 12")).toBeInTheDocument();
    expect(
      await screen.findAllByRole("link", { name: "Подключить" }),
    ).toHaveLength(FIRST_ITEMS);
    expect(screen.queryByText("Всё сделано")).not.toBeInTheDocument();
  });

  it("says everything is done when only groups for reference remain, and keeps them folded outside the count", async () => {
    answerWith({
      actionCount: 0,
      prerequisites: [],
      groups: [
        {
          group: "EmptyUnits",
          isReference: true,
          total: 3,
          items: [
            item("EmptyUnits", "EmptyUnit", "Пустой отдел", {
              isReference: true,
            }),
          ],
        },
      ],
    });
    const user = userEvent.setup();

    renderDesk();

    expect(await screen.findByText("Всё сделано")).toBeInTheDocument();
    expect(screen.queryByText(/Пустой отдел/)).not.toBeInTheDocument();
    const toggle = screen.getByRole("button", { name: /К сведению/ });
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    await user.click(toggle);
    expect(await screen.findByText(/Пустой отдел/)).toBeInTheDocument();
    expect(toggle).toHaveAttribute("aria-expanded", "true");
  });

  it("says everything is done on an empty desk", async () => {
    answerWith({ actionCount: 0, prerequisites: [], groups: [] });

    renderDesk();

    expect(await screen.findByText("Всё сделано")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /К сведению/ }),
    ).not.toBeInTheDocument();
  });

  it("reads more of a group from its start, twenty at a time, and offers no more once the group is shown", async () => {
    answerWith(
      {
        actionCount: 25,
        prerequisites: [],
        groups: [
          {
            group: "UnitsToConnect",
            isReference: false,
            total: 25,
            items: manyItems("UnitsToConnect", FIRST_ITEMS),
          },
        ],
      },
      ["GlobalAdmin"],
    );
    fakeApi.on("GET /api/v1/desk/items", (request) => {
      const limit = Number(new URL(request.url).searchParams.get("limit"));
      return Response.json({
        items: manyItems("UnitsToConnect", Math.min(limit, 25)),
        total: 25,
      });
    });
    const user = userEvent.setup();
    renderDesk();

    await user.click(
      await screen.findByRole("button", { name: "Показать ещё 15" }),
    );

    await waitFor(() =>
      expect(screen.getAllByRole("link", { name: "Подключить" })).toHaveLength(
        25,
      ),
    );
    const request = fakeApi.calls("GET /api/v1/desk/items").at(-1);
    const query = new URL(request?.url ?? "").searchParams;
    expect(query.getAll("group")).toEqual(["UnitsToConnect"]);
    expect(query.get("limit")).toBe(String(FIRST_ITEMS + SHOW_MORE_STEP));
    expect(
      screen.queryByRole("button", { name: /Показать ещё/ }),
    ).not.toBeInTheDocument();
  });

  it("says a group is cut when the server returns fewer rows than the group has, and offers no more of it", async () => {
    answerWith(
      {
        actionCount: 2500,
        prerequisites: [],
        groups: [
          {
            group: "UnitsToConnect",
            isReference: false,
            total: 2500,
            items: manyItems("UnitsToConnect", FIRST_ITEMS),
          },
        ],
      },
      ["GlobalAdmin"],
    );
    fakeApi.on("GET /api/v1/desk/items", () =>
      Response.json({ items: manyItems("UnitsToConnect", 20), total: 2500 }),
    );
    const user = userEvent.setup();
    renderDesk();

    await user.click(
      await screen.findByRole("button", { name: "Показать ещё 20" }),
    );

    expect(
      await screen.findByText(/Показаны первые 20 из 2\s?500 пунктов/),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /Показать ещё/ }),
    ).not.toBeInTheDocument();
  });

  it("reports a failed read of the desk with a way to retry, without hiding the scenarios of the user's roles", async () => {
    fakeApi.on("GET /api/v1/auth/me", () =>
      Response.json({ ...ACCOUNT, roles: ["GlobalAdmin"] }),
    );
    fakeApi.on("GET /api/v1/career/me", () =>
      Response.json({ headedUnits: [], hrUnits: [] }),
    );
    fakeApi.on("GET /api/v1/desk", () => problem(403));
    const user = userEvent.setup();
    renderDesk();

    expect(
      await screen.findByText("Не удалось загрузить ожидаемые действия"),
    ).toBeInTheDocument();
    expect(screen.queryByText("Сейчас начать нечего")).not.toBeInTheDocument();
    fakeApi.on("GET /api/v1/desk", () =>
      Response.json({ actionCount: 0, prerequisites: [], groups: [] }),
    );
    await user.click(screen.getByRole("button", { name: /Повторить/ }));
    expect(await screen.findByText("Всё сделано")).toBeInTheDocument();
  });
});

describe("an invitation sent again from the desk (AC-39, AC-40)", () => {
  const INVITED: DeskItem = {
    object: {
      type: "UserAccount",
      id: "account-1",
      name: "invited@test.local",
    },
    group: "Invitations",
    isReference: false,
    reasons: [
      { kind: "InvitationNotAccepted", details: { linkState: "Expired" } },
    ],
    invitation: { accountId: "account-1", version: 7 },
  };
  const DESK: Desk = {
    actionCount: 1,
    prerequisites: [],
    groups: [
      { group: "Invitations", isReference: false, total: 1, items: [INVITED] },
    ],
  };

  it("sends it with the account version the desk read, and reports that the letter went", async () => {
    answerWith(DESK, ["GlobalAdmin"]);
    fakeApi.on("POST /api/v1/users/account-1/resend-invitation", () =>
      Response.json({
        id: "account-1",
        email: "invited@test.local",
        mailSent: true,
      }),
    );
    const user = userEvent.setup();
    renderDesk();

    await user.click(
      await screen.findByRole("button", {
        name: "Отправить приглашение повторно",
      }),
    );

    expect(
      await screen.findByText(
        "Приглашение отправлено повторно: invited@test.local",
      ),
    ).toBeInTheDocument();
    const sent = fakeApi.calls(
      "POST /api/v1/users/account-1/resend-invitation",
    );
    expect(sent).toHaveLength(1);
    expect(sent[0]?.headers.get("If-Match")).toBe('"7"');
  });

  it("tells that the letter did not go, in a window that stays until closed", async () => {
    answerWith(DESK, ["GlobalAdmin"]);
    fakeApi.on("POST /api/v1/users/account-1/resend-invitation", () =>
      Response.json({
        id: "account-1",
        email: "invited@test.local",
        mailSent: false,
      }),
    );
    const user = userEvent.setup();
    renderDesk();

    await user.click(
      await screen.findByRole("button", {
        name: "Отправить приглашение повторно",
      }),
    );

    expect(
      (await screen.findAllByText("Письмо не отправлено"))[0],
    ).toBeInTheDocument();
  });

  it("rereads the desk after the request is refused, and does not retry by itself", async () => {
    answerWith(DESK, ["GlobalAdmin"]);
    fakeApi.on("POST /api/v1/users/account-1/resend-invitation", () =>
      problem(412),
    );
    const user = userEvent.setup();
    renderDesk();
    await screen.findByRole("button", {
      name: "Отправить приглашение повторно",
    });
    const readsBefore = fakeApi.calls("GET /api/v1/desk").length;

    await user.click(
      screen.getByRole("button", { name: "Отправить приглашение повторно" }),
    );

    await waitFor(() =>
      expect(fakeApi.calls("GET /api/v1/desk").length).toBeGreaterThan(
        readsBefore,
      ),
    );
    expect(
      fakeApi.calls("POST /api/v1/users/account-1/resend-invitation"),
    ).toHaveLength(1);
  });
});
