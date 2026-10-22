import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { catalogQueryKey } from "./catalogApi";
import { type PublishTarget, usePublishEntry } from "./usePublishEntry";

const TARGET: PublishTarget = {
  segment: "competencies",
  entry: { id: "c1", version: 4, name: "Работа в команде" },
};
const PREVIEW_ROUTE = "GET /api/v1/career/competencies/c1/publication";
const APPROVE_ROUTE = "POST /api/v1/career/competencies/c1/approve";
const DIRECTION = { kind: "Direction", id: "d1", name: "Разработка" } as const;
const SCALE = { kind: "Scale", id: "s1", name: "Пятибалльная" } as const;

function preview(
  cascadeTag: string,
  options: {
    entries?: readonly object[];
    blockers?: readonly object[];
    moreCount?: number;
  } = {},
) {
  return Response.json({
    entries: options.entries ?? [],
    blockers: options.blockers ?? [],
    moreCount: options.moreCount ?? 0,
    cascadeTag,
  });
}

function Harness() {
  const { publish, offerApproval } = usePublishEntry();
  return (
    <>
      <button type="button" onClick={() => void publish(TARGET, "approve")}>
        утвердить
      </button>
      <button type="button" onClick={() => void offerApproval(TARGET, "Draft")}>
        предложить
      </button>
      <button
        type="button"
        onClick={() => void offerApproval(TARGET, "Active")}
      >
        предложить действующей
      </button>
    </>
  );
}

function renderHarness() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter([{ path: "/", element: <Harness /> }])}
      />
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

const CATALOG_PROBE = [...catalogQueryKey, "probe"];

function isCatalogStale() {
  return queryClient.getQueryState(CATALOG_PROBE)?.isInvalidated ?? false;
}

function queryOf(request: Request | undefined) {
  return new URL(request?.url ?? "http://x").searchParams;
}

beforeEach(() => {
  queryClient.clear();
  queryClient.setQueryData(CATALOG_PROBE, 1);
});

