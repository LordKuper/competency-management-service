import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { EmployeeCards } from "./EmployeeCards";
import type { Employee } from "./orgStructureApi";

function employee(id: string, fullName: string): Employee {
  const [lastName = "", firstName = ""] = fullName.split(" ");
  return {
    id,
    lastName,
    firstName,
    middleName: null,
    fullName,
    email: null,
    orgUnitId: "unit",
    orgUnitName: "Отдел",
    position: "Инженер",
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
        isAdmin={false}
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
          isAdmin
          onAction={vi.fn()}
        />
      </ul>,
    );

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: actions }));

    expect(
      await screen.findByRole("menuitem", { name: /Править/ }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: actions })).toHaveFocus();
  });
});
