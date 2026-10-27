import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { MoveEmployeeModal } from "./MoveEmployeeModal";
import type { Employee, EmployeeAssignment } from "./orgStructureApi";

const MOVE_ROUTE = "POST /api/v1/employees/e1/move";
const ENTITY = { id: "le1", name: "ООО Альфа" };

const unit = (
  id: string,
  name: string,
  headEmployeeId: string | null = null,
) => ({
  id,
  name,
  parentId: null,
  headEmployeeId,
  isActive: true,
  version: 1,
});

const secondary: EmployeeAssignment = {
  orgUnitId: "support",
  orgUnitName: "Поддержка",
  positionId: "operator",
  position: "Оператор",
  legalEntities: [],
  isSecondary: true,
};

function employeeOf(assignments: EmployeeAssignment[] = []): Employee {
  return {
    id: "e1",
    lastName: "Иванов",
    firstName: "Иван",
    middleName: null,
    fullName: "Иванов Иван",
    email: null,
    orgUnitId: "sales",
    orgUnitName: "Продажи",
    positionId: "engineer",
    position: "Инженер",
    legalEntities: [ENTITY],
    assignments,
    unitAssignments: [],
    isActive: true,
    version: 7,
  };
}

function serve(
  employee: Employee,
  units = [
    unit("sales", "Продажи"),
    unit("support", "Поддержка"),
    unit("legal", "Юристы"),
  ],
  legalPositions: { positionId: string; name: string }[] = [],
) {
  fakeApi.on("GET /api/v1/employees/e1", () => Response.json(employee));
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json(units));
  fakeApi.on("GET /api/v1/org-units/legal/positions", () =>
    Response.json(
      legalPositions.map((position) => ({
        ...position,
        isActive: true,
        orgUnitId: "legal",
        orgUnitName: "Юристы",
      })),
    ),
  );
  fakeApi.on(MOVE_ROUTE, () => Response.json({ ...employee, version: 8 }));
}

async function open(employee: Employee, onClose = vi.fn()) {
  render(
    <Providers>
      <MoveEmployeeModal employee={employee} onClose={onClose} />
    </Providers>,
  );
  const user = userEvent.setup();
  await screen.findByText("Иванов Иван");
  return { user, onClose };
}

async function chooseUnit(
  user: ReturnType<typeof userEvent.setup>,
  name: string,
) {
  await user.click(
    await screen.findByRole("combobox", { name: "Новое подразделение" }),
  );
  await user.click(
    await screen.findByText(name, { selector: ".ant-select-tree-title" }),
  );
}

async function headshipDialog() {
  const [title] = await screen.findAllByText("Снять с руководства?", {
    selector: ".ant-modal-confirm-title",
  });
  return within(title?.closest('[role="dialog"]') as HTMLElement);
}

beforeEach(() => {
  queryClient.clear();
});

