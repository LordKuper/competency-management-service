import { describe, expect, it } from "vitest";
import { HOME_PATH, LOGIN_PATH } from "../../app/featureContract";
import { FORGOT_PASSWORD_PATH } from "./ForgotPasswordPage";
import {
  ACCEPT_INVITATION_PATH,
  RESET_PASSWORD_PATH,
} from "./LinkPasswordPage";
import { loginPathReturningTo, returnTargetOf, safeReturnTo } from "./returnTo";

const SIGN_IN_SCREENS = [
  LOGIN_PATH,
  FORGOT_PASSWORD_PATH,
  ACCEPT_INVITATION_PATH,
  RESET_PASSWORD_PATH,
];

const FOREIGN_TARGETS = [
  "//evil.example",
  "///evil.example/path",
  "/\\evil.example",
  "/\\\\evil.example",
  "https://evil.example/desk",
  "http://evil.example",
  "javascript:alert(1)",
  "data:text/html,x",
  "/.//evil.example",
  "/a/..//evil.example",
  "desk",
  "",
  " /desk",
  "/\t/evil.example",
  "/desk\nSet-Cookie: a=b",
  "/desk\r",
  "/de\u0000sk",
  "/\u007f",
];

const IN_APP_TARGETS = [
  ["/desk", "/desk"],
  ["/desk/review/8c1d?step=3", "/desk/review/8c1d?step=3"],
  ["/org-structure?q=%D0%B0&page=2", "/org-structure?q=%D0%B0&page=2"],
  ["/desk#section", "/desk"],
  ["/a/../desk", "/desk"],
  ["/%2F%2Fevil.example", "/%2F%2Fevil.example"],
] as const;

describe("return target after sign-in (open redirect)", () => {
  it.each(FOREIGN_TARGETS)(
    "refuses %j, which leaves this origin or this app's paths",
    (target) => {
      expect(safeReturnTo(target)).toBeNull();
    },
  );

  it("refuses an absent target", () => {
    expect(safeReturnTo(null)).toBeNull();
  });

  it.each(SIGN_IN_SCREENS)(
    "refuses the sign-in screen %s, which a signed-in user has no use for",
    (screen) => {
      expect(safeReturnTo(screen)).toBeNull();
      expect(safeReturnTo(`${screen}?returnTo=%2Fdesk`)).toBeNull();
    },
  );

  it.each(IN_APP_TARGETS)(
    "opens %j as the parsed in-app path %j",
    (target, opened) => {
      expect(safeReturnTo(target)).toBe(opened);
    },
  );

  it("builds a sign-in address that carries an in-app target and reads it back unchanged", () => {
    for (const [target, opened] of IN_APP_TARGETS.filter(
      ([target]) => target !== HOME_PATH,
    )) {
      const address = new URL(
        loginPathReturningTo(target),
        window.location.origin,
      );

      expect(address.pathname).toBe(LOGIN_PATH);
      expect(returnTargetOf(address.searchParams)).toBe(opened);
    }
  });

  it.each(["/", HOME_PATH, ...FOREIGN_TARGETS, ...SIGN_IN_SCREENS])(
    "builds the plain sign-in address for %j, which nothing needs to be carried for",
    (target) => {
      expect(loginPathReturningTo(target)).toBe(LOGIN_PATH);
    },
  );

  it("reads no target from a sign-in address that names none or a foreign one", () => {
    expect(returnTargetOf(new URLSearchParams())).toBeNull();
    expect(
      returnTargetOf(new URLSearchParams({ returnTo: "//evil.example" })),
    ).toBeNull();
    expect(
      returnTargetOf(new URLSearchParams({ returnTo: "https://evil.example" })),
    ).toBeNull();
  });
});
