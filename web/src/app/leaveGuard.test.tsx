import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import {
  createMemoryRouter,
  Link,
  Outlet,
  RouterProvider,
  useNavigate,
} from "react-router";
import { describe, expect, it } from "vitest";
import { LOGIN_PATH } from "./featureContract";
import {
  type LeaveEdits,
  LeaveGuardProvider,
  useLeaveGuard,
} from "./leaveGuard";
import { Providers } from "./Providers";

const DIALOG_TITLE = "Перейти без сохранения?";
const LEAVE = "Перейти без сохранения";
const STAY = "Остаться";

let leftThroughAction = 0;

/** jsdom plays no animation, so the end of the closing animation, which removes the dialog, is sent until the dialog is gone. */
async function dialogGone() {
  await waitFor(() => {
    const panel = document.querySelector(".ant-modal");
    if (panel) {
      fireEvent.animationEnd(panel);
      fireEvent.transitionEnd(panel);
    }
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
}

function Screen() {
  const [edits, setEdits] = useState<LeaveEdits>({ isUnsaved: false });
  const { confirmLeave, leaveAfterSave } = useLeaveGuard(edits);
  const navigate = useNavigate();
  return (
    <div>
      <p>Экран с правками</p>
      <button type="button" onClick={() => setEdits({ isUnsaved: true })}>
        править
      </button>
      <button
        type="button"
        onClick={() => setEdits({ isUnsaved: true, isSaving: true })}
      >
        сохранять
      </button>
      <button
        type="button"
        onClick={() => setEdits({ isUnsaved: false, isSaving: true })}
      >
        только сохранение
      </button>
      <button type="button" onClick={() => setEdits({ isUnsaved: false })}>
        сохранено
      </button>
      <button
        type="button"
        onClick={() => confirmLeave(() => leftThroughAction++)}
      >
        закрыть окно
      </button>
      <button
        type="button"
        onClick={() => leaveAfterSave(() => void navigate("/other"))}
      >
        сохранить и уйти
      </button>
      <Link to="/other">ссылка</Link>
      <Link to={LOGIN_PATH}>к входу</Link>
      <Link to="/">та же страница</Link>
    </div>
  );
}

function renderGuarded() {
  leftThroughAction = 0;
  const router = createMemoryRouter(
    [
      {
        element: (
          <LeaveGuardProvider>
            <Outlet />
          </LeaveGuardProvider>
        ),
        children: [
          { path: "/", element: <Screen /> },
          { path: "/other", element: <p>Другая страница</p> },
          { path: LOGIN_PATH, element: <p>Вход</p> },
        ],
      },
    ],
    { initialEntries: ["/"] },
  );
  render(
    <Providers>
      <RouterProvider router={router} />
    </Providers>,
  );
  return { router, user: userEvent.setup() };
}

describe("the guard against losing edits (AC-61)", () => {
  it("lets a link go at once when nothing is unsaved", async () => {
    const { router, user } = renderGuarded();

    await user.click(screen.getByRole("link", { name: "ссылка" }));

    expect(await screen.findByText("Другая страница")).toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/other");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("asks before a link leaves unsaved edits, and 'Остаться' keeps the page and the edits", async () => {
    const { router, user } = renderGuarded();
    await user.click(screen.getByRole("button", { name: "править" }));

    await user.click(screen.getByRole("link", { name: "ссылка" }));

    expect(
      await screen.findByRole("dialog", { name: DIALOG_TITLE }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: STAY }));
    await dialogGone();
    expect(router.state.location.pathname).toBe("/");
    await user.click(screen.getByRole("link", { name: "ссылка" }));
    expect(
      await screen.findByRole("dialog", { name: DIALOG_TITLE }),
    ).toBeInTheDocument();
  });

  it("leaves and drops the edits when the user agrees", async () => {
    const { router, user } = renderGuarded();
    await user.click(screen.getByRole("button", { name: "править" }));
    await user.click(screen.getByRole("link", { name: "ссылка" }));

    await user.click(await screen.findByRole("button", { name: LEAVE }));

    expect(await screen.findByText("Другая страница")).toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/other");
  });

  it("asks on the browser's Back and Forward too", async () => {
    const { router, user } = renderGuarded();
    await user.click(screen.getByRole("link", { name: "ссылка" }));
    await screen.findByText("Другая страница");
    await act(() => router.navigate(-1));
    await screen.findByText("Экран с правками");
    await user.click(screen.getByRole("button", { name: "править" }));

    await act(() => router.navigate(1));

    expect(
      await screen.findByRole("dialog", { name: DIALOG_TITLE }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/");
    await user.click(screen.getByRole("button", { name: LEAVE }));
    await waitFor(() => expect(router.state.location.pathname).toBe("/other"));
  });

  it("never holds back the sign-in screen, and does not ask for a move that changes nothing", async () => {
    const { router, user } = renderGuarded();
    await user.click(screen.getByRole("button", { name: "править" }));

    await user.click(screen.getByRole("link", { name: "та же страница" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/");
    await user.click(screen.getByRole("link", { name: "к входу" }));

    expect(await screen.findByText("Вход")).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("holds a move silently while a save runs, then lets it through without asking when the save succeeds", async () => {
    const { router, user } = renderGuarded();
    await user.click(screen.getByRole("button", { name: "сохранять" }));

    await user.click(screen.getByRole("link", { name: "ссылка" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/");
    await user.click(screen.getByRole("button", { name: "сохранено" }));
    expect(await screen.findByText("Другая страница")).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("holds a move silently while a save runs even when nothing else is unsaved", async () => {
    const { router, user } = renderGuarded();
    await user.click(screen.getByRole("button", { name: "только сохранение" }));

    await user.click(screen.getByRole("link", { name: "ссылка" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/");
    await user.click(screen.getByRole("button", { name: "сохранено" }));
    expect(await screen.findByText("Другая страница")).toBeInTheDocument();
  });

  it("lets a move that follows the caller's own save go without asking", async () => {
    const { router, user } = renderGuarded();
    await user.click(screen.getByRole("button", { name: "править" }));

    await user.click(screen.getByRole("button", { name: "сохранить и уйти" }));

    expect(await screen.findByText("Другая страница")).toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/other");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("asks before the caller's own leaving action, and runs it at once when nothing is unsaved", async () => {
    const { user } = renderGuarded();

    await user.click(screen.getByRole("button", { name: "закрыть окно" }));
    expect(leftThroughAction).toBe(1);
    await user.click(screen.getByRole("button", { name: "править" }));
    await user.click(screen.getByRole("button", { name: "закрыть окно" }));

    expect(
      await screen.findByRole("dialog", { name: DIALOG_TITLE }),
    ).toBeInTheDocument();
    expect(leftThroughAction).toBe(1);
    await user.click(screen.getByRole("button", { name: STAY }));
    await dialogGone();
    expect(leftThroughAction).toBe(1);
    await user.click(screen.getByRole("button", { name: "закрыть окно" }));
    await user.click(await screen.findByRole("button", { name: LEAVE }));
    expect(leftThroughAction).toBe(2);
  });

  it("asks only once while a question is open", async () => {
    const { user } = renderGuarded();
    await user.click(screen.getByRole("button", { name: "править" }));

    await user.click(screen.getByRole("button", { name: "закрыть окно" }));
    await screen.findByRole("dialog", { name: DIALOG_TITLE });
    await user.click(screen.getByRole("button", { name: "закрыть окно" }));

    expect(screen.getAllByRole("dialog")).toHaveLength(1);
  });

  it("makes the browser warn on closing or reloading the tab while edits are unsaved or being saved, and not otherwise", async () => {
    const { user } = renderGuarded();
    const closeTab = () => {
      const event = new Event("beforeunload", { cancelable: true });
      window.dispatchEvent(event);
      return event.defaultPrevented;
    };

    const untouched = closeTab();
    await user.click(screen.getByRole("button", { name: "править" }));
    const unsaved = closeTab();
    await user.click(screen.getByRole("button", { name: "сохранять" }));
    const saving = closeTab();
    await user.click(screen.getByRole("button", { name: "сохранено" }));
    const saved = closeTab();

    expect(untouched).toBe(false);
    expect(unsaved).toBe(true);
    expect(saving).toBe(true);
    expect(saved).toBe(false);
  });
});
