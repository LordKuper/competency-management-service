import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi } from "../../test/fakeApi";
import { UserListPage } from "./UserListPage";
import type { UserResponse } from "./usersApi";

const account = (
  id: string,
  state: { isBlocked: boolean; isInvited: boolean },
  version: number,
): UserResponse => ({
  id,
  email: `${id}@test.local`,
  role: "User",
  employeeId: null,
  employeeName: null,
  version,
  mailSent: null,
  ...state,
});

const INVITED = account("invited", { isBlocked: false, isInvited: true }, 3);
const ACTIVE = account("active", { isBlocked: false, isInvited: false }, 5);
const BLOCKED = account("blocked", { isBlocked: true, isInvited: false }, 7);
const INVITED_BLOCKED = account(
  "invitedblocked",
  { isBlocked: true, isInvited: true },
  9,
);
const ACCOUNTS = [INVITED, ACTIVE, BLOCKED, INVITED_BLOCKED];
const EMPLOYEE_PAGE = { items: [], total: 0, page: 1, pageSize: 20 };

const RESEND = /Отправить приглашение повторно/;
const RESET_LINK = /Отправить ссылку для сброса пароля/;
const OLD_RESET = /Сбросить пароль/;
const MAIL_NOT_SENT = /Почтовый сервер не принял письмо/;

function serveUsers() {
  fakeApi.on("GET /api/v1/users", () =>
    Response.json({
      items: ACCOUNTS,
      total: ACCOUNTS.length,
      page: 1,
      pageSize: 20,
    }),
  );
  fakeApi.on("GET /api/v1/employees", () => Response.json(EMPLOYEE_PAGE));
}

async function openUsers() {
  serveUsers();
  render(
    <Providers>
      <UserListPage />
    </Providers>,
  );
  await screen.findByText(INVITED.email);
}

async function openMenuOf(
  user: ReturnType<typeof userEvent.setup>,
  row: UserResponse,
) {
  await user.click(
    screen.getByRole("button", {
      name: `Действия с пользователем ${row.email}`,
    }),
  );
}

const rowOf = (row: UserResponse) =>
  within(screen.getByText(row.email).closest("tr") as HTMLElement);

/** antd keeps a closed message in the DOM for its leave animation, which jsdom never finishes. */
function isClosing(text: string) {
  return (
    screen
      .queryByText(text)
      ?.closest("[role=alert]")
      ?.classList.contains("ant-message-fade-leave") ?? false
  );
}

beforeEach(() => {
  queryClient.clear();
});

describe("account states (AC-6, AC-11)", () => {
  it("names each state in words, an invited account that is blocked as both", async () => {
    await openUsers();

    expect(rowOf(INVITED).getByText("Приглашён")).toBeInTheDocument();
    expect(rowOf(INVITED).queryByText("Заблокирован")).not.toBeInTheDocument();
    expect(rowOf(ACTIVE).getByText("Активен")).toBeInTheDocument();
    expect(rowOf(BLOCKED).getByText("Заблокирован")).toBeInTheDocument();
    expect(rowOf(INVITED_BLOCKED).getByText("Приглашён")).toBeInTheDocument();
    expect(
      rowOf(INVITED_BLOCKED).getByText("Заблокирован"),
    ).toBeInTheDocument();
  });

  it("asks the list for the invited accounts only, without a block filter, when the invited state is chosen", async () => {
    await openUsers();
    const user = userEvent.setup();

    await user.click(screen.getByLabelText("Фильтр по состоянию"));
    await user.click(await screen.findByTitle("Приглашённые"));

    await waitFor(() => {
      const query = new URL(
        fakeApi.calls("GET /api/v1/users").at(-1)?.url ?? "",
      ).searchParams;
      expect(query.get("isInvited")).toBe("true");
      expect(query.has("isBlocked")).toBe(false);
    });
  });
});