describe("approving an entry under the list of what is published with it (AC-110…AC-112)", () => {
  it("approves at once under the tag of an empty list, with the version the entry was shown with", async () => {
    fakeApi.on(PREVIEW_ROUTE, () => preview("tag-empty"));
    fakeApi.on(APPROVE_ROUTE, () => Response.json({ id: "c1" }));
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "утвердить" }));

    await waitFor(() => expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1));
    const request = fakeApi.calls(APPROVE_ROUTE)[0];
    expect(request?.headers.get("If-Match")).toBe('"4"');
    expect(queryOf(request).get("cascade")).toBe("tag-empty");
    expect(document.querySelector(".ant-modal-confirm")).toBeNull();
    expect(document.querySelector(".ant-message-notice")).toBeNull();
    await waitFor(() => expect(isCatalogStale()).toBe(true));
  });

  it("names every draft that goes with the entry and approves under their tag once confirmed", async () => {
    fakeApi.on(PREVIEW_ROUTE, () =>
      preview("tag-two", { entries: [DIRECTION, SCALE] }),
    );
    fakeApi.on(APPROVE_ROUTE, () => Response.json({ id: "c1" }));
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "утвердить" }));

    const dialog = await confirmDialog();
    expect(dialog).toHaveTextContent(
      "Утвердить компетенцию «Работа в команде»?",
    );
    expect(dialog).toHaveTextContent(
      "Вместе с ней станут действующими 2 черновые записи",
    );
    expect(dialog).toHaveTextContent("Область Разработка");
    expect(dialog).toHaveTextContent("Шкала Пятибалльная");
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(0);
    await user.click(
      screen.getByRole("button", { name: "Утвердить вместе с 2 записями" }),
    );
    await waitFor(() => expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1));
    expect(queryOf(fakeApi.calls(APPROVE_ROUTE)[0]).get("cascade")).toBe(
      "tag-two",
    );
    expect(
      await screen.findByText(/вместе с ней опубликовано записей: 2/),
    ).toBeInTheDocument();
  });

  it("shows the new list, saying it changed, when the server refuses the tag, and does not retry by itself", async () => {
    let reads = 0;
    fakeApi.on(PREVIEW_ROUTE, () => {
      reads++;
      return reads === 1
        ? preview("tag-old", { entries: [DIRECTION] })
        : preview("tag-new", { entries: [DIRECTION, SCALE] });
    });
    let attempts = 0;
    fakeApi.on(APPROVE_ROUTE, () => {
      attempts++;
      return attempts === 1
        ? problem(412, {
            publication: "/api/v1/career/competencies/c1/publication",
          })
        : Response.json({ id: "c1" });
    });
    const user = renderHarness();
    await user.click(screen.getByRole("button", { name: "утвердить" }));
    await confirmDialog();

    await user.click(
      screen.getByRole("button", { name: "Утвердить вместе с 1 записью" }),
    );

    await waitFor(() =>
      expect(
        screen.getAllByText("Перечень изменился — подтвердите заново").length,
      ).toBeGreaterThan(0),
    );
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1);
    expect(document.body).toHaveTextContent(
      "Вместе с ней станут действующими 2 черновые записи",
    );
    expect(document.body).toHaveTextContent("состав публикуемого изменился");
    await user.click(
      await screen.findByRole("button", {
        name: "Утвердить вместе с 2 записями",
      }),
    );
    await waitFor(() => expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(2));
    expect(queryOf(fakeApi.calls(APPROVE_ROUTE)[1]).get("cascade")).toBe(
      "tag-new",
    );
  });

  it("asks before approving a record that had no drafts when drafts have appeared meanwhile, and says so", async () => {
    let reads = 0;
    fakeApi.on(PREVIEW_ROUTE, () => {
      reads++;
      return reads === 1
        ? preview("tag-none")
        : preview("tag-appeared", { entries: [SCALE] });
    });
    let attempts = 0;
    fakeApi.on(APPROVE_ROUTE, () => {
      attempts++;
      return attempts === 1
        ? problem(412, {
            publication: "/api/v1/career/competencies/c1/publication",
          })
        : Response.json({ id: "c1" });
    });
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "утвердить" }));

    const dialog = await confirmDialog();
    expect(dialog).toHaveTextContent(
      "у записи появились черновые связанные записи",
    );
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1);
    await user.click(
      screen.getByRole("button", { name: "Утвердить вместе с 1 записью" }),
    );
    await waitFor(() => expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(2));
    expect(queryOf(fakeApi.calls(APPROVE_ROUTE)[1]).get("cascade")).toBe(
      "tag-appeared",
    );
  });

  it("asks again, saying the list changed, even when the new list is empty, and sends nothing by itself", async () => {
    let reads = 0;
    fakeApi.on(PREVIEW_ROUTE, () => {
      reads++;
      return reads === 1
        ? preview("tag-old", { entries: [DIRECTION] })
        : preview("tag-empty");
    });
    let attempts = 0;
    fakeApi.on(APPROVE_ROUTE, () => {
      attempts++;
      return attempts === 1
        ? problem(412, {
            publication: "/api/v1/career/competencies/c1/publication",
          })
        : Response.json({ id: "c1" });
    });
    const user = renderHarness();
    await user.click(screen.getByRole("button", { name: "утвердить" }));
    await confirmDialog();

    await user.click(
      screen.getByRole("button", { name: "Утвердить вместе с 1 записью" }),
    );

    await waitFor(() =>
      expect(
        screen.getAllByText("Перечень изменился — подтвердите заново").length,
      ).toBeGreaterThan(0),
    );
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1);
    await user.click(await screen.findByRole("button", { name: "Утвердить" }));
    await waitFor(() => expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(2));
    expect(queryOf(fakeApi.calls(APPROVE_ROUTE)[1]).get("cascade")).toBe(
      "tag-empty",
    );
  });

  it("explains what stops the publication, naming the records, and sends nothing", async () => {
    fakeApi.on(PREVIEW_ROUTE, () =>
      preview("tag-blocked", {
        entries: [DIRECTION],
        blockers: [
          {
            kind: "Level",
            id: "l1",
            name: "Младший",
            reason: "NoActivePosition",
            context: "Бэкенд",
          },
        ],
        moreCount: 2,
      }),
    );
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "утвердить" }));

    const dialog = await confirmDialog();
    expect(dialog).toHaveTextContent(
      "Запись «Работа в команде» нельзя сделать действующей",
    );
    expect(dialog).toHaveTextContent(
      "Уровень «Младший»: нет действующей должности (Бэкенд)",
    );
    expect(dialog).toHaveTextContent("2 других связанных записи");
    expect(dialog).toHaveTextContent("свяжите должности с уровнями");
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(0);
  });

  it("explains a refusal the server gives at the moment of approval, with the way to the track's positions", async () => {
    fakeApi.on(PREVIEW_ROUTE, () => preview("tag-ok"));
    fakeApi.on(APPROVE_ROUTE, () =>
      problem(409, {
        detail: "Запись «Работа в команде» не утверждена",
        blockers: [
          {
            kind: "Level",
            id: "l1",
            name: "Старший",
            reason: "NoActivePosition",
            context: null,
          },
          {
            kind: "Track",
            id: "t1",
            name: "Бэкенд",
            reason: "NoLevels",
            context: null,
          },
        ],
        moreCount: 0,
      }),
    );
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "утвердить" }));

    const dialog = await confirmDialog();
    expect(dialog).toHaveTextContent("Запись «Работа в команде» не утверждена");
    expect(dialog).toHaveTextContent(
      "Уровень «Старший»: нет действующей должности",
    );
    expect(dialog).toHaveTextContent("Трек «Бэкенд»: у трека нет уровней");
    expect(
      screen.getByRole("link", { name: "Связать должности с треком" }),
    ).toHaveAttribute("href", "/desk/track-positions/t1");
  });

  it("counts the further records that stop the publication when the server refuses at the moment of approval", async () => {
    fakeApi.on(PREVIEW_ROUTE, () => preview("tag-ok"));
    fakeApi.on(APPROVE_ROUTE, () =>
      problem(409, {
        detail: "Запись «Работа в команде» не утверждена",
        blockers: [
          {
            kind: "Level",
            id: "l1",
            name: "Старший",
            reason: "NoLevels",
            context: null,
          },
        ],
        moreCount: 2,
      }),
    );
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "утвердить" }));

    expect(await confirmDialog()).toHaveTextContent(
      "2 других связанных записи",
    );
  });

  it("reports a stale version of the entry and rereads, without asking again", async () => {
    fakeApi.on(PREVIEW_ROUTE, () => preview("tag-ok"));
    fakeApi.on(APPROVE_ROUTE, () => problem(412));
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "утвердить" }));

    await waitFor(() => expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(1));
    await waitFor(() =>
      expect(document.querySelector(".ant-message-notice")).not.toBeNull(),
    );
    expect(document.querySelector(".ant-modal-confirm")).toBeNull();
    expect(fakeApi.calls(PREVIEW_ROUTE)).toHaveLength(1);
    await waitFor(() => expect(isCatalogStale()).toBe(true));
  });
});

