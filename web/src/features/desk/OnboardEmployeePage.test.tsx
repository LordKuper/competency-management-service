import { focusManager, onlineManager } from "@tanstack/react-query";
import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { OnboardEmployeePage } from "./OnboardEmployeePage";

const EMPLOYEE_ROUTE = "GET /api/v1/employees/e1";
const CREATE_EMPLOYEE_ROUTE = "POST /api/v1/employees";
const UPDATE_EMPLOYEE_ROUTE = "PUT /api/v1/employees/e1";
const CREATE_ACCOUNT_ROUTE = "POST /api/v1/users";
const CREATE_LABEL = "Создать сотрудника и продолжить";
const SAVE_LABEL = "Сохранить и далее";
const ACCOUNT_LABEL = "Создать учётную запись и отправить приглашение";
const UNIT = { id: "u1", name: "Команда" };
const POSITION = { id: "p1", name: "Инженер" };
const POSITIONS_ROUTE = `GET /api/v1/org-units/${UNIT.id}/positions`;

function employee(version: number, lastName: string) {
  return {
    id: "e1",
    lastName,
    firstName: "Иван",
    middleName: null,
    fullName: `${lastName} Иван`,
    email: null,
    orgUnitId: UNIT.id,
    orgUnitName: UNIT.name,
    positionId: POSITION.id,
    position: POSITION.name,
    isActive: true,
    version,
  };
}

function serve() {
  let stored = employee(1, "Иванов");
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "a@test.local", roles: ["GlobalAdmin"] }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () =>
    Response.json([
      {
        id: UNIT.id,
        name: UNIT.name,
        parentId: null,
        headEmployeeId: null,
        isActive: true,
        version: 1,
      },
    ]),
  );
  fakeApi.on(`GET /api/v1/positions/${POSITION.id}`, () =>
    Response.json({ id: POSITION.id, name: POSITION.name, isActive: true }),
  );
  fakeApi.on(`GET /api/v1/org-units/${UNIT.id}/positions`, () =>
    Response.json([
      {
        positionId: POSITION.id,
        name: POSITION.name,
        isActive: true,
        orgUnitId: UNIT.id,
        orgUnitName: UNIT.name,
      },
    ]),
  );
  fakeApi.on(CREATE_EMPLOYEE_ROUTE, () => Response.json(stored));
  fakeApi.on(EMPLOYEE_ROUTE, () => Response.json(stored));
  return {
    changeBehindTheBack: (lastName: string) => {
      stored = employee(stored.version + 1, lastName);
    },
    saveAs: (lastName: string) => {
      stored = employee(stored.version + 1, lastName);
      return stored;
    },
  };
}

function renderPage() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [
            {
              path: "/desk/onboard-employee",
              element: <OnboardEmployeePage />,
            },
          ],
          {
            initialEntries: [
              `/desk/onboard-employee?unitId=${UNIT.id}&positionId=${POSITION.id}`,
            ],
          },
        )}
      />
    </Providers>,
  );
  return userEvent.setup();
}

type User = ReturnType<typeof userEvent.setup>;

async function createEmployee(user: User) {
  await user.type(await screen.findByLabelText("Фамилия"), "Иванов");
  await user.type(screen.getByLabelText("Имя"), "Иван");
  await user.click(screen.getByRole("button", { name: CREATE_LABEL }));
  await screen.findByText("Сотрудник создан:");
}

async function backToTheEmployee(user: User) {
  await user.click(screen.getByRole("button", { name: "Назад" }));
  await screen.findByRole("button", { name: SAVE_LABEL });
}

async function refuseTheSecondSave(user: User, card: ReturnType<typeof serve>) {
  await createEmployee(user);
  await backToTheEmployee(user);
  fakeApi.on(UPDATE_EMPLOYEE_ROUTE, () => {
    card.changeBehindTheBack("Сидоров");
    return problem(412);
  });
  await user.type(screen.getByLabelText("Фамилия"), "ов");
  await user.click(screen.getByRole("button", { name: SAVE_LABEL }));
}

function stepRuler() {
  return Array.from(document.querySelectorAll(".ant-steps-item"));
}

function surname() {
  return screen.getByLabelText("Фамилия");
}

const TAB_RETURN = {
  cause: "the user returns to the browser tab",
  refresh: () => {
    focusManager.setFocused(false);
    focusManager.setFocused(true);
  },
};
const NETWORK_RETURN = {
  cause: "the network comes back",
  refresh: () => {
    onlineManager.setOnline(false);
    onlineManager.setOnline(true);
  },
};

async function refreshInTheBackground(refresh: () => void) {
  await waitFor(() => expect(queryClient.isFetching()).toBe(0));
  const reads = fakeApi.calls(POSITIONS_ROUTE).length;
  await act(async () => refresh());
  await waitFor(() =>
    expect(
      fakeApi.calls(POSITIONS_ROUTE).length,
      "the refresh must reach the cache: the positions of the unit are reread",
    ).toBeGreaterThan(reads),
  );
  await waitFor(() => expect(queryClient.isFetching()).toBe(0));
}

beforeEach(() => {
  queryClient.clear();
  focusManager.setFocused(undefined);
  onlineManager.setOnline(true);
});

