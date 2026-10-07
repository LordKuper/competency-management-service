import { useQuery } from "@tanstack/react-query";
import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement } from "react";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../api/queryClient";
import { EmployeeModal } from "../features/org-structure/EmployeeModal";
import { orgUnitTreeQuery } from "../features/org-structure/orgStructureApi";
import { UnitFormModal } from "../features/org-structure/UnitFormModal";
import { UserModal } from "../features/users/UserModal";
import { fakeApi, json, problem } from "../test/fakeApi";
import { Providers } from "./Providers";

const OPENED = "Исходное значение";
const OTHERS = "Правка другого пользователя";
const MINE = "Моя правка";
const SAVE = "Сохранить";
const EMPLOYEE_PAGE = { items: [], total: 0, page: 1, pageSize: 20 };

const noop = () => undefined;

function UnitDialog() {
  const { data } = useQuery(orgUnitTreeQuery);
  const unit = data?.find((candidate) => candidate.id === "unit-1");
  return unit ? (
    <UnitFormModal unit={unit} onClose={noop} onSaved={noop} />
  ) : null;
}

interface Dialog {
  name: string;
  readRoute: string;
  saveRoute: string;
  field: string;
  bodyField: string;
  detailKey?: readonly string[];
  answer: (version: number, text: string) => unknown;
  serveOthers: () => void;
  element: () => ReactElement;
}

type ReadingDialog = Dialog & { detailKey: readonly string[] };

function readsItsRecord(dialog: Dialog): dialog is ReadingDialog {
  return dialog.detailKey !== undefined;
}

const dialogs: Dialog[] = [
  {
    name: "account dialog",
    readRoute: "GET /api/v1/users/user-1",
    saveRoute: "PUT /api/v1/users/user-1",
    field: "E-mail",
    bodyField: "email",
    detailKey: ["users", "detail", "user-1"],
    answer: (version, text) => ({
      id: "user-1",
      email: text,
      role: "User",
      isBlocked: false,
      employeeId: null,
      employeeName: null,
      version,
    }),
    serveOthers: () =>
      fakeApi.on("GET /api/v1/employees", () => json(EMPLOYEE_PAGE)),
    element: () => <UserModal userId="user-1" onClose={noop} />,
  },
  {
    name: "employee dialog",
    readRoute: "GET /api/v1/employees/employee-1",
    saveRoute: "PUT /api/v1/employees/employee-1",
    field: "Фамилия",
    bodyField: "lastName",
    detailKey: ["employees", "detail", "employee-1"],
    answer: (version, text) => ({
      id: "employee-1",
      lastName: text,
      firstName: "Иван",
      middleName: null,
      fullName: `${text} Иван`,
      email: null,
      orgUnitId: "unit-1",
      orgUnitName: "Отдел",
      position: "Инженер",
      isActive: true,
      version,
    }),
    serveOthers: () => fakeApi.on("GET /api/v1/org-units/tree", () => json([])),
    element: () => (
      <EmployeeModal employeeId="employee-1" onClose={noop} onSaved={noop} />
    ),
  },
  {
    name: "unit dialog",
    readRoute: "GET /api/v1/org-units/tree",
    saveRoute: "PUT /api/v1/org-units/unit-1",
    field: "Название",
    bodyField: "name",
    answer: (version, text) => [
      {
        id: "unit-1",
        name: text,
        parentId: null,
        headEmployeeId: null,
        isActive: true,
        version,
        headName: null,
        employeeCount: 0,
      },
    ],
    serveOthers: () =>
      fakeApi.on("GET /api/v1/employees", () => json(EMPLOYEE_PAGE)),
    element: () => <UnitDialog />,
  },
];

async function openDialog(dialog: Dialog, answerSave: () => Response) {
  const server = { version: 1, text: OPENED };
  fakeApi.on(dialog.readRoute, () =>
    json(dialog.answer(server.version, server.text)),
  );
  fakeApi.on(dialog.saveRoute, answerSave);
  dialog.serveOthers();
  render(<Providers>{dialog.element()}</Providers>);
  const field = () => screen.getByLabelText(dialog.field);
  await waitFor(() => expect(field()).toHaveValue(OPENED));
  const user = userEvent.setup();
  await user.clear(field());
  await user.type(field(), MINE);

  async function otherUserSavesInTheMeantime() {
    server.version = 2;
    server.text = OTHERS;
    await act(() => queryClient.invalidateQueries());
    await user.type(field(), "!");
  }

  async function save() {
    await user.click(screen.getByRole("button", { name: SAVE }));
  }

  return { field, user, server, otherUserSavesInTheMeantime, save };
}

