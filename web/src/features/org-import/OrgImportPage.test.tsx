import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, Link, Outlet, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { LeaveGuardProvider } from "../../app/leaveGuard";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import {
  headUnit,
  missingEmployee,
  missingUnit,
  planOf,
} from "../../test/orgImportPlan";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import {
  formParts,
  nodeFile,
  partText,
  useNodeFormData,
} from "../../test/uploadForm";
import { OrgImportPage } from "./OrgImportPage";
import type { OrgImportPlan } from "./orgImportApi";

const PREVIEW_ROUTE = "POST /api/v1/org-import/preview";
const APPLY_ROUTE = "POST /api/v1/org-import/apply";
const NEXT = "Далее";
const APPLY = "Применить";

const EMPTY_RESULT = {
  newUnits: 0,
  activatedUnits: 0,
  newPositions: 0,
  reactivatedPositions: 0,
  newLegalEntities: 0,
  newEmployees: 0,
  rehiredEmployees: 0,
  transfers: 0,
  addedAssignments: 0,
  removedAssignments: 0,
  changedLegalEntities: 0,
  unitPositionsAdded: 0,
  unitPositionsRemoved: 0,
  deactivatedUnits: 0,
  dismissedEmployees: 0,
  assignedHeads: 0,
};

function fullPlan(fingerprint: string, overrides: Partial<OrgImportPlan> = {}) {
  return planOf({
    fingerprint,
    newUnits: ["Новый отдел"],
    missingUnits: [missingUnit("u1")],
    missingEmployees: [missingEmployee("e1", ["u1"]), missingEmployee("e2")],
    heads: {
      units: [headUnit("h-key", { candidates: ["n1"], employees: ["n1"] })],
      employees: [{ key: "n1", fullName: "Новый Начальник" }],
    },
    ...overrides,
  });
}

function servePreview(plans: OrgImportPlan[]) {
  let served = 0;
  fakeApi.on(PREVIEW_ROUTE, () =>
    Response.json({
      errors: [],
      plan: plans[Math.min(served++, plans.length - 1)],
    }),
  );
}

function renderPage() {
  const router = createMemoryRouter(
    [
      {
        element: (
          <LeaveGuardProvider>
            <Outlet />
            <Link to="/other">Уйти</Link>
          </LeaveGuardProvider>
        ),
        children: [
          { path: "/desk/org-import", element: <OrgImportPage /> },
          { path: "/other", element: <p>Другая страница</p> },
        ],
      },
    ],
    { initialEntries: ["/desk/org-import"] },
  );
  render(
    <Providers>
      <RouterProvider router={router} />
    </Providers>,
  );
  return { router, user: userEvent.setup() };
}

type User = ReturnType<typeof userEvent.setup>;

async function upload(user: User) {
  await screen.findByText("Файл ещё не выбран.");
  const input = document.querySelector<HTMLInputElement>('input[type="file"]');
  if (!input) throw new Error("the upload input is missing");
  await user.upload(
    input,
    nodeFile(
      "содержимое",
      "org.xlsx",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    ),
  );
}

async function next(user: User) {
  await user.click(await screen.findByRole("button", { name: NEXT }));
}

function rowOf(text: string) {
  const row = screen.getAllByText(text)[0]?.closest("tr");
  if (!row) throw new Error(`no row holds «${text}»`);
  return within(row);
}

async function sentForm(route: string, index = 0) {
  const request = fakeApi.calls(route)[index];
  if (!request) throw new Error(`no request reached ${route}`);
  return formParts(request);
}

beforeEach(() => {
  useNodeFormData();
  queryClient.clear();
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "a@test.local", roles: ["GlobalAdmin"] }),
  );
});

