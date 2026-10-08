import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StrictMode } from "react";
import { RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { LOGIN_PATH } from "../../app/featureContract";
import { Providers } from "../../app/Providers";
import { router } from "../../app/router";
import { fakeApi, problem } from "../../test/fakeApi";
import { FORGOT_PASSWORD_PATH } from "./ForgotPasswordPage";

const TOKEN = "token-from-the-mail";
const PASSWORD = "Correct-horse-1!";
const INVITATION = {
  path: "/accept-invitation",
  route: "POST /api/v1/auth/accept-invitation",
  title: "Задание пароля",
  submit: "Задать пароль",
  done: "Пароль задан. Войдите в систему.",
};
const RESET = {
  path: "/reset-password",
  route: "POST /api/v1/auth/reset-password",
  title: "Новый пароль",
  submit: "Сохранить пароль",
  done: "Пароль изменён. Войдите в систему с новым паролем.",
};
const INVALID_LINK_TITLE = "Ссылка недействительна";
const INVALID_LINK_DETAIL = "Ссылка устарела или уже использована.";

const currentPath = () => router.state.location.pathname;

async function openApp(address: string) {
  await router.navigate(address);
  render(
    <StrictMode>
      <Providers>
        <RouterProvider router={router} />
      </Providers>
    </StrictMode>,
  );
}

async function submitPasswords(submit: string, confirmation = PASSWORD) {
  const user = userEvent.setup();
  await user.type(await screen.findByLabelText("Пароль"), PASSWORD);
  await user.type(screen.getByLabelText("Повторите пароль"), confirmation);
  await user.click(screen.getByRole("button", { name: submit }));
}

beforeEach(() => {
  queryClient.clear();
});

describe.each([INVITATION, RESET])(
  "$title screen (AC-5, AC-8, AC-11)",
  (screenUnderTest) => {
    it("reads the token from the address fragment, drops the fragment from the address at once and sends the token only in the request body", async () => {
      fakeApi.on(
        screenUnderTest.route,
        () => new Response(null, { status: 204 }),
      );

      await openApp(`${screenUnderTest.path}#token=${TOKEN}`);

      expect(
        await screen.findByRole("heading", { name: screenUnderTest.title }),
      ).toBeInTheDocument();
      await waitFor(() => expect(window.location.hash).toBe(""));
      expect(router.state.location.hash).toBe("");
      expect(window.location.href).not.toContain(TOKEN);
      expect(fakeApi.calls(screenUnderTest.route)).toHaveLength(0);

      await submitPasswords(screenUnderTest.submit);

      await waitFor(() => expect(currentPath()).toBe(LOGIN_PATH));
      expect(await screen.findByText(screenUnderTest.done)).toBeInTheDocument();
      const [request] = fakeApi.calls(screenUnderTest.route);
      expect(await request?.json()).toEqual({
        token: TOKEN,
        password: PASSWORD,
      });
      expect(request?.url).not.toContain(TOKEN);
    });

    it("explains an address without a token and offers no form", async () => {
      await openApp(screenUnderTest.path);

      expect(
        await screen.findByText("Откройте ссылку из письма полностью.", {
          exact: false,
        }),
      ).toBeInTheDocument();
      expect(screen.queryByLabelText("Пароль")).not.toBeInTheDocument();
      expect(
        screen.getByRole("link", { name: "Не помню пароль" }),
      ).toBeInTheDocument();
    });

    it("replaces the form with the server's explanation and the way to ask for a new link when the link is refused", async () => {
      fakeApi.on(screenUnderTest.route, () =>
        problem(400, {
          title: INVALID_LINK_TITLE,
          detail: INVALID_LINK_DETAIL,
        }),
      );
      await openApp(`${screenUnderTest.path}#token=${TOKEN}`);

      await submitPasswords(screenUnderTest.submit);

      expect(await screen.findByText(INVALID_LINK_DETAIL)).toBeInTheDocument();
      expect(screen.getByText(INVALID_LINK_TITLE)).toBeInTheDocument();
      expect(screen.queryByLabelText("Пароль")).not.toBeInTheDocument();
      await userEvent
        .setup()
        .click(screen.getByRole("link", { name: "Не помню пароль" }));
      await waitFor(() => expect(currentPath()).toBe(FORGOT_PASSWORD_PATH));
    });

    it("keeps the form and marks the password field when the password breaks the policy, which leaves the link unspent", async () => {
      const policy = "Пароль: слишком короткий.";
      fakeApi.on(screenUnderTest.route, () =>
        problem(400, { title: "Validation", errors: { password: [policy] } }),
      );
      await openApp(`${screenUnderTest.path}#token=${TOKEN}`);

      await submitPasswords(screenUnderTest.submit);

      expect(await screen.findByText(policy)).toBeInTheDocument();
      expect(screen.getByLabelText("Пароль")).toBeInTheDocument();
      expect(screen.queryByText(INVALID_LINK_TITLE)).not.toBeInTheDocument();
      expect(currentPath()).toBe(screenUnderTest.path);
    });

    it("sends nothing while the two passwords differ", async () => {
      await openApp(`${screenUnderTest.path}#token=${TOKEN}`);

      await submitPasswords(screenUnderTest.submit, `${PASSWORD}-other`);

      expect(
        await screen.findByText("Пароли не совпадают"),
      ).toBeInTheDocument();
      expect(fakeApi.calls(screenUnderTest.route)).toHaveLength(0);
    });
  },
);

describe("forgotten password (AC-7, AC-11)", () => {
  it("is reached from the sign-in screen", async () => {
    await openApp(LOGIN_PATH);

    await userEvent
      .setup()
      .click(await screen.findByRole("link", { name: "Не помню пароль" }));

    expect(
      await screen.findByRole("heading", { name: "Восстановление пароля" }),
    ).toBeInTheDocument();
    expect(currentPath()).toBe(FORGOT_PASSWORD_PATH);
  });

  it("sends the address and answers with the same neutral notice, without session", async () => {
    fakeApi.on(
      "POST /api/v1/auth/forgot-password",
      () => new Response(null, { status: 202 }),
    );
    await openApp(FORGOT_PASSWORD_PATH);
    const user = userEvent.setup();

    await user.type(
      await screen.findByLabelText("E-mail"),
      "someone@test.local",
    );
    await user.click(screen.getByRole("button", { name: "Отправить ссылку" }));

    expect(await screen.findByText("Проверьте почту")).toBeInTheDocument();
    expect(
      screen.getByText(/Если учётная запись с этим e-mail существует/),
    ).toBeInTheDocument();
    expect(
      await fakeApi.calls("POST /api/v1/auth/forgot-password")[0]?.json(),
    ).toEqual({ email: "someone@test.local" });
    expect(fakeApi.calls("GET /api/v1/auth/me")).toHaveLength(0);
  });
});
