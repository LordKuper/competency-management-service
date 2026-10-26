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

const UNIT = { id: "unit", name: "Отдел" };

function answerWith(
  desk: Desk,
  roles: string[] = [],
  session: {
    headedUnits?: object[];
    hrUnits?: object[];
    employeeId?: string;
  } = {},
) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({
      ...ACCOUNT,
      roles,
      employeeId: session.employeeId ?? null,
    }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({
      headedUnits: session.headedUnits ?? [],
      hrUnits: session.hrUnits ?? [],
    }),
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
  it("shows the groups of expected actions with their full counts, and the count of actions apart from them", async () => {
    answerWith(
      {
        actionCount: 15,
        prerequisites: [],
        groups: [
          {
            group: "UnitsToConnect",
            isReference: false,
            total: 12,
            items: manyItems("UnitsToConnect", FIRST_ITEMS),
          },
          {
            group: "Invitations",
            isReference: false,
            total: 3,
            items: [
              item("Invitations", "InvitationNotAccepted", "a@test.local", {
                type: "UserAccount",
              }),
            ],
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
    const invitations = screen.getByRole("heading", {
      name: "Приглашения не приняты",
    });
    expect(
      within(invitations.parentElement as HTMLElement).getByText("3"),
    ).toBeInTheDocument();
    const expected = screen.getByRole("heading", {
      name: "Ожидают вашего действия",
    });
    expect(
      within(expected.parentElement as HTMLElement).getByText("15"),
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

  it("reports a failed read of the desk with a way to retry, keeping the cards that need no desk and no neutral text", async () => {
    answerWith({ actionCount: 0, prerequisites: [], groups: [] }, [], {
      headedUnits: [UNIT],
    });
    fakeApi.on("GET /api/v1/desk", () => problem(403));
    const user = userEvent.setup();
    renderDesk();

    expect(
      await screen.findByText("Не удалось загрузить ожидаемые действия"),
    ).toBeInTheDocument();
    expect(
      await screen.findByRole("link", {
        name: "Построить матрицу компетенций",
      }),
    ).toBeInTheDocument();
    expect(screen.queryByText("Сейчас начать нечего")).not.toBeInTheDocument();
    fakeApi.on("GET /api/v1/desk", () =>
      Response.json({ actionCount: 0, prerequisites: [], groups: [] }),
    );
    await user.click(screen.getByRole("button", { name: /Повторить/ }));
    expect(await screen.findByText("Всё сделано")).toBeInTheDocument();
  });
});

describe("the scenario cards follow the session roles (AC-204…AC-208)", () => {
  const WAITING_VERSION: DeskItem = {
    object: { type: "Matrix", id: "matrix", name: "Матрица" },
    group: "VersionsToApprove",
    isReference: false,
    reasons: [{ kind: "VersionAwaitsHeadApproval" }],
    target: { type: "MatrixVersion", id: "version", name: "Версия" },
  };
  const WAITING_DRAFT: DeskItem = {
    object: { type: "Competency", id: "draft", name: "Черновик" },
    group: "DraftsToApprove",
    isReference: false,
    reasons: [{ kind: "DraftAwaitsApproval" }],
  };
  const deskWith = (...waiting: DeskItem[]): Desk => ({
    actionCount: waiting.length,
    prerequisites: [],
    groups: waiting.map((one) => ({
      group: one.group,
      isReference: false,
      total: 1,
      items: [one],
    })),
  });
  const card = (name: string) => screen.findByRole("link", { name });
  const expectNoCard = (name: string) =>
    expect(screen.queryByRole("link", { name })).not.toBeInTheDocument();

  it("gives a head of a unit the card to build a matrix", async () => {
    answerWith(deskWith(WAITING_VERSION), [], { headedUnits: [UNIT] });

    renderDesk();

    expect(await card("Построить матрицу компетенций")).toBeInTheDocument();
    expect(await card("Согласовать версию")).toBeInTheDocument();
  });

  it("gives an HR of a unit the review, and not the card to build a matrix that a head has", async () => {
    answerWith(deskWith(WAITING_VERSION), [], { hrUnits: [UNIT] });

    renderDesk();

    expect(await card("Согласовать версию")).toBeInTheDocument();
    expectNoCard("Построить матрицу компетенций");
  });

  it("gives an account bound to an employee the card of their place in the company", async () => {
    answerWith(deskWith(), [], { employeeId: "employee" });

    renderDesk();

    expect(await card("Моё место в компании")).toBeInTheDocument();
  });

  it("gives the HR role the queue of drafts and none of the cards of an administrator", async () => {
    answerWith(deskWith(WAITING_DRAFT), ["Hr"]);

    renderDesk();

    expect(await card("Утвердить черновые записи")).toBeInTheDocument();
    expectNoCard("Принять сотрудника");
    expectNoCard("Подключить подразделение");
  });
});

describe("the columns of a group (AC-7, AC-8, AC-160)", () => {
  const AGO = new Date(Date.now() - 5.5 * 24 * 3_600_000).toISOString();
  const IN_TWO_DAYS = new Date(Date.now() + 2 * 24 * 3_600_000).toISOString();

  function deskOf(...groups: Desk["groups"]): Desk {
    return { actionCount: groups.length, prerequisites: [], groups };
  }

  function returnedItem(name: string, occurredAt?: string): DeskItem {
    return {
      object: { type: "Matrix", id: `id-${name}`, name },
      group: "ReturnedVersions",
      isReference: false,
      reasons: [{ kind: "VersionReturned", occurredAt }],
      target: { type: "MatrixVersion", id: "version", name },
    };
  }

  const rowOf = (text: string) =>
    screen.getByText(text).closest("tr")?.textContent ?? "";

  it("shows how long ago it was in a dated group, only for the item that has a date", async () => {
    answerWith(
      deskOf({
        group: "ReturnedVersions",
        isReference: false,
        total: 2,
        items: [returnedItem("Датированная", AGO), returnedItem("Без даты")],
      }),
    );

    renderDesk();

    await screen.findByText(/Датированная/);
    expect(
      screen.getByRole("columnheader", { name: "Давность" }),
    ).toBeInTheDocument();
    expect(rowOf("Матрица «Датированная»")).toContain("назад");
    expect(rowOf("Матрица «Без даты»")).not.toContain("назад");
  });

  it("shows no age in a group whose items are not dated events, even when the reason has a moment", async () => {
    answerWith(
      deskOf({
        group: "UnitsToConnect",
        isReference: false,
        total: 1,
        items: [
          {
            ...item("UnitsToConnect", "UnitWithoutHead", "Отдел"),
            reasons: [{ kind: "UnitWithoutHead", occurredAt: AGO }],
          },
        ],
      }),
    );

    renderDesk();

    await screen.findByText(/Отдел/);
    expect(
      screen.queryByRole("columnheader", { name: "Давность" }),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/назад/)).not.toBeInTheDocument();
  });

  it("shows an invitation link that is about to end and one that has ended, in a column of their own", async () => {
    const invited = (name: string, linkState: "ExpiringSoon" | "Expired") => ({
      ...item("Invitations", "InvitationNotAccepted", name, {
        type: "UserAccount" as const,
      }),
      reasons: [
        {
          kind: "InvitationNotAccepted" as const,
          occurredAt: linkState === "Expired" ? AGO : IN_TWO_DAYS,
          details: { linkState },
        },
      ],
    });
    answerWith(
      deskOf({
        group: "Invitations",
        isReference: false,
        total: 2,
        items: [
          invited("soon@test.local", "ExpiringSoon"),
          invited("gone@test.local", "Expired"),
        ],
      }),
    );

    renderDesk();

    await screen.findByText("soon@test.local");
    expect(
      screen.getByRole("columnheader", { name: "Срок ссылки" }),
    ).toBeInTheDocument();
    expect(rowOf("soon@test.local")).toContain("Скоро истечёт");
    expect(rowOf("gone@test.local")).toContain("Истекла");
    expect(rowOf("gone@test.local")).not.toContain("Скоро истечёт");
  });

  it("keeps the link to the scenario and the button to send the invitation again on an item with two reasons", async () => {
    answerWith(
      deskOf({
        group: "UnitsToConnect",
        isReference: false,
        total: 1,
        items: [
          {
            ...item("UnitsToConnect", "UnitWithoutHead", "Отдел"),
            reasons: [
              { kind: "UnitWithoutHead" },
              {
                kind: "InvitationNotAccepted",
                details: { linkState: "Expired" },
              },
            ],
            invitation: { accountId: "account-1", version: 2 },
          },
        ],
      }),
      ["GlobalAdmin"],
    );

    renderDesk();

    const row = (await screen.findByText(/Отдел/)).closest("tr") as HTMLElement;
    expect(
      within(row).getByRole("link", { name: "Подключить" }),
    ).toHaveAttribute("href", "/desk/connect-unit/id-Отдел");
    expect(
      within(row).getByRole("button", {
        name: "Отправить приглашение повторно",
      }),
    ).toBeInTheDocument();
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
