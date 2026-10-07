import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { EmployeeImpactList } from "./EmployeeImpactList";
import type { EmployeeImpact } from "./orgStructureApi";

const SALES = {
  id: "sales",
  name: "Отдел продаж",
  parentId: null,
  headEmployeeId: "employee",
  isActive: true,
  version: 1,
};

function impact(
  account: EmployeeImpact["account"],
  headOfUnits = [SALES],
): EmployeeImpact {
  return { headOfUnits, account };
}

const ACTIVE_ACCOUNT = {
  email: "ivan@test.local",
  isBlocked: false,
  isLastActiveAdministrator: false,
};

describe("EmployeeImpactList (AC-18)", () => {
  it("lists every change a dismissal makes elsewhere: the unit left without a head and the blocked account", () => {
    render(
      <EmployeeImpactList impact={impact(ACTIVE_ACCOUNT)} action="dismiss" />,
    );

    expect(
      screen.getByText("Сотрудник получит статус «не работает»."),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "Подразделение «Отдел продаж» останется без руководителя",
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Будет заблокирована учётная запись ivan@test\.local/),
    ).toBeInTheDocument();
    expect(screen.queryByText(/отвязана/)).not.toBeInTheDocument();
  });

  it("adds the detaching of the account on deletion", () => {
    render(
      <EmployeeImpactList impact={impact(ACTIVE_ACCOUNT)} action="delete" />,
    );

    expect(
      screen.getByText(
        "Сотрудник будет удалён без возможности восстановления.",
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "Учётная запись ivan@test.local будет отвязана от сотрудника",
      ),
    ).toBeInTheDocument();
  });

  it("does not announce blocking an account that is blocked already, and says so when nothing else changes", () => {
    render(
      <EmployeeImpactList
        impact={impact({ ...ACTIVE_ACCOUNT, isBlocked: true }, [])}
        action="dismiss"
      />,
    );

    expect(screen.queryByText(/Будет заблокирована/)).not.toBeInTheDocument();
    expect(screen.getByText("Связанных изменений нет.")).toBeInTheDocument();
  });

  it("shows only the refusal when the account is the last active administrator", () => {
    render(
      <EmployeeImpactList
        impact={impact({ ...ACTIVE_ACCOUNT, isLastActiveAdministrator: true })}
        action="delete"
      />,
    );

    expect(
      screen.getByText(
        /последний активный глобальный администратор \(ivan@test\.local\)/,
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText("Связанные изменения:")).not.toBeInTheDocument();
    expect(screen.queryByText(/будет удалён/)).not.toBeInTheDocument();
  });
});
