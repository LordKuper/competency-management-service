import type { Active, Over } from "@dnd-kit/core";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { stubRowLayout } from "../../test/stubRowLayout";
import type { CareerLevel, CareerTrack } from "./catalogApi";
import {
  arrangeLevels,
  type LevelRow,
  levelAnnouncements,
  movedOrder,
  type PendingLevel,
  TrackLevels,
} from "./TrackLevels";

const TRACK: CareerTrack = {
  id: "track",
  name: "Бэкенд",
  directionId: "direction",
  status: "Active",
  ownerOrgUnitId: "unit",
  ownerOrgUnitName: "Отдел",
  version: 1,
};

function level(id: string, name: string, position: number): CareerLevel {
  return {
    id,
    name,
    directionId: "direction",
    trackId: "track",
    position,
    status: "Active",
    version: 1,
  };
}

const SAVED = [
  level("junior", "Младший", 1),
  level("middle", "Средний", 2),
  level("senior", "Старший", 3),
];

const pending = (key: string, name: string): PendingLevel => ({
  key,
  values: { name },
});

const keys = (rows: readonly LevelRow[]) => rows.map((row) => row.key);
const active = (id: string) => ({ id }) as Active;
const over = (id: string) => ({ id }) as Over;

describe("arrangeLevels (AC-11)", () => {
  it("lists the saved levels in track order and the pending ones after them", () => {
    const rows = arrangeLevels(SAVED, [pending("new", "Ведущий")], undefined);

    expect(rows.map((row) => row.name)).toEqual([
      "Младший",
      "Средний",
      "Старший",
      "Ведущий",
    ]);
    expect(rows[3]?.pending?.key).toBe("new");
    expect(rows[0]?.level?.id).toBe("junior");
  });

  it("follows the order the user arranged, and keeps rows that order does not name after the named ones", () => {
    const rows = arrangeLevels(
      SAVED,
      [pending("new", "Ведущий")],
      ["senior", "junior"],
    );

    expect(keys(rows)).toEqual(["senior", "junior", "middle", "new"]);
  });

  it("puts a pending level that was moved among the saved ones where it was dropped", () => {
    const rows = arrangeLevels(
      SAVED,
      [pending("new", "Ведущий")],
      ["junior", "new", "middle", "senior"],
    );

    expect(keys(rows)).toEqual(["junior", "new", "middle", "senior"]);
  });
});

describe("movedOrder (AC-11)", () => {
  const rows = arrangeLevels(SAVED, [], undefined);

  it("moves a level onto the place of another, in either direction", () => {
    expect(
      movedOrder(rows, { active: active("junior"), over: over("senior") }),
    ).toEqual(["middle", "senior", "junior"]);
    expect(
      movedOrder(rows, { active: active("senior"), over: over("junior") }),
    ).toEqual(["senior", "junior", "middle"]);
  });

  it("changes nothing when the level is dropped where it was or nowhere", () => {
    expect(
      movedOrder(rows, { active: active("middle"), over: over("middle") }),
    ).toBeNull();
    expect(
      movedOrder(rows, { active: active("middle"), over: null }),
    ).toBeNull();
  });
});

describe("level announcements for a screen reader (AC-11)", () => {
  const rows = arrangeLevels(SAVED, [], undefined);
  const announcements = levelAnnouncements(rows);
  const event = (activeId: string, overId: string | null = null) => ({
    active: active(activeId),
    over: overId === null ? null : over(overId),
  });

  it("names the level and its place of the total when it is picked up", () => {
    expect(announcements.onDragStart?.(event("middle"))).toBe(
      "Взят уровень «Средний», позиция 2 из 3",
    );
  });

  it("says where the level is while it is moved over another's place", () => {
    expect(announcements.onDragOver?.(event("middle", "senior"))).toBe(
      "Уровень «Средний» на позиции 3 из 3",
    );
    expect(announcements.onDragOver?.(event("middle"))).toBeUndefined();
  });

  it("says where the level was dropped", () => {
    expect(announcements.onDragEnd?.(event("junior", "senior"))).toBe(
      "Уровень «Младший» перемещён на позицию 3 из 3",
    );
    expect(announcements.onDragEnd?.(event("junior"))).toBeUndefined();
  });

  it("says the move was cancelled and where the level stayed", () => {
    expect(announcements.onDragCancel?.(event("senior"))).toBe(
      "Перемещение отменено. Уровень «Старший» остался на позиции 3",
    );
  });
});

function renderLevels(rows: LevelRow[], onOrderChange = vi.fn()) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "a", email: "admin@test.local", role: "GlobalAdmin" }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  render(
    <Providers>
      <TrackLevels
        track={TRACK}
        rows={rows}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        pending={[]}
        onPendingChange={vi.fn()}
        onOrderChange={onOrderChange}
      />
    </Providers>,
  );
}

const originalScrollIntoView = Element.prototype.scrollIntoView;

