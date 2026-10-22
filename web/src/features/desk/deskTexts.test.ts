import { describe, expect, it } from "vitest";
import type { DeskGroup, DeskItem, DeskKind, DeskObject } from "./deskApi";
import { deskPaths, itemAction, itemLink } from "./deskTexts";

const OBJECT_ID = "8f1c0a52-0000-4000-8000-000000000001";
const VERSION_ID = "8f1c0a52-0000-4000-8000-000000000002";
const OTHER_ID = "8f1c0a52-0000-4000-8000-000000000003";

function objectOf(type: DeskObject["type"], id = OBJECT_ID): DeskObject {
  return { type, id, name: "Имя" };
}

function itemOf(
  kind: DeskKind,
  options: {
    type?: DeskObject["type"];
    target?: DeskObject | null;
    isReference?: boolean;
    group?: DeskGroup;
    entries?: DeskObject[];
  } = {},
): DeskItem {
  return {
    object: objectOf(options.type ?? "Matrix"),
    group: options.group ?? "ReturnedVersions",
    isReference: options.isReference ?? false,
    reasons: [
      {
        kind,
        details: options.entries ? { entries: options.entries } : undefined,
      },
    ],
    target: options.target === null ? undefined : options.target,
  };
}

const VERSION = objectOf("MatrixVersion", VERSION_ID);

describe("the addresses the e-mails link to (AC-110)", () => {
  it("keeps the review, rework and drafts routes the mail letters are built from", () => {
    expect(deskPaths.review(VERSION_ID)).toBe(`/desk/review/${VERSION_ID}`);
    expect(deskPaths.rework(VERSION_ID)).toBe(`/desk/rework/${VERSION_ID}`);
    expect(deskPaths.draft("competency", OBJECT_ID)).toBe(
      `/desk/drafts/competency/${OBJECT_ID}`,
    );
  });
});

describe("the screen an action item opens (AC-6, AC-160)", () => {
  it.each([
    ["VersionReturned", `/desk/rework/${VERSION_ID}`],
    ["VersionNotSubmitted", `/desk/matrix-builder/${VERSION_ID}`],
    ["VersionAwaitsHeadApproval", `/desk/review/${VERSION_ID}`],
    ["VersionAwaitsHrApproval", `/desk/review/${VERSION_ID}`],
  ] as const)("opens a %s item on the version the item names", (kind, path) => {
    expect(itemAction(itemOf(kind, { target: VERSION }))?.path).toBe(path);
  });

  it("opens no scenario for a version item that names no version", () => {
    expect(itemAction(itemOf("VersionReturned", { target: null }))).toBeNull();
  });

  it.each([
    ["Direction", "direction"],
    ["Track", "track"],
    ["Competency", "competency"],
    ["Scale", "scale"],
    ["Block", "block"],
  ] as const)(
    "opens the draft %s on the queue's entry of its kind",
    (type, kind) => {
      expect(itemAction(itemOf("DraftAwaitsApproval", { type }))?.path).toBe(
        `/desk/drafts/${kind}/${OBJECT_ID}`,
      );
    },
  );

  it("opens the whole drafts queue for a draft of a kind the queue has no entry for", () => {
    expect(
      itemAction(itemOf("DraftAwaitsApproval", { type: "Level" }))?.path,
    ).toBe(deskPaths.drafts);
  });

  it("preselects the unit, and the track too when exactly one is named, for a unit that needs a matrix", () => {
    const track = objectOf("Track", OTHER_ID);
    const one = itemAction(
      itemOf("UnitWithoutMatrix", { type: "OrgUnit", entries: [track] }),
    )?.path;
    const several = itemAction(
      itemOf("UnitWithoutMatrix", {
        type: "OrgUnit",
        entries: [track, objectOf("Track", VERSION_ID)],
      }),
    )?.path;

    expect(one).toBe(
      `/desk/matrix-builder?${new URLSearchParams({ unitId: OBJECT_ID, trackId: OTHER_ID })}`,
    );
    expect(several).toBe(
      `/desk/matrix-builder?${new URLSearchParams({ unitId: OBJECT_ID })}`,
    );
  });

  it.each([
    "UnitWithoutHead",
    "HeadWithoutAccount",
    "HeadAccountInvited",
    "HeadAccountBlocked",
    "UnitWithoutHr",
    "UnitWithoutPositions",
  ] as const)("sends %s to connecting the unit it is about", (kind) => {
    expect(itemAction(itemOf(kind, { type: "OrgUnit" }))?.path).toBe(
      `/desk/connect-unit/${OBJECT_ID}`,
    );
  });

  it.each(["TrackWithoutPositions", "LevelWithoutPosition"] as const)(
    "sends %s to linking the positions of the track",
    (kind) => {
      expect(itemAction(itemOf(kind, { type: "Track" }))?.path).toBe(
        `/desk/track-positions/${OBJECT_ID}`,
      );
    },
  );

  it.each(["PositionOutsideList", "AccountWithoutEmployee"] as const)(
    "sends %s to the organisation queue",
    (kind) => {
      expect(itemAction(itemOf(kind))?.path).toBe(deskPaths.orgCleanup);
    },
  );

  it("leaves an item whose action is on the desk itself without a screen", () => {
    expect(
      itemAction(itemOf("InvitationNotAccepted", { type: "UserAccount" })),
    ).toBeNull();
  });
});

describe("where an item's name leads (AC-6)", () => {
  it("leads an action to its scenario and a note for reference to a view of the object", () => {
    expect(itemLink(itemOf("VersionReturned", { target: VERSION }))).toBe(
      `/desk/rework/${VERSION_ID}`,
    );
    expect(
      itemLink(
        itemOf("OwnVersionInApproval", {
          isReference: true,
          group: "VersionsInApproval",
        }),
      ),
    ).toBe(`/career/matrices/${OBJECT_ID}`);
    expect(
      itemLink(
        itemOf("NoTrackForPosition", {
          type: "Employee",
          isReference: true,
          group: "NoTrack",
        }),
      ),
    ).toBe(deskPaths.myPlace);
    expect(
      itemLink(
        itemOf("LevelWithoutPosition", {
          type: "Track",
          isReference: true,
          group: "PositionLinksInfo",
        }),
      ),
    ).toBe(`/desk/track-positions/${OBJECT_ID}`);
  });

  it("leads a reference item of a group with a queue to the queue", () => {
    expect(
      itemLink(
        itemOf("UnusedCompetency", {
          type: "Competency",
          isReference: true,
          group: "UnusedCatalogEntries",
        }),
      ),
    ).toBe(deskPaths.catalogCleanup);
  });
});
