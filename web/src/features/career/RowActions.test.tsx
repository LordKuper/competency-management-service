import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { type RowAction, RowActions } from "./RowActions";

describe("RowActions: two visible, the rest behind «…» (AC-19)", () => {
  const action = (key: string, fields: Partial<RowAction> = {}): RowAction => ({
    key,
    label: key,
    onClick: vi.fn(),
    ...fields,
  });

  const visibleLabels = () =>
    screen
      .queryAllByRole("button")
      .filter((button) => !button.getAttribute("aria-label"))
      .map((button) => button.textContent);

  function showActions(actions: RowAction[]) {
    render(<RowActions actions={actions} menuLabel="Действия с «Х»" />);
    return userEvent.setup();
  }

  it.each([
    [[]],
    [[action("a")]],
    [[action("a"), action("b", { danger: true })]],
  ])(
    "shows up to two actions as they are, destructive too, with no «…» (case %#)",
    (actions) => {
      showActions(actions);

      expect(
        screen.queryByRole("button", { name: "Действия с «Х»" }),
      ).toBeNull();
      expect(visibleLabels()).toEqual(actions.map((a) => a.label));
      for (const { label, danger } of actions) {
        expect(
          screen
            .getByRole("button", { name: label })
            .classList.contains("ant-btn-dangerous"),
        ).toBe(danger === true);
      }
    },
  );

  it("with more, keeps the first two that are not destructive visible and puts the rest in «…» in their order", async () => {
    const user = showActions([
      action("a"),
      action("rm", { danger: true }),
      action("b"),
      action("c"),
      action("off", { danger: true }),
    ]);

    expect(visibleLabels()).toEqual(["a", "b"]);
    expect(screen.queryByRole("menuitem")).toBeNull();

    await user.click(screen.getByRole("button", { name: "Действия с «Х»" }));

    const items = await screen.findAllByRole("menuitem");
    expect(items.map((item) => item.textContent)).toEqual(["rm", "c", "off"]);
    expect(
      items.map((item) =>
        item.classList.contains("ant-dropdown-menu-item-danger"),
      ),
    ).toEqual([true, false, true]);
  });

  it("runs the action chosen in the menu, and not a disabled one", async () => {
    const run = vi.fn();
    const blocked = vi.fn();
    const user = showActions([
      action("a"),
      action("b"),
      action("blocked", { disabled: true, onClick: blocked }),
      action("run", { onClick: run }),
    ]);
    await user.click(screen.getByRole("button", { name: "Действия с «Х»" }));

    await user.click(await screen.findByRole("menuitem", { name: "blocked" }));
    expect(blocked).not.toHaveBeenCalled();
    await user.click(await screen.findByRole("menuitem", { name: "run" }));

    expect(run).toHaveBeenCalledOnce();
  });
});