describe("the scenario that takes an employee on", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("offers «Отмена» on the first step only until the employee exists (AC-86)", async () => {
    serve();
    const user = renderPage();
    expect(
      await screen.findByRole("button", { name: "Отмена" }),
    ).toBeInTheDocument();

    await createEmployee(user);
    await backToTheEmployee(user);

    expect(screen.queryByRole("button", { name: "Отмена" })).toBeNull();
  });

  it("rereads the employee after a refused save and shows what the card holds now (AC-86)", async () => {
    const card = serve();
    const user = renderPage();

    await refuseTheSecondSave(user, card);

    await waitFor(() =>
      expect(screen.getByLabelText("Фамилия")).toHaveValue("Сидоров"),
    );
    expect(
      screen.getByText("Карточка сотрудника не обновлена"),
    ).toBeInTheDocument();
  });

  it("sends the version it reread with the save that follows a refused one (AC-86)", async () => {
    const card = serve();
    const user = renderPage();
    await refuseTheSecondSave(user, card);
    await waitFor(() =>
      expect(screen.getByLabelText("Фамилия")).toHaveValue("Сидоров"),
    );
    fakeApi.on(UPDATE_EMPLOYEE_ROUTE, () =>
      Response.json(card.saveAs("Кузнецов")),
    );

    await user.clear(screen.getByLabelText("Фамилия"));
    await user.type(screen.getByLabelText("Фамилия"), "Кузнецов");
    await user.click(screen.getByRole("button", { name: SAVE_LABEL }));

    await waitFor(() =>
      expect(fakeApi.calls(UPDATE_EMPLOYEE_ROUTE)).toHaveLength(2),
    );
    const [refused, accepted] = fakeApi.calls(UPDATE_EMPLOYEE_ROUTE);
    expect(refused?.headers.get("If-Match")).toBe('"1"');
    expect(accepted?.headers.get("If-Match")).toBe('"2"');
  });

  it.each([TAB_RETURN, NETWORK_RETURN])(
    "keeps what was typed on the card of the created employee when $cause and the card has changed behind the back (AC-61)",
    async ({ refresh }) => {
      const card = serve();
      const user = renderPage();
      await createEmployee(user);
      await backToTheEmployee(user);
      await user.type(surname(), "ов");
      card.changeBehindTheBack("Сидоров");

      await refreshInTheBackground(refresh);

      await user.type(surname(), "а");
      expect(surname()).toHaveValue("Ивановова");
      fakeApi.on(UPDATE_EMPLOYEE_ROUTE, () =>
        Response.json(card.saveAs("Ивановова")),
      );
      await user.click(screen.getByRole("button", { name: SAVE_LABEL }));
      await screen.findByLabelText("E-mail");
      const [saved] = fakeApi.calls(UPDATE_EMPLOYEE_ROUTE);
      expect(saved?.headers.get("If-Match")).toBe('"1"');
      expect(await saved?.json()).toMatchObject({ lastName: "Ивановова" });
    },
  );

  it("goes back to leaving the card alone once the reread after a refused save is on screen (AC-61)", async () => {
    const card = serve();
    const user = renderPage();
    await refuseTheSecondSave(user, card);
    await waitFor(() => expect(surname()).toHaveValue("Сидоров"));
    await user.type(surname(), "а");
    card.changeBehindTheBack("Петров");

    await refreshInTheBackground(TAB_RETURN.refresh);

    await user.type(surname(), "в");
    expect(surname()).toHaveValue("Сидоровав");
  });

  it("keeps what was typed when the save fails for another reason than a stale card, and rereads nothing (AC-61)", async () => {
    const card = serve();
    const user = renderPage();
    await createEmployee(user);
    await backToTheEmployee(user);
    fakeApi.on(UPDATE_EMPLOYEE_ROUTE, () => {
      card.changeBehindTheBack("Сидоров");
      return problem(500);
    });
    await user.type(surname(), "ов");

    await user.click(screen.getByRole("button", { name: SAVE_LABEL }));

    expect(
      await screen.findByText("Карточка сотрудника не обновлена"),
    ).toBeInTheDocument();
    await waitFor(() => expect(queryClient.isFetching()).toBe(0));
    await user.type(surname(), "а");
    expect(surname()).toHaveValue("Ивановова");
  });

  it("holds the steps, «Назад», «Пропустить» and the e-mail while the account is being created (AC-187)", async () => {
    serve();
    const created = deferred<Response>();
    fakeApi.on(CREATE_ACCOUNT_ROUTE, () => created.promise);
    const user = renderPage();
    await createEmployee(user);
    await user.type(await screen.findByLabelText("E-mail"), "new@test.local");

    await user.click(screen.getByRole("button", { name: ACCOUNT_LABEL }));

    await waitFor(() =>
      expect(fakeApi.calls(CREATE_ACCOUNT_ROUTE)).toHaveLength(1),
    );
    expect(screen.getByRole("button", { name: "Назад" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Пропустить" })).toBeDisabled();
    expect(screen.getByLabelText("E-mail")).toBeDisabled();
    expect(
      stepRuler().filter((step) =>
        step.classList.contains("ant-steps-item-disabled"),
      ),
    ).toHaveLength(3);
    created.resolve(Response.json({ id: "acc", mailSent: true }));
    await waitFor(() =>
      expect(
        screen.queryByRole("button", { name: ACCOUNT_LABEL }),
      ).not.toBeInTheDocument(),
    );
  });
});
