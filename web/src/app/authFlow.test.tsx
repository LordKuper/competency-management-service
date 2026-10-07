import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { api } from "../api/client";
import { queryClient } from "../api/queryClient";
import { currentUserQueryKey } from "../features/auth/useCurrentUser";
import { paths } from "../features/org-structure/paths";
import { deferred, fakeApi, problem } from "../test/fakeApi";
import { LOGIN_PATH } from "./featureContract";
import { Providers } from "./Providers";
import { router } from "./router";

const ACCOUNT = {
  id: "account",
  email: "user@test.local",
  role: "User",
  employeeId: null,
  employeeName: null,
  employeeLastName: null,
  employeeFirstName: null,
  employeeMiddleName: null,
};
const MARKER_KEY = ["marker"];
const SIGN_IN_HEADING = "Вход в систему";
const REFUSAL = "Проверьте e-mail и пароль.";

function answerAsSignedIn() {
  fakeApi.on("GET /api/v1/auth/me", () => Response.json(ACCOUNT));
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
}

async function openApp(path: string) {
  await router.navigate(path);
  render(
    <Providers>
      <RouterProvider router={router} />
    </Providers>,
  );
}

async function openUserMenu() {
  const user = userEvent.setup();
  await user.click(await screen.findByRole("button", { name: ACCOUNT.email }));
  return user;
}

async function submitCredentials() {
  const user = userEvent.setup();
  await user.type(await screen.findByLabelText("E-mail"), ACCOUNT.email);
  await user.type(screen.getByLabelText("Пароль"), "Correct-horse-1!");
  await user.click(screen.getByRole("button", { name: "Войти" }));
}

const currentPath = () => router.state.location.pathname;

beforeEach(() => {
  queryClient.clear();
});

describe("route guard (AC-6, AC-13)", () => {
  it("sends a visitor without a session to the sign-in screen and renders no section before that", async () => {
    fakeApi.on("GET /api/v1/auth/me", () => problem(401));

    await openApp(paths.tree);

    expect(
      await screen.findByRole("heading", { name: SIGN_IN_HEADING }),
    ).toBeInTheDocument();
    expect(currentPath()).toBe(LOGIN_PATH);
    expect(fakeApi.calls("GET /api/v1/org-units/tree")).toHaveLength(0);
  });

  it("shows the section to a signed-in user and leaves the address alone", async () => {
    answerAsSignedIn();

    await openApp(paths.tree);

    expect(
      await screen.findByRole("button", { name: ACCOUNT.email }),
    ).toBeInTheDocument();
    expect(currentPath()).toBe(paths.tree);
  });
});

describe("sign-in (AC-6, AC-13)", () => {
  it("caches the account the server returned and opens the application without waiting for /auth/me", async () => {
    const meAnswer = deferred<Response>();
    fakeApi.on("GET /api/v1/auth/me", () => meAnswer.promise);
    fakeApi.on("GET /api/v1/org-units/tree", () => Response.json([]));
    fakeApi.on("POST /api/v1/auth/login", () => Response.json(ACCOUNT));

    await openApp(LOGIN_PATH);
    await submitCredentials();

    expect(
      await screen.findByRole("button", { name: ACCOUNT.email }),
    ).toBeInTheDocument();
    expect(queryClient.getQueryData(currentUserQueryKey)).toEqual(ACCOUNT);
    expect(currentPath()).toBe(paths.tree);
    expect(await fakeApi.calls("POST /api/v1/auth/login")[0]?.json()).toEqual({
      email: ACCOUNT.email,
      password: "Correct-horse-1!",
    });
    meAnswer.resolve(Response.json(ACCOUNT));
  });

  it("stays on the sign-in screen when the credentials are refused: the unauthorized handler does not clear the cache or navigate on /login", async () => {
    fakeApi.on("POST /api/v1/auth/login", () =>
      problem(401, { detail: REFUSAL }),
    );
    queryClient.setQueryData(MARKER_KEY, "kept");

    await openApp(LOGIN_PATH);
    await submitCredentials();

    expect(await screen.findByText(REFUSAL)).toBeInTheDocument();
    expect(currentPath()).toBe(LOGIN_PATH);
    expect(queryClient.getQueryData(MARKER_KEY)).toBe("kept");
  });
});

describe("sign-out (AC-6, AC-13)", () => {
  it("ends the session, empties the cache and replaces the page with the sign-in screen", async () => {
    answerAsSignedIn();
    fakeApi.on(
      "POST /api/v1/auth/logout",
      () => new Response(null, { status: 204 }),
    );
    await openApp(paths.tree);
    const user = await openUserMenu();
    queryClient.setQueryData(MARKER_KEY, "stale");
    const entriesBefore = window.history.length;

    await user.click(await screen.findByRole("menuitem", { name: /Выйти/ }));

    expect(
      await screen.findByRole("heading", { name: SIGN_IN_HEADING }),
    ).toBeInTheDocument();
    expect(currentPath()).toBe(LOGIN_PATH);
    expect(queryClient.getQueryData(MARKER_KEY)).toBeUndefined();
    expect(window.history.length).toBe(entriesBefore);
  });

  it("keeps the user where they are, with the cache intact, when the server fails to end the session", async () => {
    answerAsSignedIn();
    fakeApi.on("POST /api/v1/auth/logout", () => problem(500));
    await openApp(paths.tree);
    const user = await openUserMenu();
    queryClient.setQueryData(MARKER_KEY, "kept");

    await user.click(await screen.findByRole("menuitem", { name: /Выйти/ }));

    expect(await screen.findByText(/ошибки сервиса/)).toBeInTheDocument();
    expect(currentPath()).toBe(paths.tree);
    expect(queryClient.getQueryData(MARKER_KEY)).toBe("kept");
    expect(
      screen.getByRole("button", { name: ACCOUNT.email }),
    ).toBeInTheDocument();
  });
});

describe("session ended mid-session (AC-6, AC-13)", () => {
  it("clears the cache and replaces the page with the sign-in screen when any call is answered 401", async () => {
    answerAsSignedIn();
    fakeApi.on("GET /api/v1/users", () => problem(401));
    await openApp(paths.tree);
    await screen.findByRole("button", { name: ACCOUNT.email });
    queryClient.setQueryData(MARKER_KEY, "stale");
    const entriesBefore = window.history.length;

    await api.GET("/api/v1/users", {
      params: { query: { page: 1, pageSize: 1 } },
    });

    expect(
      await screen.findByRole("heading", { name: SIGN_IN_HEADING }),
    ).toBeInTheDocument();
    expect(currentPath()).toBe(LOGIN_PATH);
    expect(queryClient.getQueryData(MARKER_KEY)).toBeUndefined();
    expect(window.history.length).toBe(entriesBefore);
  });
});
