import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { type ReactElement, useState } from "react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { LONG_FLOW_TIMEOUT } from "../test/timeouts";
import { AppModal } from "./AppModal";
import { Providers } from "./Providers";

const original = HTMLElement.prototype.getClientRects;

beforeEach(() => {
  HTMLElement.prototype.getClientRects = () => [{}] as unknown as DOMRectList;
});

afterEach(() => {
  HTMLElement.prototype.getClientRects = original;
});

/** jsdom plays no animation, so the end of the opening animation, which tells the dialog it has opened, is sent until it has taken effect. */
async function finishOpening() {
  await waitFor(() => {
    const panel = document.querySelector(".ant-modal");
    expect(panel).not.toBeNull();
    if (panel?.classList.contains("ant-zoom-appear")) {
      fireEvent.animationEnd(panel);
      fireEvent.transitionEnd(panel);
    }
    expect(panel).not.toHaveClass("ant-zoom-appear");
  });
}

async function open(ui: ReactElement) {
  render(ui);
  await finishOpening();
}

function Dialog({
  children,
  footer,
}: {
  children: React.ReactNode;
  footer?: null;
}) {
  return (
    <Providers>
      <AppModal
        open
        title="Новая шкала"
        okText="Сохранить"
        footer={footer}
        onCancel={() => undefined}
      >
        {children}
      </AppModal>
    </Providers>
  );
}

describe("AppModal initial focus", { timeout: LONG_FLOW_TIMEOUT }, () => {
  it("moves focus to the first field of the form when the dialog opens", async () => {
    await open(
      <Dialog>
        <label>
          Название
          <input />
        </label>
        <label>
          Описание
          <textarea />
        </label>
      </Dialog>,
    );

    await waitFor(() =>
      expect(screen.getByLabelText("Название")).toHaveFocus(),
    );
  });

  it("focuses the primary button of the footer when the dialog has no field", async () => {
    await open(
      <Dialog>
        <p>Подтвердите действие</p>
      </Dialog>,
    );

    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Сохранить" })).toHaveFocus(),
    );
  });

  it("skips a disabled and a hidden field", async () => {
    await open(
      <Dialog>
        <input aria-label="Отключено" disabled />
        <input aria-label="Скрыто" type="hidden" />
        <input aria-label="Рабочее" />
      </Dialog>,
    );

    await waitFor(() => expect(screen.getByLabelText("Рабочее")).toHaveFocus());
  });

  it("focuses a field that appears after the opening, as when the record is still loading", async () => {
    function Loading() {
      const [loaded, setLoaded] = useState(false);
      return (
        <Dialog footer={null}>
          {loaded ? <input aria-label="Название" /> : <p>Загрузка</p>}
          <button type="button" onClick={() => setLoaded(true)}>
            Загрузилось
          </button>
        </Dialog>
      );
    }
    await open(<Loading />);
    expect(screen.queryByLabelText("Название")).not.toBeInTheDocument();

    await act(async () =>
      screen.getByRole("button", { name: "Загрузилось" }).click(),
    );

    await waitFor(() =>
      expect(screen.getByLabelText("Название")).toHaveFocus(),
    );
  });

  it("leaves the focus alone when the user has already moved it inside the dialog before the field appears", async () => {
    function Loading() {
      const [loaded, setLoaded] = useState(false);
      return (
        <Dialog footer={null}>
          {loaded ? <input aria-label="Название" /> : <p>Загрузка</p>}
          <button type="button" onClick={() => setLoaded(true)}>
            Загрузилось
          </button>
        </Dialog>
      );
    }
    await open(<Loading />);
    const user = userEvent.setup();
    const loadedButton = screen.getByRole("button", { name: "Загрузилось" });
    await user.click(loadedButton);

    await waitFor(() =>
      expect(screen.getByLabelText("Название")).toBeInTheDocument(),
    );
    expect(screen.getByLabelText("Название")).not.toHaveFocus();
    expect(loadedButton).toHaveFocus();
  });

  it("does not move the focus again when the dialog re-renders", async () => {
    function Counter() {
      const [count, setCount] = useState(0);
      return (
        <Dialog>
          <input aria-label="Название" />
          <input aria-label="Описание" />
          <button type="button" onClick={() => setCount(count + 1)}>
            Ещё {count}
          </button>
        </Dialog>
      );
    }
    await open(<Counter />);
    await waitFor(() =>
      expect(screen.getByLabelText("Название")).toHaveFocus(),
    );
    const user = userEvent.setup();
    await user.click(screen.getByLabelText("Описание"));

    await user.click(screen.getByRole("button", { name: /Ещё/ }));

    expect(screen.getByRole("button", { name: /Ещё 1/ })).toHaveFocus();
  });
});
