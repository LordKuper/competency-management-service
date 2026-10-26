import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import type { PublicationEntry } from "../career/catalogApi";
import type { MatrixPublication, MatrixVersion } from "../career/matrixApi";
import { type Decision, useVersionTransition } from "./useVersionTransition";

const VERSION = { id: "v1", version: 5 } as MatrixVersion;
const APPROVE_ROUTE = "POST /api/v1/career/matrix-versions/v1/approve";
const DIRECTION: PublicationEntry = {
  kind: "Direction",
  id: "d1",
  name: "Разработка",
  context: null,
};

function publication(
  hiddenEntryCount: number,
  entries: PublicationEntry[] = [],
): MatrixPublication {
  return {
    publishingActions: ["Approve"],
    entries,
    hiddenEntryCount,
    blockers: [],
    moreCount: 0,
    cascadeTag: "tag-version",
  };
}

function Harness({ decision }: { decision: Decision }) {
  const { decide } = useVersionTransition();
  return (
    <button type="button" onClick={() => void decide(decision)}>
      решить
    </button>
  );
}

function decisionOf(
  preview: MatrixPublication,
  isAskedFirst: boolean,
  onDone: Decision["onDone"],
): Decision {
  return {
    version: VERSION,
    transition: "approve",
    preview,
    publishing: {
      title: "Согласовать и опубликовать версию?",
      okText: "Согласовать",
      isAskedFirst,
      lead: (count) => `Станут действующими записей: ${count}.`,
    },
    onDone,
  };
}

function renderDecision(decision: Decision) {
  render(
    <Providers>
      <Harness decision={decision} />
    </Providers>,
  );
  return userEvent.setup();
}

/** jsdom plays no animation: the end of the opening animation is sent so the dialog counts as open. */
async function confirmDialog() {
  return waitFor(() => {
    const panel = document.querySelector(".ant-modal-confirm");
    expect(panel).not.toBeNull();
    const modal = panel as HTMLElement;
    fireEvent.animationEnd(modal);
    return modal;
  });
}

function cascadeOf(request: Request | undefined) {
  return new URL(request?.url ?? "http://x").searchParams.get("cascade");
}

describe("a decision on a version that publishes drafts the user may not read (AC-110, AC-112)", () => {
  it("asks first, counting the whole list and naming none of it, and publishes under its tag once confirmed", async () => {
    const onDone = vi.fn();
    fakeApi.on(APPROVE_ROUTE, () => Response.json({ id: "v1", version: 6 }));
    const user = renderDecision(decisionOf(publication(2), true, onDone));

    await user.click(screen.getByRole("button", { name: "решить" }));

    const dialog = await confirmDialog();
    expect(dialog).toHaveTextContent("Станут действующими записей: 2.");
    expect(dialog).toHaveTextContent("и ещё 2 черновые записи");
    expect(dialog).toHaveTextContent("Всё публикуется сразу");
    expect(dialog.querySelector("table")).toBeNull();
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(0);
    await user.click(screen.getByRole("button", { name: "Согласовать" }));
    await waitFor(() => expect(onDone).toHaveBeenCalledTimes(1));
    expect(cascadeOf(fakeApi.calls(APPROVE_ROUTE)[0])).toBe("tag-version");
    expect(onDone).toHaveBeenCalledWith(
      expect.objectContaining({ id: "v1" }),
      2,
    );
  });

  it("takes a decision under the list its screen showed, at once, and reports the whole count of what it published", async () => {
    const onDone = vi.fn();
    fakeApi.on(APPROVE_ROUTE, () => Response.json({ id: "v1", version: 6 }));
    const user = renderDecision(
      decisionOf(publication(2, [DIRECTION]), false, onDone),
    );

    await user.click(screen.getByRole("button", { name: "решить" }));

    await waitFor(() => expect(onDone).toHaveBeenCalledTimes(1));
    expect(document.querySelector(".ant-modal-confirm")).toBeNull();
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1);
    expect(cascadeOf(fakeApi.calls(APPROVE_ROUTE)[0])).toBe("tag-version");
    expect(onDone).toHaveBeenCalledWith(
      expect.objectContaining({ id: "v1" }),
      3,
    );
  });
});
