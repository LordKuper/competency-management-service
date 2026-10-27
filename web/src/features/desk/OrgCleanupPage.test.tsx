import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { LeaveGuardProvider } from "../../app/leaveGuard";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { OrgCleanupPage } from "./OrgCleanupPage";

const ITEMS_ROUTE = "GET /api/v1/desk/items";
const DIALOG_TITLE = "Перейти без сохранения?";
const LEAVE = "Перейти без сохранения";
const STAY = "Остаться";
const POSITION_CHOICE = "Старший инженер";
const EMPLOYEE_CHOICE = "Петров Пётр";
const FILTER_LABEL = "Подразделение не подключено";

const POSITION_FINDING = {
  object: { type: "Employee", id: "e1", name: "Иванов Иван" },
  group: "OrgCleanup",
  isReference: false,
  reasons: [
    {
      kind: "PositionOutsideList",
      details: {
        entries: [
          { type: "Position", id: "p1", name: "Инженер" },
          { type: "OrgUnit", id: "u1", name: "Команда" },
        ],
      },
    },
  ],
};
const ACCOUNT_FINDING = {
  object: { type: "UserAccount", id: "acc1", name: "lost@test.local" },
  group: "OrgCleanup",
  isReference: false,
  reasons: [{ kind: "AccountWithoutEmployee" }],
};
const UNIT_FINDING = {
  object: { type: "OrgUnit", id: "u2", name: "Склад" },
  group: "UnitsToConnect",
  isReference: false,
  reasons: [{ kind: "UnitWithoutHead" }],
};
const FINDINGS = [POSITION_FINDING, ACCOUNT_FINDING, UNIT_FINDING];

function serve() {
  fakeApi.on(ITEMS_ROUTE, (request) => {
    const groups = new URL(request.url).searchParams.getAll("group");
    const items = FINDINGS.filter((item) => groups.includes(item.group));
    return Response.json({ items, total: items.length });
  });
  fakeApi.on("GET /api/v1/employees/e1", () =>
    Response.json({
      id: "e1",
      lastName: "Иванов",
      firstName: "Иван",
      middleName: null,
      fullName: "Иванов Иван",
      email: null,
      orgUnitId: "u1",
      orgUnitName: "Команда",
      positionId: "p0",
      position: "Прежняя",
      version: 3,
    }),
  );
  fakeApi.on("GET /api/v1/org-units/u1/positions", () =>
    Response.json([
      {
        positionId: "p2",
        name: POSITION_CHOICE,
        isActive: true,
        orgUnitId: "u1",
        orgUnitName: "Команда",
      },
    ]),
  );
  fakeApi.on("GET /api/v1/users/acc1", () =>
    Response.json({
      id: "acc1",
      email: "lost@test.local",
      roles: [],
      employeeId: null,
      version: 2,
    }),
  );
  fakeApi.on("GET /api/v1/employees", () =>
    Response.json({
      items: [
        {
          id: "e2",
          lastName: "Петров",
          firstName: "Пётр",
          middleName: null,
          fullName: EMPLOYEE_CHOICE,
          email: null,
          orgUnitId: "u1",
          orgUnitName: "Команда",
          positionId: "p0",
          position: "Инженер",
        },
      ],
      total: 1,
      page: 1,
      pageSize: 20,
    }),
  );
}

function renderQueue() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter([
          {
            path: "/",
            element: (
              <LeaveGuardProvider>
                <OrgCleanupPage />
              </LeaveGuardProvider>
            ),
          },
        ])}
      />
    </Providers>,
  );
  return userEvent.setup();
}

type User = ReturnType<typeof userEvent.setup>;

