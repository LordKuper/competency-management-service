import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import type { CurrentUser } from "../features/auth/useCurrentUser";
import { Providers } from "./Providers";
import { UserMenu } from "./UserMenu";

const NBSP = String.fromCharCode(0x00a0);

function user(overrides: Partial<CurrentUser>): CurrentUser {
  return {
    id: "account",
    email: "user@test.local",
    role: "User",
    employeeId: null,
    employeeName: null,
    employeeLastName: null,
    employeeFirstName: null,
    employeeMiddleName: null,
    ...overrides,
  };
}

function renderMenu(current: CurrentUser) {
  render(
    <Providers>
      <MemoryRouter>
        <UserMenu user={current} />
      </MemoryRouter>
    </Providers>,
  );
}

describe("UserMenu header identity (AC-13)", () => {
  it("shows the initials and last name joined by non-breaking spaces, and the first letters of first and last name on the avatar", () => {
    renderMenu(
      user({
        employeeLastName: "Иванова",
        employeeFirstName: "Анна",
        employeeMiddleName: "Борисовна",
      }),
    );

    expect(screen.getByRole("button").getAttribute("aria-label")).toBe(
      ["А.", "Б.", "Иванова"].join(NBSP),
    );
    expect(screen.getByText("АИ")).toBeInTheDocument();
  });

  it("drops the patronymic initial when the employee has none", () => {
    renderMenu(
      user({ employeeLastName: "Иванова", employeeFirstName: "Анна" }),
    );

    expect(screen.getByRole("button").getAttribute("aria-label")).toBe(
      ["А.", "Иванова"].join(NBSP),
    );
  });

  it("falls back to the e-mail and its first letter for an account without an employee", () => {
    renderMenu(user({ email: "user@test.local" }));

    expect(
      screen.getByRole("button", { name: "user@test.local" }),
    ).toBeInTheDocument();
    expect(screen.getByText("U")).toBeInTheDocument();
  });
});