describe("the offer to approve a draft right after it was created (AC-115)", () => {
  it("offers the approval to whoever may give it, naming the drafts that go with it, and can be put off", async () => {
    fakeApi.on(PREVIEW_ROUTE, () =>
      preview("tag-offer", { entries: [DIRECTION] }),
    );
    const user = renderHarness();

    await user.click(screen.getByRole("button", { name: "предложить" }));

    const dialog = await confirmDialog();
    expect(dialog).toHaveTextContent(
      "Компетенция «Работа в команде» создана черновиком",
    );
    expect(dialog).toHaveTextContent("Запись ссылается на черновые записи");
    expect(
      screen.getByRole("button", { name: "Не сейчас" }),
    ).toBeInTheDocument();
    expect(fakeApi.calls(APPROVE_ROUTE)).toHaveLength(0);
  });

  it("makes no offer for an entry that is not a draft", async () => {
    fakeApi.on(PREVIEW_ROUTE, () =>
      preview("tag-offer", { entries: [DIRECTION] }),
    );
    const user = renderHarness();

    await user.click(
      screen.getByRole("button", { name: "предложить действующей" }),
    );

    expect(fakeApi.calls(PREVIEW_ROUTE)).toHaveLength(0);
    expect(document.querySelector(".ant-modal-confirm")).toBeNull();
  });

  it.each([403, 404])(
    "makes no offer, and no complaint, to a user the preview answers %i",
    async (status) => {
      fakeApi.on(PREVIEW_ROUTE, () => problem(status));
      const user = renderHarness();

      await user.click(screen.getByRole("button", { name: "предложить" }));

      await waitFor(() => expect(fakeApi.calls(PREVIEW_ROUTE)).toHaveLength(1));
      expect(document.querySelector(".ant-modal-confirm")).toBeNull();
      expect(document.querySelector(".ant-message-notice")).toBeNull();
    },
  );
});