describe("the import wizard", () => {
  it("lists every problem of a file with its row and column, and does not go on (AC-13)", async () => {
    fakeApi.on(PREVIEW_ROUTE, () =>
      Response.json({
        errors: [
          { row: 3, column: "ФИО", message: "Укажите фамилию и имя" },
          { row: null, column: null, message: "Файл повреждён" },
        ],
        plan: null,
      }),
    );
    const { user } = renderPage();

    await upload(user);

    expect(await screen.findByText("Файл не принят")).toBeInTheDocument();
    expect(screen.getByText("Укажите фамилию и имя")).toBeInTheDocument();
    expect(rowOf("Укажите фамилию и имя").getByText("3")).toBeInTheDocument();
    expect(rowOf("Укажите фамилию и имя").getByText("ФИО")).toBeInTheDocument();
    expect(rowOf("Файл повреждён").getAllByText("—")).toHaveLength(2);
    expect(screen.getByRole("button", { name: NEXT })).toBeDisabled();
  });

  it(
    "walks from the file to the result and sends one decision for everything missing and a head for every unit (AC-18, AC-31)",
    async () => {
      servePreview([fullPlan("fp-1")]);
      fakeApi.on(APPLY_ROUTE, () =>
        Response.json({ ...EMPTY_RESULT, newUnits: 1, assignedHeads: 1 }),
      );
      const { user } = renderPage();

      await upload(user);
      expect(await screen.findByText(/Изменений: 1/)).toBeInTheDocument();
      await next(user);
      await user.click(
        within(rowOf("Сотрудник e2").getByRole("radiogroup")).getByText(
          "Оставить",
        ),
      );
      await next(user);
      await user.click(await screen.findByRole("button", { name: APPLY }));

      await screen.findByText("Импорт применён.");
      const form = await sentForm(APPLY_ROUTE);
      expect(partText(form, "fingerprint")).toBe("fp-1");
      expect(form.find((part) => part.name === "file")?.fileName).toBe(
        "org.xlsx",
      );
      expect(JSON.parse(partText(form, "decisions"))).toEqual([
        { id: "u1", decision: "deactivate" },
        { id: "e1", decision: "dismiss" },
        { id: "e2", decision: "keep" },
      ]);
      expect(JSON.parse(partText(form, "heads"))).toEqual([
        { unit: "h-key", employee: "n1" },
      ]);
      expect(rowOf("Новые подразделения").getByText("1")).toBeInTheDocument();
      expect(
        rowOf("Назначенные руководители").getByText("1"),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole("button", { name: APPLY }),
      ).not.toBeInTheDocument();
    },
    LONG_FLOW_TIMEOUT,
  );

  it(
    "marks a deactivation the rules refuse and holds the next step until it is fixed (AC-18)",
    async () => {
      servePreview([fullPlan("fp-1")]);
      const { user } = renderPage();
      await upload(user);
      await next(user);

      await user.click(
        within(rowOf("Сотрудник e1").getByRole("radiogroup")).getByText(
          "Оставить",
        ),
      );

      expect(await screen.findByText("Нельзя применить")).toBeInTheDocument();
      expect(screen.getByRole("button", { name: NEXT })).toBeDisabled();
      await user.click(
        within(rowOf("Путь u1").getByRole("radiogroup")).getByText("Оставить"),
      );
      await waitFor(() =>
        expect(screen.getByRole("button", { name: NEXT })).toBeEnabled(),
      );
      expect(screen.queryByText("Нельзя применить")).not.toBeInTheDocument();
    },
    LONG_FLOW_TIMEOUT,
  );

  it(
    "keeps the last active administrator whatever the user chose (AC-17)",
    async () => {
      servePreview([
        fullPlan("fp-1", {
          missingUnits: [],
          missingEmployees: [missingEmployee("admin", [], true)],
          heads: { units: [], employees: [] },
        }),
      ]);
      fakeApi.on(APPLY_ROUTE, () => Response.json(EMPTY_RESULT));
      const { user } = renderPage();
      await upload(user);
      await next(user);

      expect(
        await screen.findByText("Последний администратор"),
      ).toBeInTheDocument();
      expect(
        within(rowOf("Сотрудник admin").getByRole("radiogroup")).getByLabelText(
          "Уволить",
        ),
      ).toBeDisabled();
      await next(user);
      await user.click(await screen.findByRole("button", { name: APPLY }));

      await screen.findByText("Импорт применён.");
      expect(
        JSON.parse(partText(await sentForm(APPLY_ROUTE), "decisions")),
      ).toEqual([{ id: "admin", decision: "keep" }]);
    },
    LONG_FLOW_TIMEOUT,
  );

  it(
    "reads the file again when the plan changed, warns, and keeps the decisions that still apply (AC-20)",
    async () => {
      servePreview([
        fullPlan("fp-1"),
        fullPlan("fp-2", {
          missingUnits: [],
          missingEmployees: [missingEmployee("e2")],
        }),
      ]);
      let applies = 0;
      fakeApi.on(APPLY_ROUTE, () =>
        applies++ === 0
          ? problem(409, { code: "planChanged" })
          : Response.json(EMPTY_RESULT),
      );
      const { user } = renderPage();
      await upload(user);
      await next(user);
      await user.click(
        within(rowOf("Сотрудник e2").getByRole("radiogroup")).getByText(
          "Оставить",
        ),
      );
      await next(user);

      await user.click(await screen.findByRole("button", { name: APPLY }));

      expect(
        await screen.findByText("План изменился после предпросмотра."),
      ).toBeInTheDocument();
      expect(await screen.findByText(/Изменений: 1/)).toBeInTheDocument();
      expect(fakeApi.calls(PREVIEW_ROUTE)).toHaveLength(2);
      await next(user);
      expect(
        within(rowOf("Сотрудник e2").getByRole("radiogroup")).getByLabelText(
          "Оставить",
        ),
      ).toBeChecked();
      await next(user);
      await user.click(await screen.findByRole("button", { name: APPLY }));
      await screen.findByText("Импорт применён.");
      const form = await sentForm(APPLY_ROUTE, 1);
      expect(partText(form, "fingerprint")).toBe("fp-2");
      expect(JSON.parse(partText(form, "decisions"))).toEqual([
        { id: "e2", decision: "keep" },
      ]);
    },
    LONG_FLOW_TIMEOUT,
  );

  it(
    "shows the reason of a refused apply and stays on the step (AC-18)",
    async () => {
      servePreview([fullPlan("fp-1")]);
      fakeApi.on(APPLY_ROUTE, () =>
        problem(400, {
          errors: { decisions: ["Решение по одной записи указано дважды."] },
        }),
      );
      const { user } = renderPage();
      await upload(user);
      await next(user);
      await next(user);

      await user.click(await screen.findByRole("button", { name: APPLY }));

      expect(await screen.findByText("Импорт не применён")).toBeInTheDocument();
      expect(
        screen.getByText("Решение по одной записи указано дважды."),
      ).toBeInTheDocument();
      expect(screen.getByRole("button", { name: APPLY })).toBeEnabled();
    },
    LONG_FLOW_TIMEOUT,
  );

  it(
    "asks before the user leaves a loaded plan, and lets them stay (AC-22)",
    async () => {
      servePreview([fullPlan("fp-1")]);
      const { router, user } = renderPage();
      await upload(user);
      await screen.findByText(/Изменений: 1/);

      await user.click(screen.getByRole("link", { name: "Уйти" }));

      expect(
        (await screen.findAllByText("Перейти без сохранения?")).length,
      ).toBeGreaterThan(0);
      await user.click(screen.getByRole("button", { name: "Остаться" }));
      expect(router.state.location.pathname).toBe("/desk/org-import");
    },
    LONG_FLOW_TIMEOUT,
  );

  it("lets the user leave an empty wizard without asking (AC-22)", async () => {
    const { router, user } = renderPage();
    await screen.findByText("Файл ещё не выбран.");

    await user.click(screen.getByRole("link", { name: "Уйти" }));

    await waitFor(() => expect(router.state.location.pathname).toBe("/other"));
  });
});
