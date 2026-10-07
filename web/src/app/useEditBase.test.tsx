import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ApiError } from "../api/ApiError";
import { useEditBase } from "./useEditBase";

interface Snapshot {
  version: number;
}

const OPENED: Snapshot = { version: 1 };
const REFETCHED: Snapshot = { version: 2 };
const REFETCHED_AGAIN: Snapshot = { version: 3 };

function openEditor() {
  return renderHook(({ latest }) => useEditBase(latest), {
    initialProps: { latest: OPENED as Snapshot | undefined },
  });
}

describe("useEditBase (edit dialogs of units, employees and accounts)", () => {
  it("keeps the record the dialog opened with when a background refetch delivers a newer version", () => {
    const editor = openEditor();

    editor.rerender({ latest: REFETCHED });

    expect(editor.result.current.base).toBe(OPENED);
  });

  it("moves to the newer record when a save refused as stale meets one the cache already holds", () => {
    const editor = openEditor();
    editor.rerender({ latest: REFETCHED });

    act(() => editor.result.current.noteSaveFailure(new ApiError(412, {})));

    expect(editor.result.current.base).toBe(REFETCHED);
  });

  it("waits for the newer record when the stale refusal comes before the reread has delivered it", () => {
    const editor = openEditor();

    act(() => editor.result.current.noteSaveFailure(new ApiError(412, {})));
    expect(editor.result.current.base).toBe(OPENED);
    editor.rerender({ latest: REFETCHED });

    expect(editor.result.current.base).toBe(REFETCHED);
  });

  it("stays on the moved-to record when a later refetch delivers yet another version", () => {
    const editor = openEditor();
    editor.rerender({ latest: REFETCHED });
    act(() => editor.result.current.noteSaveFailure(new ApiError(412, {})));

    editor.rerender({ latest: REFETCHED_AGAIN });

    expect(editor.result.current.base).toBe(REFETCHED);
  });

  it.each([
    ["a conflict", new ApiError(409, {})],
    ["invalid input", new ApiError(400, {})],
    ["a server failure", new ApiError(500, {})],
    ["a lost connection", new TypeError("Failed to fetch")],
  ])("does not move to a newer record after %s", (_, failure) => {
    const editor = openEditor();
    editor.rerender({ latest: REFETCHED });

    act(() => editor.result.current.noteSaveFailure(failure));

    expect(editor.result.current.base).toBe(OPENED);
  });
});
