import { describe, expect, it } from "vitest";
import { destructiveConfirm } from "./destructiveConfirm";

describe("destructiveConfirm", () => {
  it("focuses «Отмена» and styles the action as danger by default, so Enter cannot fire it by accident", () => {
    const props = destructiveConfirm(true);

    expect(props.autoFocusButton).toBe("cancel");
    expect(props.okButtonProps?.danger).toBe(true);
  });

  it("keeps the focus on the action of a harmless confirmation, without the danger style", () => {
    const props = destructiveConfirm(false);

    expect(props.autoFocusButton).toBe("ok");
    expect(props.okButtonProps?.danger).toBe(false);
  });

  it("focuses «Отмена» without restyling the button for an irreversible action that is not danger-styled", () => {
    const props = destructiveConfirm(false, {}, true);

    expect(props.autoFocusButton).toBe("cancel");
    expect(props.okButtonProps?.danger).toBe(false);
  });

  it("passes the other button properties through", () => {
    const props = destructiveConfirm(true, { loading: true });

    expect(props.okButtonProps).toMatchObject({ loading: true, danger: true });
  });
});