/** jsdom plays no animation, so the end of the closing animation, which removes the dialog, is sent until the dialog is gone. */
async function dialogGone() {
  await waitFor(() => {
    const panel = document.querySelector(".ant-modal");
    if (panel) {
      fireEvent.animationEnd(panel);
      fireEvent.transitionEnd(panel);
    }
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
}

async function openFinding(user: User, name: string) {
  await user.click(
    await screen.findByRole("button", { name: new RegExp(name) }),
  );
  await screen.findByRole("heading", { name: new RegExp(name) });
}

async function choosePosition(user: User) {
  await user.click(
    await screen.findByRole("combobox", { name: "Новая должность" }),
  );
  await user.click(
    await screen.findByText(POSITION_CHOICE, {
      selector: ".ant-select-item-option-content",
    }),
  );
}

async function chooseEmployee(user: User) {
  await user.click(await screen.findByRole("combobox", { name: "Сотрудник" }));
  await user.click(
    await screen.findByText(EMPLOYEE_CHOICE, {
      selector: ".option-truncate",
    }),
  );
}

function selectedIn(label: string) {
  const select = screen
    .getByRole("combobox", { name: label })
    .closest(".ant-select");
  if (!(select instanceof HTMLElement)) throw new Error(`No select ${label}`);
  return within(select);
}

async function openFilter(user: User) {
  await user.click(screen.getByRole("combobox", { name: "Вид" }));
  await user.click(
    await screen.findByText(FILTER_LABEL, {
      selector: ".ant-select-item-option-content",
    }),
  );
}

beforeEach(() => {
  queryClient.clear();
});

describe("the queue of the organisation structure asks before a choice not yet saved is dropped (AC-47)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("asks before another finding opens over a new position chosen and not saved, and keeps the choice when the user stays", async () => {
    serve();
    const user = renderQueue();
    await screen.findByRole("heading", { name: /Иванов Иван/ });
    await choosePosition(user);

    await user.click(screen.getByRole("button", { name: /Склад/ }));
    expect(
      await screen.findByText(DIALOG_TITLE, { selector: ".ant-modal-title" }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: STAY }));
    await dialogGone();
    expect(
      screen.getByRole("heading", { name: /Иванов Иван/ }),
    ).toBeInTheDocument();
    expect(
      selectedIn("Новая должность").getByTitle(POSITION_CHOICE),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /Склад/ }));
    await user.click(await screen.findByRole("button", { name: LEAVE }));
    expect(
      await screen.findByRole("heading", { name: /Склад/ }),
    ).toBeInTheDocument();
  });

  it("asks before another finding opens over an employee chosen for an account and not saved, and keeps the choice when the user stays", async () => {
    serve();
    const user = renderQueue();
    await openFinding(user, "lost@test.local");
    await chooseEmployee(user);

    await user.click(screen.getByRole("button", { name: /Склад/ }));

    expect(
      await screen.findByText(DIALOG_TITLE, { selector: ".ant-modal-title" }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: STAY }));
    await dialogGone();
    expect(
      screen.getByRole("heading", { name: /lost@test.local/ }),
    ).toBeInTheDocument();
    expect(
      selectedIn("Сотрудник").getByTitle(EMPLOYEE_CHOICE),
    ).toBeInTheDocument();
  });

  it("asks before the kind filter changes over a choice not saved, and leaves the filter as it was when the user stays", async () => {
    serve();
    const user = renderQueue();
    await screen.findByRole("heading", { name: /Иванов Иван/ });
    await choosePosition(user);

    await openFilter(user);

    expect(
      await screen.findByText(DIALOG_TITLE, { selector: ".ant-modal-title" }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: STAY }));
    await dialogGone();
    expect(
      selectedIn("Новая должность").getByTitle(POSITION_CHOICE),
    ).toBeInTheDocument();
    const narrowed = fakeApi
      .calls(ITEMS_ROUTE)
      .filter(
        (request) =>
          new URL(request.url).searchParams.getAll("group").length === 1,
      );
    expect(narrowed).toHaveLength(0);
  });

  it("applies the filter at once when nothing is left unsaved", async () => {
    serve();
    const user = renderQueue();
    await screen.findByRole("heading", { name: /Иванов Иван/ });

    await openFilter(user);

    expect(
      await screen.findByRole("heading", { name: /Склад/ }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).toBeNull();
  });
});

describe("a unit finding opens where its session can close the gap (AC-8)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it.each([
    {
      role: "Hr",
      label: "Открыть карточку подразделения",
      href: "/org-structure?unit=u2",
    },
    {
      role: "GlobalAdmin",
      label: "Открыть «Подключить подразделение»",
      href: "/desk/connect-unit/u2",
    },
  ])("leads a $role to $href", async ({ role, label, href }) => {
    serve();
    fakeApi.on("GET /api/v1/auth/me", () =>
      Response.json({
        id: "account",
        email: "user@test.local",
        roles: [role],
        employeeId: null,
      }),
    );
    const user = renderQueue();
    await screen.findByRole("heading", { name: /Иванов Иван/ });
    await user.click(screen.getByRole("button", { name: /Склад/ }));

    const link = await screen.findByRole("link", { name: label });

    await waitFor(() => expect(link).toHaveAttribute("href", href));
  });
});
