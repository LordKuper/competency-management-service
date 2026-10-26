import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
import { UserModal } from "./UserModal";

const USER_ID = "user-1";
const REFUSAL = "HR отвечает за подразделения: сначала передайте их другому";
const SAVE_FAILED = "Не удалось сохранить пользователя";
const CREATE_ROUTE = "POST /api/v1/users";
const SAVE_ROUTE = `PUT /api/v1/users/${USER_ID}`;
const HR_ACCOUNT = {
  id: USER_ID,
  email: "hr@test.local",
  roles: ["Hr"],
  isBlocked: false,
  isInvited: false,
  employeeId: null,
  employeeName: null,
  version: 4,
  mailSent: null,
};

const noop = () => undefined;

async function openModal(userId?: string) {
  fakeApi.on("GET /api/v1/employees", () =>
    Response.json({ items: [], total: 0, page: 1, pageSize: 20 }),
  );
  render(
    <Providers>
      <UserModal userId={userId} onClose={noop} />
    </Providers>,
  );
  return within(await screen.findByRole("dialog"));
}

async function openAccount() {
  fakeApi.on(`GET /api/v1/users/${USER_ID}`, () => Response.json(HR_ACCOUNT));
  return openModal(USER_ID);
}

async function openNewAccount() {
  const dialog = await openModal();
  await userEvent.type(dialog.getByLabelText("E-mail"), "new@test.local");
  return dialog;
}

function lockedFields(dialog: ReturnType<typeof within>) {
  const fields: [string, HTMLElement][] = [
    ["e-mail", dialog.getByLabelText("E-mail")],
    ["roles", dialog.getByRole("combobox", { name: "Роли" })],
    ["employee", dialog.getByRole("combobox", { name: "Сотрудник" })],
  ];
  return fields
    .filter(([, field]) => field.hasAttribute("disabled"))
    .map(([kind]) => kind);
}

async function removeHrRole(dialog: ReturnType<typeof within>) {
  const tag = (await dialog.findByText("HR")).closest(
    ".ant-select-selection-item",
  );
  await userEvent.click(
    (tag as HTMLElement).querySelector(
      ".ant-select-selection-item-remove",
    ) as HTMLElement,
  );
}

beforeEach(() => {
  queryClient.clear();
});

describe("removing the HR role from an account (user-management AC-50, AC-51)", () => {
  it("shows the refusal under the roles, keeps the roles as the administrator left them and adds no alert", async () => {
    fakeApi.on(`PUT /api/v1/users/${USER_ID}`, () =>
      problem(409, { detail: REFUSAL }),
    );
    const dialog = await openAccount();
    await dialog.findByText("HR");

    await removeHrRole(dialog);
    await userEvent.click(dialog.getByRole("button", { name: "Сохранить" }));

    expect(await dialog.findByText(REFUSAL)).toBeInTheDocument();
    expect(dialog.queryByText("HR")).not.toBeInTheDocument();
    expect(dialog.queryByText(SAVE_FAILED)).not.toBeInTheDocument();
    expect(
      await fakeApi.calls(`PUT /api/v1/users/${USER_ID}`)[0]?.json(),
    ).toMatchObject({ roles: [] });
  });

  it("leaves a conflict that names a field to that field, and any other failure to the alert", async () => {
    fakeApi.on(`PUT /api/v1/users/${USER_ID}`, () =>
      problem(409, { detail: REFUSAL, errors: { email: ["E-mail занят"] } }),
    );
    const dialog = await openAccount();
    await removeHrRole(dialog);

    await userEvent.click(dialog.getByRole("button", { name: "Сохранить" }));

    expect(await dialog.findByText("E-mail занят")).toBeInTheDocument();
    expect(dialog.queryByText(REFUSAL)).not.toBeInTheDocument();
    expect(dialog.queryByText(SAVE_FAILED)).not.toBeInTheDocument();
    fakeApi.on(`PUT /api/v1/users/${USER_ID}`, () =>
      problem(500, { detail: REFUSAL }),
    );

    await userEvent.click(dialog.getByRole("button", { name: "Сохранить" }));

    expect(await dialog.findByText(SAVE_FAILED)).toBeInTheDocument();
    expect(dialog.queryByText(REFUSAL)).not.toBeInTheDocument();
  });

  it("shows the same refusal as a general alert when the HR role is not being removed", async () => {
    fakeApi.on(`PUT /api/v1/users/${USER_ID}`, () =>
      problem(409, { detail: REFUSAL }),
    );
    const dialog = await openAccount();
    await dialog.findByText("HR");

    await userEvent.click(dialog.getByRole("button", { name: "Сохранить" }));

    await waitFor(() =>
      expect(fakeApi.calls(`PUT /api/v1/users/${USER_ID}`)).toHaveLength(1),
    );
    expect(await dialog.findByText(SAVE_FAILED)).toBeInTheDocument();
  });
});

describe("the account form is held while the account is being saved (AC-20)", () => {
  it.each([
    {
      what: "creates",
      route: CREATE_ROUTE,
      submit: "Создать",
      open: openNewAccount,
    },
    {
      what: "changes",
      route: SAVE_ROUTE,
      submit: "Сохранить",
      open: openAccount,
    },
  ])(
    "holds the e-mail, the roles and the employee while the form $what the account, and frees them when the server refuses",
    async ({ route, submit, open }) => {
      const held = deferred<Response>();
      fakeApi.on(route, () => held.promise);
      const dialog = await open();
      await dialog.findByLabelText("E-mail");
      expect(lockedFields(dialog)).toEqual([]);

      await userEvent.click(dialog.getByRole("button", { name: submit }));

      await waitFor(() => expect(fakeApi.calls(route)).toHaveLength(1));
      expect(lockedFields(dialog)).toEqual(["e-mail", "roles", "employee"]);
      held.resolve(problem(500));
      expect(await dialog.findByText(SAVE_FAILED)).toBeInTheDocument();
      await waitFor(() => expect(lockedFields(dialog)).toEqual([]));
    },
  );
});
