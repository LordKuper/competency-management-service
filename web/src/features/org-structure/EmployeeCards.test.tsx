import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { shownAssignments } from "./EmployeeCard";
import { EmployeeCards } from "./EmployeeCards";
import type { Employee } from "./orgStructureApi";

function employee(id: string, fullName: string): Employee {
  const [lastName = "", firstName = ""] = fullName.split(" ");
  return {
    id,
    lastName,
    firstName,
    middleName: null,
    education: null,
    fullName,
    email: null,
    orgUnitId: "unit",
    orgUnitName: "Отдел",
    positionId: "position",
    position: "Инженер",
    legalEntities: [],
    assignments: [],
    unitAssignments: [],
  };
}

function namesInOrder(
  employees: Employee[],
  headEmployeeId: string | null,
): string[] {
  const { container } = render(
    <ul>
      <EmployeeCards
        employees={employees}
        headEmployeeId={headEmployeeId}
        canEdit={false}
        onAction={vi.fn()}
      />
    </ul>,
  );
  return [...container.querySelectorAll("strong")].map(
    (name) => name.textContent ?? "",
  );
}

describe("EmployeeCards order (AC-19)", () => {
  it("lists the head first and the others alphabetically in Russian, with «ё» sorted as «е» rather than after «я»", () => {
    const employees = [
      employee("1", "Яшин Пётр"),
      employee("2", "Фролов Олег"),
      employee("3", "Федотов Иван"),
      employee("4", "Фёдоров Сергей"),
      employee("5", "Жуков Андрей"),
    ];

    expect(namesInOrder(employees, "1")).toEqual([
      "Яшин Пётр",
      "Жуков Андрей",
      "Фёдоров Сергей",
      "Федотов Иван",
      "Фролов Олег",
    ]);
  });

  it("orders purely by name when the unit has no head among its employees", () => {
    expect(
      namesInOrder(
        [employee("1", "Борисов Иван"), employee("2", "Агафонов Олег")],
        null,
      ),
    ).toEqual(["Агафонов Олег", "Борисов Иван"]);
  });
});

describe("EmployeeCard actions menu (AC-18)", () => {
  it("opens the menu on the first press and leaves keyboard focus on the card's actions button", async () => {
    const actions = "Действия с сотрудником Яшин Пётр";
    render(
      <ul>
        <EmployeeCards
          employees={[employee("1", "Яшин Пётр")]}
          headEmployeeId={null}
          canEdit
          onAction={vi.fn()}
        />
      </ul>,
    );

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: actions }));

    expect(
      await screen.findByRole("menuitem", { name: /Изменить/ }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: actions })).toHaveFocus();
  });
});

describe("EmployeeCard assignments (AC-2, AC-5)", () => {
  const entity = { id: "le1", name: "ООО Альфа" };
  const secondary = {
    orgUnitId: "other",
    orgUnitName: "Другой отдел",
    positionId: "p2",
    position: "Аналитик",
    legalEntities: [entity],
    isSecondary: true,
  };

  function cardOf(overrides: Partial<Employee>) {
    const { container } = render(
      <ul>
        <EmployeeCards
          employees={[{ ...employee("1", "Яшин Пётр"), ...overrides }]}
          headEmployeeId={null}
          canEdit={false}
          onAction={vi.fn()}
        />
      </ul>,
    );
    return within(container);
  }

  it("shows the position and the legal entities of the main assignment, with the e-mail on the first line", () => {
    const card = cardOf({
      email: "yashin@test.local",
      legalEntities: [entity],
    });

    expect(
      card.getByText("Инженер · Юрлица: ООО Альфа · yashin@test.local"),
    ).toBeInTheDocument();
    expect(card.queryByText("Совмещение")).not.toBeInTheDocument();
  });

  it("marks a secondary assignment shown for the unit as «Совмещение» and keeps the e-mail on the first line only", () => {
    const card = cardOf({
      email: "yashin@test.local",
      unitAssignments: [
        {
          orgUnitId: "unit",
          orgUnitName: "Отдел",
          positionId: "position",
          position: "Инженер",
          legalEntities: [],
          isSecondary: false,
        },
        secondary,
      ],
    });

    expect(card.getByText("Совмещение")).toBeInTheDocument();
    expect(card.getByText("Инженер · yashin@test.local")).toBeInTheDocument();
    expect(card.getByText("Аналитик · Юрлица: ООО Альфа")).toBeInTheDocument();
  });

  it("falls back to the main assignment when the list was not filtered by a unit", () => {
    const main = employee("1", "Яшин Пётр");

    expect(shownAssignments(main)).toEqual([
      expect.objectContaining({ positionId: "position", isSecondary: false }),
    ]);
    expect(shownAssignments({ ...main, unitAssignments: [secondary] })).toEqual(
      [secondary],
    );
  });
});

describe("EmployeeCard move action (AC-28)", () => {
  async function openMenuOf(
    overrides: Partial<Employee>,
    onAction = vi.fn(),
    canEdit = true,
  ) {
    const person = { ...employee("1", "Яшин Пётр"), ...overrides };
    render(
      <ul>
        <EmployeeCards
          employees={[person]}
          headEmployeeId={null}
          canEdit={canEdit}
          onAction={onAction}
        />
      </ul>,
    );
    const user = userEvent.setup();
    const button = screen.queryByRole("button", {
      name: "Действия с сотрудником Яшин Пётр",
    });
    if (button) await user.click(button);
    return { user, onAction, person };
  }

  it("offers «Переместить» for a working employee and passes the employee to the action", async () => {
    const { user, onAction, person } = await openMenuOf({ isActive: true });

    await user.click(
      await screen.findByRole("menuitem", { name: /Переместить/ }),
    );

    expect(onAction).toHaveBeenCalledWith("move", person);
  });

  it("does not offer «Переместить» for a dismissed employee, who can be brought back instead", async () => {
    await openMenuOf({ isActive: false });

    await screen.findByRole("menuitem", { name: /Вернуть на работу/ });
    expect(
      screen.queryByRole("menuitem", { name: /Переместить/ }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("menuitem", { name: /Уволить/ }),
    ).not.toBeInTheDocument();
  });

  it("shows no menu to a user who cannot change employees", async () => {
    await openMenuOf({}, vi.fn(), false);

    expect(
      screen.queryByRole("button", { name: /Действия с сотрудником/ }),
    ).not.toBeInTheDocument();
  });
});