describe("row menu (AC-6, AC-9, AC-11)", () => {
  it.each([
    ["an invited account", INVITED, RESEND, RESET_LINK],
    ["a registered active account", ACTIVE, RESET_LINK, RESEND],
  ])(
    "offers %s the link that fits its state and never the old password reset",
    async (_name, row, offered, withheld) => {
      await openUsers();
      const user = userEvent.setup();

      await openMenuOf(user, row);

      expect(
        await screen.findByRole("menuitem", { name: offered }),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole("menuitem", { name: withheld }),
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole("menuitem", { name: OLD_RESET }),
      ).not.toBeInTheDocument();
    },
  );

  it.each([
    ["a blocked account", BLOCKED],
    ["an invited account that is blocked", INVITED_BLOCKED],
  ])("offers no mail to %s", async (_name, row) => {
    await openUsers();
    const user = userEvent.setup();

    await openMenuOf(user, row);

    expect(
      await screen.findByRole("menuitem", { name: /Разблокировать/ }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("menuitem", { name: RESEND }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("menuitem", { name: RESET_LINK }),
    ).not.toBeInTheDocument();
  });

  it.each([
    ["resend-invitation", INVITED, RESEND, "Приглашение отправлено повторно"],
    [
      "send-password-reset",
      ACTIVE,
      RESET_LINK,
      "Ссылка для сброса пароля отправлена",
    ],
  ])(
    "%s: sends the row's version, reports the mail as sent and rereads the list",
    async (action, row, item, sentText) => {
      fakeApi.on(`POST /api/v1/users/${row.id}/${action}`, () =>
        Response.json({ ...row, mailSent: true }),
      );
      await openUsers();
      const user = userEvent.setup();

      await openMenuOf(user, row);
      await user.click(await screen.findByRole("menuitem", { name: item }));

      expect(await screen.findByText(sentText)).toBeInTheDocument();
      const [request] = fakeApi.calls(`POST /api/v1/users/${row.id}/${action}`);
      expect(request?.headers.get("If-Match")).toBe(`"${row.version}"`);
      await waitFor(() =>
        expect(fakeApi.calls("GET /api/v1/users")).toHaveLength(2),
      );
      expect(screen.queryByText(MAIL_NOT_SENT)).not.toBeInTheDocument();
    },
  );

  it("shows progress and issues one request only while a send is pending, then removes the progress", async () => {
    const answer = deferred<Response>();
    fakeApi.on(
      `POST /api/v1/users/${INVITED.id}/resend-invitation`,
      () => answer.promise,
    );
    await openUsers();
    const user = userEvent.setup();

    await openMenuOf(user, INVITED);
    await user.click(await screen.findByRole("menuitem", { name: RESEND }));

    expect(await screen.findByText("Отправка письма…")).toBeInTheDocument();
    await openMenuOf(user, INVITED);
    const again = await screen.findByRole("menuitem", { name: RESEND });
    expect(again).toHaveAttribute("aria-disabled", "true");
    await user.click(again);

    answer.resolve(Response.json({ ...INVITED, mailSent: true }));
    expect(
      await screen.findByText("Приглашение отправлено повторно"),
    ).toBeInTheDocument();
    await waitFor(() => expect(isClosing("Отправка письма…")).toBe(true));
    expect(
      fakeApi.calls(`POST /api/v1/users/${INVITED.id}/resend-invitation`),
    ).toHaveLength(1);
  });

  it("keeps a warning on screen until closed when the mail server did not accept the mail", async () => {
    fakeApi.on(`POST /api/v1/users/${INVITED.id}/resend-invitation`, () =>
      Response.json({ ...INVITED, mailSent: false }),
    );
    await openUsers();
    const user = userEvent.setup();

    await openMenuOf(user, INVITED);
    await user.click(await screen.findByRole("menuitem", { name: RESEND }));

    expect(await screen.findByText(MAIL_NOT_SENT)).toBeInTheDocument();
    expect(
      screen.queryByText("Приглашение отправлено повторно"),
    ).not.toBeInTheDocument();
  });
});

describe("new account (AC-4, AC-11)", () => {
  it("asks for no password, sends none and reports whether the invitation went out", async () => {
    fakeApi.on("POST /api/v1/users", () =>
      Response.json({ ...INVITED, mailSent: false }, { status: 201 }),
    );
    await openUsers();
    const user = userEvent.setup();

    await user.click(
      screen.getByRole("button", { name: "Создать пользователя" }),
    );
    const dialog = within(await screen.findByRole("dialog"));
    expect(dialog.getByLabelText("E-mail")).toBeInTheDocument();
    expect(dialog.queryByLabelText(/Пароль/)).not.toBeInTheDocument();
    await user.type(dialog.getByLabelText("E-mail"), "new@test.local");
    await user.click(dialog.getByRole("button", { name: "Создать" }));

    expect(await screen.findByText(MAIL_NOT_SENT)).toBeInTheDocument();
    expect(await fakeApi.calls("POST /api/v1/users")[0]?.json()).toEqual({
      email: "new@test.local",
      role: "User",
      employeeId: null,
    });
  });
});
