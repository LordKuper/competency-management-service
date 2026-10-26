import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Form, Input } from "antd";
import { useContext } from "react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LONG_FLOW_TIMEOUT } from "../test/timeouts";
import { AppModal } from "./AppModal";
import { LeaveGuardProvider } from "./leaveGuard";
import { ModalSavedContext } from "./modalSaved";
import { Providers } from "./Providers";

const original = HTMLElement.prototype.getClientRects;
const ASK = "Перейти без сохранения?";
const LEAVE = "Перейти без сохранения";
const STAY = "Остаться";

beforeEach(() => {
  HTMLElement.prototype.getClientRects = () => [{}] as unknown as DOMRectList;
});

afterEach(() => {
  HTMLElement.prototype.getClientRects = original;
});

/** jsdom plays no animation, so the end of an animation, which moves a dialog to its next state, is sent until it has taken effect. */
async function settle(isGone = false) {
  await waitFor(() => {
    const panel = document.querySelector(".ant-modal");
    if (panel) {
      fireEvent.animationEnd(panel);
      fireEvent.transitionEnd(panel);
    }
    if (isGone) expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    else expect(panel).not.toHaveClass("ant-zoom-appear");
  });
}

function Saver() {
  const markSaved = useContext(ModalSavedContext);
  return (
    <button type="button" onClick={markSaved}>
      сохранить
    </button>
  );
}

function Host({
  onCancel,
  isUnsaved,
}: {
  onCancel: () => void;
  isUnsaved?: boolean;
}) {
  return (
    <AppModal
      open
      title="Новая шкала"
      footer={null}
      isUnsaved={isUnsaved}
      onCancel={onCancel}
    >
      <Form>
        <Form.Item name="name" label="Название">
          <Input />
        </Form.Item>
        <Saver />
      </Form>
    </AppModal>
  );
}

async function renderHost(options: { isUnsaved?: boolean } = {}) {
  const onCancel = vi.fn();
  const router = createMemoryRouter([
    {
      path: "/",
      element: (
        <LeaveGuardProvider>
          <Host onCancel={onCancel} isUnsaved={options.isUnsaved} />
        </LeaveGuardProvider>
      ),
    },
  ]);
  render(
    <Providers>
      <RouterProvider router={router} />
    </Providers>,
  );
  await settle();
  return { onCancel, user: userEvent.setup() };
}

describe(
  "closing a dialog with unsaved edits (AC-61)",
  () => {
    it("closes at once by the cross when nothing was changed", async () => {
      const { onCancel, user } = await renderHost();

      await user.click(screen.getByRole("button", { name: /Close|Закрыть/ }));

      expect(onCancel).toHaveBeenCalledTimes(1);
    });

    it.each(["the cross", "Escape"] as const)(
      "asks before %s closes a dialog whose form was changed, and 'Остаться' keeps it",
      async (way) => {
        const { onCancel, user } = await renderHost();
        await user.type(screen.getByLabelText("Название"), "Новая");

        if (way === "Escape") {
          await user.keyboard("{Escape}");
        } else {
          await user.click(
            screen.getByRole("button", { name: /Close|Закрыть/ }),
          );
        }

        expect(
          await screen.findByText(ASK, {
            selector: ".ant-modal-confirm-title",
          }),
        ).toBeInTheDocument();
        expect(onCancel).not.toHaveBeenCalled();
        await user.click(screen.getByRole("button", { name: STAY }));
        await settle(false);
        expect(screen.getByLabelText("Название")).toHaveValue("Новая");
        expect(onCancel).not.toHaveBeenCalled();
      },
    );

    it("closes the dialog when the user agrees to lose the edits", async () => {
      const { onCancel, user } = await renderHost();
      await user.type(screen.getByLabelText("Название"), "Новая");
      await user.click(screen.getByRole("button", { name: /Close|Закрыть/ }));

      await user.click(await screen.findByRole("button", { name: LEAVE }));

      expect(onCancel).toHaveBeenCalledTimes(1);
    });

    it("asks when the caller marks the dialog unsaved although the form was not touched", async () => {
      const { onCancel, user } = await renderHost({ isUnsaved: true });

      await user.click(screen.getByRole("button", { name: /Close|Закрыть/ }));

      expect(
        await screen.findByText(ASK, { selector: ".ant-modal-confirm-title" }),
      ).toBeInTheDocument();
      expect(onCancel).not.toHaveBeenCalled();
    });

    it("closes at once after a save has cleared the unsaved mark", async () => {
      const { onCancel, user } = await renderHost();
      await user.type(screen.getByLabelText("Название"), "Новая");
      await user.click(screen.getByRole("button", { name: "сохранить" }));

      await user.click(screen.getByRole("button", { name: /Close|Закрыть/ }));

      expect(onCancel).toHaveBeenCalledTimes(1);
      expect(
        screen.queryByText(ASK, { selector: ".ant-modal-confirm-title" }),
      ).not.toBeInTheDocument();
    });
  },
  LONG_FLOW_TIMEOUT,
);