beforeEach(() => {
  queryClient.clear();
  Element.prototype.scrollIntoView = vi.fn();
});

let restoreLayout: () => void = () => undefined;

afterEach(() => {
  Element.prototype.scrollIntoView = originalScrollIntoView;
  restoreLayout();
  restoreLayout = () => undefined;
});

describe("the level list of the track dialog (AC-11)", () => {
  it("shows the hint, a handle named after each level with the Russian role description, and the instructions for the keyboard", async () => {
    renderLevels(arrangeLevels(SAVED, [], undefined));

    expect(
      screen.getByText(
        "Уровни идут от младшего к старшему. Перетащите строку, чтобы изменить порядок",
      ),
    ).toBeInTheDocument();
    const handles = screen
      .getAllByRole("button", { name: /^Перетащить уровень/ })
      .map((handle) => handle.getAttribute("aria-label"));
    expect(handles).toEqual([
      "Перетащить уровень «Младший»",
      "Перетащить уровень «Средний»",
      "Перетащить уровень «Старший»",
    ]);
    const handle = screen.getByRole("button", {
      name: "Перетащить уровень «Средний»",
    });
    expect(handle).toHaveAttribute(
      "aria-roledescription",
      "перемещаемый элемент",
    );
    const description = document.getElementById(
      handle.getAttribute("aria-describedby") ?? "",
    );
    expect(description?.textContent).toBe(
      "Чтобы взять уровень, нажмите пробел или Enter. Перемещайте его стрелками вверх и вниз, отпустите пробелом или Enter, Escape — отмена",
    );
  });

  it("lists the levels from the most junior, with the status and the actions of each saved one", async () => {
    renderLevels(arrangeLevels(SAVED, [pending("new", "Ведущий")], undefined));

    const rows = (await screen.findAllByRole("row")).slice(1);
    expect(
      rows.map((row) => within(row).getAllByRole("cell")[1]?.textContent),
    ).toEqual(["Младший", "Средний", "Старший", "Ведущий"]);
    expect(
      within(rows[0] as HTMLElement).getByText("Действует"),
    ).toBeInTheDocument();
    expect(
      within(rows[3] as HTMLElement).getByRole("button", { name: "Удалить" }),
    ).toBeInTheDocument();
    expect(
      within(rows[3] as HTMLElement).queryByText("Действует"),
    ).not.toBeInTheDocument();
  });

  it("says «В треке пока нет уровней» for a track without levels", () => {
    renderLevels([]);

    expect(screen.getByText("В треке пока нет уровней")).toBeInTheDocument();
  });

  it("announces in Russian, through the live region, the level taken with the keyboard and where it is dropped, and reports no new order for a drop in place", async () => {
    const onOrderChange = vi.fn();
    renderLevels(arrangeLevels(SAVED, [], undefined), onOrderChange);
    const user = userEvent.setup();

    screen
      .getByRole("button", { name: "Перетащить уровень «Младший»" })
      .focus();
    await user.keyboard(" ");
    expect(
      await screen.findByText("Уровень «Младший» на позиции 1 из 3"),
    ).toBeInTheDocument();
    await user.keyboard(" ");

    expect(
      await screen.findByText("Уровень «Младший» перемещён на позицию 1 из 3"),
    ).toBeInTheDocument();
    expect(onOrderChange).not.toHaveBeenCalled();
  });

  it("moves a level down with the keyboard, says where it went, and reports the new order", async () => {
    const onOrderChange = vi.fn();
    renderLevels(arrangeLevels(SAVED, [], undefined), onOrderChange);
    restoreLayout = stubRowLayout();
    const user = userEvent.setup();

    screen
      .getByRole("button", { name: "Перетащить уровень «Младший»" })
      .focus();
    await user.keyboard(" ");
    await user.keyboard("{ArrowDown}");
    expect(
      await screen.findByText("Уровень «Младший» на позиции 2 из 3"),
    ).toBeInTheDocument();
    await user.keyboard(" ");

    expect(
      await screen.findByText("Уровень «Младший» перемещён на позицию 2 из 3"),
    ).toBeInTheDocument();
    expect(onOrderChange).toHaveBeenCalledExactlyOnceWith([
      "middle",
      "junior",
      "senior",
    ]);
  });

  it("leaves the order alone when the move is cancelled with Escape, and says so", async () => {
    const onOrderChange = vi.fn();
    renderLevels(arrangeLevels(SAVED, [], undefined), onOrderChange);
    restoreLayout = stubRowLayout();
    const user = userEvent.setup();

    screen
      .getByRole("button", { name: "Перетащить уровень «Средний»" })
      .focus();
    await user.keyboard(" ");
    await user.keyboard("{ArrowDown}");
    await user.keyboard("{Escape}");

    expect(
      await screen.findByText(
        "Перемещение отменено. Уровень «Средний» остался на позиции 2",
      ),
    ).toBeInTheDocument();
    expect(onOrderChange).not.toHaveBeenCalled();
  });
});