describe("the move of an employee's assignment (AC-28)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("moves the main assignment, naming its legal entities, and warns that the position joins the new unit's list", async () => {
    const employee = employeeOf();
    serve(employee);
    const { user, onClose } = await open(employee);

    expect(
      screen.getByText("Назначение: Инженер, Продажи"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Юрлица назначения сохраняются: ООО Альфа."),
    ).toBeInTheDocument();
    await chooseUnit(user, "Юристы");
    expect(
      await screen.findByText(
        "Должность «Инженер» будет добавлена в перечень подразделения.",
      ),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Переместить" }));

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    const [request] = fakeApi.calls(MOVE_ROUTE);
    expect(request?.headers.get("If-Match")).toBe('"7"');
    expect(await request?.json()).toEqual({
      fromOrgUnitId: "sales",
      positionId: "engineer",
      toOrgUnitId: "legal",
    });
  });

  it("does not warn when the new unit's list already holds the position", async () => {
    const employee = employeeOf();
    serve(employee, undefined, [{ positionId: "engineer", name: "Инженер" }]);
    const { user } = await open(employee);

    await chooseUnit(user, "Юристы");

    await waitFor(() =>
      expect(
        fakeApi.calls("GET /api/v1/org-units/legal/positions"),
      ).toHaveLength(1),
    );
    expect(
      screen.queryByText(/будет добавлена в перечень/),
    ).not.toBeInTheDocument();
  });

  it("offers every assignment the card shows and moves the one chosen, a secondary one included", async () => {
    const employee = employeeOf([secondary]);
    serve(employee);
    const { user, onClose } = await open({
      ...employee,
      unitAssignments: [mainOf(employee), secondary],
    });

    await user.click(
      screen.getByRole("combobox", { name: "Перемещаемое назначение" }),
    );
    await user.click(
      await screen.findByText("Оператор, Поддержка (совмещение)", {
        selector: ".ant-select-item-option-content",
      }),
    );
    await chooseUnit(user, "Юристы");
    await user.click(screen.getByRole("button", { name: "Переместить" }));

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(await fakeApi.calls(MOVE_ROUTE)[0]?.json()).toEqual({
      fromOrgUnitId: "support",
      positionId: "operator",
      toOrgUnitId: "legal",
    });
  });

  it("does not offer the unit the assignment is in as the target", async () => {
    const employee = employeeOf();
    serve(employee);
    const { user } = await open(employee);

    await user.click(
      await screen.findByRole("combobox", { name: "Новое подразделение" }),
    );

    const own = await screen.findByText("Продажи", {
      selector: ".ant-select-tree-title",
    });
    expect(own.closest(".ant-select-tree-treenode")).toHaveClass(
      "ant-select-tree-treenode-disabled",
    );
  });

  it("asks before the move leaves the head of a unit without an assignment there, and moves nothing when declined (AC-3)", async () => {
    const employee = employeeOf();
    serve(employee, [
      unit("sales", "Продажи", "e1"),
      unit("support", "Поддержка"),
      unit("legal", "Юристы"),
    ]);
    const { user, onClose } = await open(employee);

    await chooseUnit(user, "Юристы");
    await user.click(screen.getByRole("button", { name: "Переместить" }));
    const dialog = await headshipDialog();
    expect(
      dialog.getByText(
        "Подразделение «Продажи» останется без руководителя. Продолжить?",
      ),
    ).toBeInTheDocument();
    await user.click(dialog.getByRole("button", { name: "Отмена" }));

    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Переместить" })).toBeEnabled(),
    );
    expect(fakeApi.calls(MOVE_ROUTE)).toHaveLength(0);
    expect(onClose).not.toHaveBeenCalled();
  });

  it("moves after the user confirms leaving the headship", async () => {
    const employee = employeeOf();
    serve(employee, [
      unit("sales", "Продажи", "e1"),
      unit("support", "Поддержка"),
      unit("legal", "Юристы"),
    ]);
    const { user, onClose } = await open(employee);

    await chooseUnit(user, "Юристы");
    await user.click(screen.getByRole("button", { name: "Переместить" }));
    const dialog = await headshipDialog();
    await user.click(dialog.getByRole("button", { name: "Продолжить" }));

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(fakeApi.calls(MOVE_ROUTE)).toHaveLength(1);
  });

  it("does not ask while another assignment of the employee stays in the headed unit", async () => {
    const stays: EmployeeAssignment = {
      ...secondary,
      orgUnitId: "sales",
      orgUnitName: "Продажи",
      positionId: "analyst",
      position: "Аналитик",
    };
    const employee = employeeOf([stays]);
    serve(employee, [
      unit("sales", "Продажи", "e1"),
      unit("support", "Поддержка"),
      unit("legal", "Юристы"),
    ]);
    const { user, onClose } = await open(employee);

    await chooseUnit(user, "Юристы");
    await user.click(screen.getByRole("button", { name: "Переместить" }));

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(
      screen.queryByText("Снять с руководства?", {
        selector: ".ant-modal-confirm-title",
      }),
    ).not.toBeInTheDocument();
  });

  it("shows the reason the server refused the move and keeps the dialog open", async () => {
    const employee = employeeOf();
    serve(employee);
    fakeApi.on(MOVE_ROUTE, () =>
      problem(400, { errors: { toOrgUnitId: ["Подразделение неактивно."] } }),
    );
    const { user, onClose } = await open(employee);

    await chooseUnit(user, "Юристы");
    await user.click(screen.getByRole("button", { name: "Переместить" }));

    expect(
      await screen.findByText("Не удалось переместить сотрудника"),
    ).toBeInTheDocument();
    expect(screen.getByText("Подразделение неактивно.")).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });

  it("asks for a unit before it sends anything", async () => {
    const employee = employeeOf();
    serve(employee);
    const { user } = await open(employee);

    await user.click(screen.getByRole("button", { name: "Переместить" }));

    expect(
      await screen.findByText("Выберите подразделение."),
    ).toBeInTheDocument();
    expect(fakeApi.calls(MOVE_ROUTE)).toHaveLength(0);
  });
});

function mainOf(employee: Employee): EmployeeAssignment {
  return {
    orgUnitId: employee.orgUnitId,
    orgUnitName: employee.orgUnitName,
    positionId: employee.positionId,
    position: employee.position,
    legalEntities: employee.legalEntities,
    isSecondary: false,
  };
}