const saveBody = async (dialog: Dialog, index: number) =>
  (await fakeApi.calls(dialog.saveRoute)[index]?.json()) as Record<
    string,
    unknown
  >;

beforeEach(() => {
  queryClient.clear();
});

describe.each(dialogs)("$name (AC-13, AC-17)", (dialog) => {
  it("keeps the unsaved input and saves against the opening version when a background refetch brings a newer one; after a stale refusal it takes the fresh record", async () => {
    const opened = await openDialog(dialog, () => problem(412));

    await opened.otherUserSavesInTheMeantime();
    expect(opened.field()).toHaveValue(`${MINE}!`);
    await opened.save();

    await waitFor(() =>
      expect(fakeApi.calls(dialog.saveRoute)).toHaveLength(1),
    );
    expect(fakeApi.calls(dialog.saveRoute)[0]?.headers.get("If-Match")).toBe(
      '"1"',
    );
    expect((await saveBody(dialog, 0))[dialog.bodyField]).toBe(`${MINE}!`);
    await waitFor(() => expect(opened.field()).toHaveValue(OTHERS));
    fakeApi.on(dialog.saveRoute, () =>
      json(dialog.answer(opened.server.version, opened.server.text)),
    );
    await opened.save();

    await waitFor(() =>
      expect(fakeApi.calls(dialog.saveRoute)).toHaveLength(2),
    );
    expect(fakeApi.calls(dialog.saveRoute)[1]?.headers.get("If-Match")).toBe(
      '"2"',
    );
    expect((await saveBody(dialog, 1))[dialog.bodyField]).toBe(OTHERS);
  });

  it("keeps the opening record and its version after a conflict, which is not a stale refusal", async () => {
    const opened = await openDialog(dialog, () =>
      problem(409, { title: "Конфликт" }),
    );
    await opened.otherUserSavesInTheMeantime();
    await opened.save();
    await waitFor(() =>
      expect(fakeApi.calls(dialog.saveRoute)).toHaveLength(1),
    );
    await waitFor(() =>
      expect(fakeApi.calls(dialog.readRoute)).toHaveLength(3),
    );

    await opened.user.type(opened.field(), "?");
    expect(opened.field()).toHaveValue(`${MINE}!?`);
    await opened.save();

    await waitFor(() =>
      expect(fakeApi.calls(dialog.saveRoute)).toHaveLength(2),
    );
    expect(fakeApi.calls(dialog.saveRoute)[1]?.headers.get("If-Match")).toBe(
      '"1"',
    );
  });

  it("shows the server's message under the field it names and lets the user save again", async () => {
    const message = "Сервер не принял значение.";
    const opened = await openDialog(dialog, () =>
      problem(400, { errors: { [dialog.bodyField]: [message] } }),
    );

    await opened.save();

    expect(await screen.findByText(message)).toBeInTheDocument();
    await opened.save();
    await waitFor(() =>
      expect(fakeApi.calls(dialog.saveRoute)).toHaveLength(2),
    );
  });
});

describe.each(dialogs.filter(readsItsRecord))(
  "$name opened again (AC-13, AC-17)",
  (dialog) => {
    it("reads the record afresh instead of starting from the version the previous opening cached", async () => {
      const server = { version: 1, text: OPENED };
      fakeApi.on(dialog.readRoute, () =>
        json(dialog.answer(server.version, server.text)),
      );
      dialog.serveOthers();
      const first = render(<Providers>{dialog.element()}</Providers>);
      await waitFor(() =>
        expect(screen.getByLabelText(dialog.field)).toHaveValue(OPENED),
      );
      first.unmount();
      await waitFor(() =>
        expect(
          queryClient.getQueryCache().find({ queryKey: dialog.detailKey }),
        ).toBeUndefined(),
      );
      server.version = 2;
      server.text = OTHERS;

      render(<Providers>{dialog.element()}</Providers>);

      await waitFor(() =>
        expect(screen.getByLabelText(dialog.field)).toHaveValue(OTHERS),
      );
    });
  },
);
