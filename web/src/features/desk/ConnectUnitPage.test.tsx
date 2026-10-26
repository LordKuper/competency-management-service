import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { ConnectUnitPage } from "./ConnectUnitPage";

const SETUP_ROUTE = "GET /api/v1/career/org-units/u1/setup";
const CREATE_ROUTE = "POST /api/v1/users";
const RESEND_ROUTE = "POST /api/v1/users/acc-head/resend-invitation";
const CREATE_LABEL = "Создать и отправить приглашение";
const SENDING_TEXT =
  "Пока идёт отправка письма, выбор подразделения и повторная отправка в других строках недоступны.";
const HEAD = { id: "e-head", fullName: "Иванов Иван Иванович" };
const HR = {
  employeeId: "e-hr",
  name: "Петрова Анна Сергеевна",
  orgUnitId: "u1",
  orgUnitName: "Команда",
};
const NO_ACCOUNT = { id: null, email: null, state: "None", version: null };
const INVITED_HEAD_ACCOUNT = {
  id: "acc-head",
  email: "head@test.local",
  state: "Invited",
  version: 2,
};

/** The part of the setup answer that differs between the arrangements of the tests. */
type SetupRows = Record<string, unknown>;

const BOTH_PRESENT: SetupRows = {
  head: HEAD,
  headAccount: NO_ACCOUNT,
  hr: { ...HR, account: NO_ACCOUNT },
  rows: [
    { row: "Head", status: "Ready" },
    { row: "HeadAccount", status: "Missing" },
    { row: "Hr", status: "Ready" },
    { row: "HrAccount", status: "Missing" },
    { row: "Positions", status: "Ready" },
  ],
};
const WITHOUT_HEAD: SetupRows = {
  ...BOTH_PRESENT,
  head: null,
  headAccount: null,
  rows: [
    { row: "Head", status: "Missing" },
    { row: "HeadAccount", status: "Waiting" },
    { row: "Hr", status: "Ready" },
    { row: "HrAccount", status: "Missing" },
    { row: "Positions", status: "Ready" },
  ],
};
const WITHOUT_HR: SetupRows = {
  ...BOTH_PRESENT,
  hr: null,
  rows: [
    { row: "Head", status: "Ready" },
    { row: "HeadAccount", status: "Missing" },
    { row: "Hr", status: "Missing" },
    { row: "HrAccount", status: "Waiting" },
    { row: "Positions", status: "Ready" },
  ],
};

function setupOf(rows: SetupRows) {
  return {
    unit: [{ id: "u1", name: "Команда" }],
    employees: 3,
    positions: { own: [], inherited: [] },
    ...rows,
  };
}

function serve(rows: SetupRows = BOTH_PRESENT) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "a@test.local", roles: ["GlobalAdmin"] }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
  fakeApi.on(SETUP_ROUTE, () => Response.json(setupOf(rows)));
  fakeApi.on(CREATE_ROUTE, () =>
    Response.json({ id: "created", mailSent: true }),
  );
}

function renderPage() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [
            {
              path: "/desk/connect-unit/:unitId",
              element: <ConnectUnitPage />,
            },
          ],
          { initialEntries: ["/desk/connect-unit/u1"] },
        )}
      />
    </Providers>,
  );
  return userEvent.setup();
}

const HEAD_ACCOUNT_ROW = 0;
const HR_ACCOUNT_ROW = 1;

/** Opens the dialog of the account row at the given place among the rows that offer to create an account, in the order of the page. */
async function openAccountDialog(
  user: ReturnType<typeof userEvent.setup>,
  place: number,
) {
  const buttons = await screen.findAllByRole("button", { name: CREATE_LABEL });
  expect(buttons).toHaveLength(2);
  await user.click(buttons[place] as HTMLElement);
}

async function createdBody() {
  await waitFor(() => expect(fakeApi.calls(CREATE_ROUTE)).toHaveLength(1));
  return fakeApi.calls(CREATE_ROUTE)[0]?.json();
}

beforeEach(() => {
  queryClient.clear();
});

const ACCOUNT_ROWS = [
  {
    who: "head",
    place: HEAD_ACCOUNT_ROW,
    title: "Учётная запись руководителя",
    intro: `У ${HEAD.fullName} нет учётной записи. Она будет создана с приглашением по почте и привязана к нему.`,
    sent: { roles: [], employeeId: HEAD.id },
  },
  {
    who: "HR",
    place: HR_ACCOUNT_ROW,
    title: "Учётная запись HR",
    intro: `У ${HR.name} нет учётной записи. Она будет создана с ролью «HR» и с приглашением по почте и привязана к нему.`,
    sent: { roles: ["Hr"], employeeId: HR.employeeId },
  },
];

describe("a gap in the account of the unit's head or HR is closed on the spot for the person the row is about (AC-83)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it.each(ACCOUNT_ROWS)(
    "names the $who in the dialog opened from the row of their account",
    async ({ place, title, intro }) => {
      serve();
      const user = renderPage();

      await openAccountDialog(user, place);

      expect(
        await screen.findByText(title, { selector: ".ant-modal-title" }),
      ).toBeInTheDocument();
      expect(screen.getByText(intro)).toBeInTheDocument();
    },
  );

  it.each(ACCOUNT_ROWS)(
    "creates the account of the $who with the roles of the row, bound to them",
    async ({ place, sent }) => {
      serve();
      const user = renderPage();
      await openAccountDialog(user, place);
      await user.type(await screen.findByLabelText("E-mail"), "who@test.local");

      await user.click(
        within(screen.getByRole("dialog")).getByRole("button", {
          name: CREATE_LABEL,
        }),
      );

      expect(await createdBody()).toEqual({ email: "who@test.local", ...sent });
    },
  );
});

const ONE_PRESENT = [
  {
    who: "HR when the unit has no head",
    rows: WITHOUT_HEAD,
    title: "Учётная запись HR",
  },
  {
    who: "head when the unit has no HR",
    rows: WITHOUT_HR,
    title: "Учётная запись руководителя",
  },
];

describe("the account of the one who is assigned is offered whether or not the other is (AC-83)", () => {
  it.each(ONE_PRESENT)(
    "offers to create the account of the $who, and only that one",
    async ({ rows, title }) => {
      serve(rows);
      const user = renderPage();
      const buttons = await screen.findAllByRole("button", {
        name: CREATE_LABEL,
      });
      expect(buttons).toHaveLength(1);

      await user.click(buttons[0] as HTMLElement);

      expect(
        await screen.findByText(title, { selector: ".ant-modal-title" }),
      ).toBeInTheDocument();
    },
  );
});

describe("the unit page says why its choices are off while an invitation is being sent (AC-83)", () => {
  it("shows the reason and turns the choice of the unit off until the letter is sent", async () => {
    serve({ ...BOTH_PRESENT, headAccount: INVITED_HEAD_ACCOUNT });
    const sent = deferred<Response>();
    fakeApi.on(RESEND_ROUTE, () => sent.promise);
    const user = renderPage();
    await user.click(
      await screen.findByRole("button", {
        name: "Отправить приглашение повторно",
      }),
    );

    expect(await screen.findByText(SENDING_TEXT)).toBeInTheDocument();
    expect(screen.getByRole("combobox")).toBeDisabled();
    sent.resolve(
      Response.json({
        id: "acc-head",
        email: "head@test.local",
        mailSent: true,
      }),
    );
    await waitFor(() => expect(screen.queryByText(SENDING_TEXT)).toBeNull());
    expect(screen.getByRole("combobox")).toBeEnabled();
  });
});
