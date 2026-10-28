import { describe, expect, it } from "vitest";
import { paths as orgPaths } from "../org-structure/paths";
import type {
  DeskGroup,
  DeskItem,
  DeskKind,
  DeskObject,
  DeskReason,
} from "./deskApi";
import {
  connectUnitTarget,
  deskPaths,
  itemAction,
  itemLink,
  objectContext,
  objectTitle,
  reasonLines,
  typeLabelOf,
} from "./deskTexts";

const OBJECT_ID = "8f1c0a52-0000-4000-8000-000000000001";
const VERSION_ID = "8f1c0a52-0000-4000-8000-000000000002";
const OTHER_ID = "8f1c0a52-0000-4000-8000-000000000003";
const NOON_UTC = "2026-10-09T12:00:00Z";
const NOON_DATE = /09\.10\.2026/;

function objectOf(
  type: DeskObject["type"],
  id = OBJECT_ID,
  name = "Имя",
): DeskObject {
  return { type, id, name };
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

function withReason(
  kind: DeskKind,
  reason: Omit<DeskReason, "kind"> = {},
  item: Partial<DeskItem> = {},
): DeskItem {
  return {
    object: objectOf("Matrix"),
    group: "ReturnedVersions",
    isReference: false,
    ...item,
    reasons: [{ kind, ...reason }],
  };
}

const textsOf = (item: DeskItem) => reasonLines(item).map((line) => line.text);
const firstTextOf = (item: DeskItem) => textsOf(item)[0] ?? "";

const VERSION = objectOf("MatrixVersion", VERSION_ID);
const PERSON = objectOf("Employee", OTHER_ID, "Иванов Иван Иванович");
const SHORT_PERSON = "Иванов И. И.";

describe("the addresses the e-mails link to (AC-110)", () => {
  it("keeps the review, rework and drafts routes the mail letters are built from", () => {
    expect(deskPaths.review(VERSION_ID)).toBe(`/desk/review/${VERSION_ID}`);
    expect(deskPaths.rework(VERSION_ID)).toBe(`/desk/rework/${VERSION_ID}`);
    expect(deskPaths.draft("competency", OBJECT_ID)).toBe(
      `/desk/drafts/competency/${OBJECT_ID}`,
    );
  });
});

const ACTION_PATH: Record<DeskKind, string | null> = {
  VersionReturned: `/desk/rework/${VERSION_ID}`,
  VersionNotSubmitted: `/desk/matrix-builder/${VERSION_ID}`,
  VersionAwaitsHeadApproval: `/desk/review/${VERSION_ID}`,
  VersionAwaitsHrApproval: `/desk/review/${VERSION_ID}`,
  MatrixLevelsChanged: `/career/matrices/${OBJECT_ID}`,
  MatrixUsesInactive: `/career/matrices/${OBJECT_ID}`,
  MatrixCompetencyChanged: null,
  MatrixSharedBlockChanged: `/desk/matrix-builder?matrixId=${OBJECT_ID}`,
  OwnVersionInApproval: null,
  VersionStuckAtHead: null,
  UnitWithoutMatrix: `/desk/matrix-builder?unitId=${OBJECT_ID}`,
  ResponsibleUnitWithoutMatrix: null,
  UnitWithoutHead: `/desk/connect-unit/${OBJECT_ID}`,
  ResponsibleUnitWithoutHead: null,
  UnitWithoutHr: `/desk/connect-unit/${OBJECT_ID}`,
  UnitWithoutPositions: `/desk/connect-unit/${OBJECT_ID}`,
  HeadWithoutAccount: `/desk/connect-unit/${OBJECT_ID}`,
  HeadAccountInvited: `/desk/connect-unit/${OBJECT_ID}`,
  HeadAccountBlocked: `/desk/connect-unit/${OBJECT_ID}`,
  EmptyUnit: null,
  DraftAwaitsApproval: deskPaths.drafts,
  TrackWithoutLevels: "/career",
  TrackWithOneLevel: "/career",
  TrackWithoutPositions: `/desk/track-positions/${OBJECT_ID}`,
  LevelWithoutPosition: `/desk/track-positions/${OBJECT_ID}`,
  OwnDraftAwaitsApproval: null,
  UnusedCompetency: null,
  UnusedScale: null,
  DirectionWithoutTracks: null,
  PositionWithoutTrack: deskPaths.trackPositions,
  InactivePositionInUse: null,
  UnusedPosition: null,
  PositionOutsideList: deskPaths.orgCleanup,
  NoTrackForPosition: null,
  AccountWithoutEmployee: deskPaths.orgCleanup,
  InvitationNotAccepted: null,
  PromotionWaveOpen: null,
  PromotionArtifactsRequired: `/promotion/applications/${OBJECT_ID}`,
  PromotionFeedbackPending: `/promotion/applications/${OBJECT_ID}`,
  PromotionRatingRequested: `/promotion/ratings/${OBJECT_ID}`,
  PromotionRatersReview: `/promotion/raters/${OBJECT_ID}`,
  PromotionFormalCheckPending: `/promotion/waves/${OBJECT_ID}`,
  PromotionDecisionsPending: `/promotion/review/${OBJECT_ID}`,
};

const REFERENCE_PATH: Partial<Record<DeskKind, string>> = {
  ResponsibleUnitWithoutMatrix: `${orgPaths.tree}?unit=${OBJECT_ID}`,
  ResponsibleUnitWithoutHead: `${orgPaths.tree}?unit=${OBJECT_ID}`,
  OwnVersionInApproval: `/career/matrices/${OBJECT_ID}`,
  VersionStuckAtHead: `/career/matrices/${OBJECT_ID}`,
  MatrixCompetencyChanged: `/career/matrices/${OBJECT_ID}`,
  OwnDraftAwaitsApproval: "/career",
  NoTrackForPosition: deskPaths.myPlace,
  TrackWithoutLevels: `/desk/track-positions/${OBJECT_ID}`,
  TrackWithOneLevel: `/desk/track-positions/${OBJECT_ID}`,
  TrackWithoutPositions: `/desk/track-positions/${OBJECT_ID}`,
  LevelWithoutPosition: `/desk/track-positions/${OBJECT_ID}`,
};

describe("the screen an action item opens (AC-6, AC-160, AC-209, AC-210, AC-218)", () => {
  it.each(Object.entries(ACTION_PATH))(
    "opens a %s item on the screen %s",
    (kind, path) => {
      const opened = itemAction(
        itemOf(kind as DeskKind, { target: VERSION }),
        true,
      );

      expect(opened?.path ?? null).toBe(path);
    },
  );

  it("opens no scenario for a version item that names no version", () => {
    expect(
      itemAction(itemOf("VersionReturned", { target: null }), true),
    ).toBeNull();
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
      expect(
        itemAction(itemOf("DraftAwaitsApproval", { type }), true)?.path,
      ).toBe(`/desk/drafts/${kind}/${OBJECT_ID}`);
    },
  );

  it("opens the whole drafts queue for a draft of a kind the queue has no entry for", () => {
    expect(
      itemAction(itemOf("DraftAwaitsApproval", { type: "Level" }), true)?.path,
    ).toBe(deskPaths.drafts);
  });

  it("preselects the unit, and the track too when exactly one is named, for a unit that needs a matrix", () => {
    const track = objectOf("Track", OTHER_ID);
    const one = itemAction(
      itemOf("UnitWithoutMatrix", { type: "OrgUnit", entries: [track] }),
      true,
    )?.path;
    const several = itemAction(
      itemOf("UnitWithoutMatrix", {
        type: "OrgUnit",
        entries: [track, objectOf("Track", VERSION_ID)],
      }),
      true,
    )?.path;

    expect(one).toBe(
      `/desk/matrix-builder?${new URLSearchParams({ unitId: OBJECT_ID, trackId: OTHER_ID })}`,
    );
    expect(several).toBe(
      `/desk/matrix-builder?${new URLSearchParams({ unitId: OBJECT_ID })}`,
    );
  });

  it("follows the first reason of an item with two, whatever the second asks (AC-160)", () => {
    const item: DeskItem = {
      ...itemOf("UnitWithoutHead", { type: "OrgUnit" }),
      reasons: [
        { kind: "UnitWithoutHead" },
        { kind: "InvitationNotAccepted", details: { linkState: "Expired" } },
      ],
      invitation: { accountId: OTHER_ID, version: 1 },
    };

    expect(itemAction(item, true)?.path).toBe(
      `/desk/connect-unit/${OBJECT_ID}`,
    );
    expect(itemLink(item, true)).toBe(`/desk/connect-unit/${OBJECT_ID}`);
  });

  const CONNECT_UNIT_KINDS = Object.entries(ACTION_PATH)
    .filter(([, path]) => path === `/desk/connect-unit/${OBJECT_ID}`)
    .map(([kind]) => kind as DeskKind);
  const UNIT_CARD = `${orgPaths.tree}?unit=${OBJECT_ID}`;

  it.each(CONNECT_UNIT_KINDS)(
    "opens the unit card, not the administrators' scenario, for a %s item when the user is not an administrator",
    (kind) => {
      const item = itemOf(kind, { type: "OrgUnit" });

      expect(itemAction(item, false)).toMatchObject({
        title: "Открыть карточку",
        path: UNIT_CARD,
      });
      expect(itemLink(item, false)).toBe(UNIT_CARD);
    },
  );

  it("labels the button of a unit with a gap by where it leads, for an administrator and for everyone else", () => {
    expect(connectUnitTarget(OBJECT_ID, true)).toMatchObject({
      path: `/desk/connect-unit/${OBJECT_ID}`,
      buttonLabel: "Открыть «Подключить подразделение»",
    });
    expect(connectUnitTarget(OBJECT_ID, false)).toMatchObject({
      path: UNIT_CARD,
      buttonLabel: "Открыть карточку подразделения",
    });
  });
});

describe("where an item's name leads (AC-6)", () => {
  it.each(Object.keys(ACTION_PATH) as DeskKind[])(
    "leads a reference %s item, in a group with no queue, where its kind says",
    (kind) => {
      const item = itemOf(kind, {
        isReference: true,
        group: "NoTrack",
      });

      expect(itemLink(item, true)).toBe(REFERENCE_PATH[kind] ?? null);
    },
  );

  it("leads an action to its scenario", () => {
    expect(itemLink(itemOf("VersionReturned", { target: VERSION }), true)).toBe(
      `/desk/rework/${VERSION_ID}`,
    );
  });

  it("leads a reference item of a group with a queue to the queue", () => {
    expect(
      itemLink(
        itemOf("UnusedCompetency", {
          type: "Competency",
          isReference: true,
          group: "UnusedCatalogEntries",
        }),
        true,
      ),
    ).toBe(deskPaths.catalogCleanup);
  });
});

describe("what the row shows of the object (AC-5)", () => {
  it.each([
    ["Matrix", null],
    ["MatrixVersion", null],
    ["OrgUnit", null],
    ["Track", "Трек"],
    ["Position", "Должность"],
    ["UserAccount", "Учётная запись"],
  ] as const)("tags a %s with %s", (type, label) => {
    expect(
      typeLabelOf({ ...withReason("EmptyUnit"), object: objectOf(type) }),
    ).toBe(label);
  });

  it("tags no invitation, whose address says what it is", () => {
    expect(
      typeLabelOf({
        ...withReason("InvitationNotAccepted"),
        object: objectOf("UserAccount"),
        group: "Invitations",
      }),
    ).toBeNull();
  });

  it("names a matrix with the last place of its context, a unit with its type, others as they are", () => {
    expect(
      objectTitle({ ...objectOf("Matrix"), context: ["Верх", "Низ"] }),
    ).toMatch(/^Матрица «Имя».*Низ$/);
    expect(objectTitle(objectOf("Matrix"))).toBe("Матрица «Имя»");
    expect(objectTitle(objectOf("OrgUnit"))).toMatch(/^Подразделение «Имя»$/);
    expect(objectTitle(objectOf("Track"))).toBe("Имя");
  });

  it("puts the version number, and the state it is in, under a matrix", () => {
    const context = (kind: DeskKind, details: DeskReason["details"]) =>
      objectContext(withReason(kind, { details }));

    expect(context("VersionReturned", { number: 3, hasChanges: true })).toMatch(
      /3.*Возвращена с правками/,
    );
    expect(context("VersionReturned", { number: 3 })).not.toMatch(/правками/);
    expect(context("VersionNotSubmitted", { number: 3 })).toMatch(/Черновик/);
    expect(
      context("VersionAwaitsHeadApproval", { number: 3, person: PERSON }),
    ).toContain(`автор ${SHORT_PERSON}`);
    expect(context("MatrixUsesInactive", { number: 3 })).toMatch(
      /^Опубликована, версия 3$/,
    );
    expect(context("VersionStuckAtHead", { number: 3 })).toMatch(/^Версия 3$/);
    expect(context("VersionReturned", {})).toBeNull();
  });

  it("puts the units above a unit under it, and nothing when it has none", () => {
    const unit = (context: string[] | undefined): DeskItem => ({
      ...withReason("EmptyUnit"),
      object: { ...objectOf("OrgUnit"), context },
    });

    expect(objectContext(unit(["Верх", "Низ"]))).toBe("Верх → Низ");
    expect(objectContext(unit([]))).toBeNull();
  });

  it("puts the direction of a track and, for a draft, its author under it", () => {
    const track = (kind: DeskKind, details?: DeskReason["details"]) =>
      objectContext({
        ...withReason(kind, { details }),
        object: { ...objectOf("Track"), context: ["Область"] },
      });

    expect(track("TrackWithoutLevels")).toBe("Область");
    expect(track("DraftAwaitsApproval", { person: PERSON })).toBe(
      `Область · автор ${SHORT_PERSON}`,
    );
  });

  it("says a draft of another kind is common and names its author", () => {
    const entry = withReason("DraftAwaitsApproval", {
      details: { person: PERSON },
    });

    expect(objectContext({ ...entry, object: objectOf("Scale") })).toBe(
      `Общая · автор ${SHORT_PERSON}`,
    );
    expect(
      objectContext({
        ...withReason("UnusedScale"),
        object: { ...objectOf("Scale"), context: ["А", "Б"] },
      }),
    ).toBe("А · Б");
  });
});

describe("the reasons an item states (AC-5, AC-160)", () => {
  it.each<[DeskReason["details"], string[]]>([
    [{ hasChanges: true, hasComment: true }, ["правками", "комментарием"]],
    [{ hasChanges: true }, ["правками"]],
    [{ hasComment: true }, ["комментарием"]],
    [{}, []],
  ])("says what a returned version came back with: %j", (details, tokens) => {
    const text = firstTextOf(withReason("VersionReturned", { details }));

    for (const token of ["правками", "комментарием"]) {
      expect(text.includes(token)).toBe(tokens.includes(token));
    }
  });

  it("names the step a draft stopped at, and none when the step is not known", () => {
    expect(
      firstTextOf(withReason("VersionNotSubmitted", { details: { step: 2 } })),
    ).toMatch(/шаге 2 «Компетенции»/);
    expect(firstTextOf(withReason("VersionNotSubmitted"))).not.toMatch(/шаге/);
  });

  it("tells the approver which step is theirs", () => {
    expect(firstTextOf(withReason("VersionAwaitsHeadApproval"))).toMatch(
      /шаг 1/,
    );
    expect(firstTextOf(withReason("VersionAwaitsHrApproval"))).toMatch(/шаг 2/);
  });

  it("puts a single inactive entry in its own gender and several in the plural", () => {
    const inactive = (entries: DeskObject[]) =>
      firstTextOf(withReason("MatrixUsesInactive", { details: { entries } }));

    expect(inactive([objectOf("Track")])).toMatch(/трек «Имя» неактивен$/);
    expect(inactive([objectOf("Competency")])).toMatch(/неактивна$/);
    expect(inactive([objectOf("Track"), objectOf("Competency")])).toMatch(
      /неактивны$/,
    );
  });

  it("lists the level changes of a track, each kind in the singular or the plural, and only those that happened", () => {
    const levels = (...names: string[]) =>
      names.map((name) => objectOf("Level", OTHER_ID, name));
    const changed = (details: DeskReason["details"]) =>
      firstTextOf(withReason("MatrixLevelsChanged", { details }));

    const text = changed({
      addedLevels: levels("Один"),
      removedLevels: levels("Два", "Три"),
      movedLevels: [],
    });

    expect(text).toContain("добавлен «Один»");
    expect(text).toContain("удалены «Два», «Три»");
    expect(text).not.toContain("переставлен");
    expect(changed({ movedLevels: levels("Один") })).toContain("переставлен ");
  });

  it("gives every changed shared block its own line", () => {
    const lines = textsOf(
      withReason("MatrixSharedBlockChanged", {
        details: {
          entries: [
            objectOf("Block", OTHER_ID, "А"),
            objectOf("Block", "b", "Б"),
          ],
        },
      }),
    );

    expect(lines).toHaveLength(2);
    expect(lines[0]).toContain("«А»");
    expect(lines[1]).toContain("«Б»");
  });

  it.each([
    ["MatrixCompetencyChanged", /^Компетенция /, /^Компетенции /],
    ["UnitWithoutMatrix", /действующий трек/, /действующие треки/],
  ] as const)(
    "%s: puts one entry in the singular and several in the plural",
    (kind, one, many) => {
      const named = (count: number) =>
        firstTextOf(
          withReason(kind, {
            details: {
              entries: Array.from({ length: count }, (_, index) =>
                objectOf("Competency", `id-${index}`, `Имя ${index}`),
              ),
            },
          }),
        );

      expect(named(1)).toMatch(one);
      expect(named(2)).toMatch(many);
      expect(named(2)).toContain("«Имя 0», «Имя 1»");
    },
  );

  it("names whose move it is, and which step, for a version of the user's own", () => {
    expect(
      firstTextOf(
        withReason("OwnVersionInApproval", {
          details: { person: PERSON, step: 2 },
        }),
      ),
    ).toContain(`${SHORT_PERSON} (шаг 2)`);
    expect(firstTextOf(withReason("OwnVersionInApproval"))).toMatch(
      /ход у согласующего \(шаг 1\)/,
    );
    expect(firstTextOf(withReason("VersionStuckAtHead"))).not.toContain(
      "ход у",
    );
    expect(
      firstTextOf(
        withReason("VersionStuckAtHead", { details: { person: PERSON } }),
      ),
    ).toContain(`ход у ${SHORT_PERSON}`);
  });

  it("names the head of a unit without a matrix in full when the reader may see it", () => {
    expect(
      firstTextOf(
        withReason("ResponsibleUnitWithoutMatrix", {
          details: { person: PERSON },
        }),
      ),
    ).toContain(`Руководитель: ${PERSON.name}`);
    expect(
      firstTextOf(withReason("ResponsibleUnitWithoutMatrix")),
    ).not.toContain("Руководитель");
  });

  it("adds to a unit without a head that only an administrator may assign one, when the reader is responsible", () => {
    expect(firstTextOf(withReason("UnitWithoutHead"))).not.toMatch(
      /администратор/,
    );
    expect(firstTextOf(withReason("ResponsibleUnitWithoutHead"))).toMatch(
      /администратор/,
    );
  });

  it.each([
    ["Blocked", "заблокирована"],
    ["Invited", "не приняла приглашение"],
    ["NoHrRole", "«HR»"],
    ["None", "не может действовать"],
  ] as const)(
    "says why the assigned HR of a unit cannot act: %s",
    (accountState, token) => {
      expect(
        firstTextOf(withReason("UnitWithoutHr", { details: { accountState } })),
      ).toContain(token);
      expect(firstTextOf(withReason("UnitWithoutHr"))).not.toContain(":");
    },
  );

  it("counts the employees of a unit with no positions, and a missing count as none", () => {
    expect(
      firstTextOf(
        withReason("UnitWithoutPositions", { details: { employeeCount: 4 } }),
      ),
    ).toMatch(/: 4$/);
    expect(firstTextOf(withReason("UnitWithoutPositions"))).toMatch(/: 0$/);
  });

  it.each([
    ["HeadWithoutAccount", "нет учётной записи"],
    ["HeadAccountInvited", "приглашение не принято"],
    ["HeadAccountBlocked", "заблокирована"],
  ] as const)(
    "says what is wrong with the account of the head: %s",
    (kind, token) => {
      const text = firstTextOf(
        withReason(kind, { details: { person: PERSON } }),
      );

      expect(text).toContain(SHORT_PERSON);
      expect(text).toContain(token);
    },
  );

  it("flags a draft track as too early to approve when it has no levels or a level with no position", () => {
    const draft = (type: DeskObject["type"], details: DeskReason["details"]) =>
      reasonLines(
        withReason(
          "DraftAwaitsApproval",
          { details },
          { object: objectOf(type) },
        ),
      );

    expect(
      draft("Track", { levelCount: 0 }).map((line) => line.isEarly),
    ).toEqual([undefined, true]);
    expect(
      draft("Track", { levelCount: 2, entries: [objectOf("Level")] }).at(-1)
        ?.isEarly,
    ).toBe(true);
    expect(draft("Track", { levelCount: 2 })).toHaveLength(1);
    expect(draft("Scale", { levelCount: 0 })).toHaveLength(1);
  });

  it.each([
    ["TrackWithoutLevels"],
    ["TrackWithOneLevel"],
    ["TrackWithoutPositions"],
    ["PositionWithoutTrack"],
  ] as const)(
    "%s: points a reference item to the responsible HR, and an action item to nobody",
    (kind) => {
      const reference = withReason(
        kind,
        { details: { person: PERSON } },
        { isReference: true },
      );

      expect(firstTextOf(reference)).toContain(
        `ответственный HR: ${SHORT_PERSON}`,
      );
      expect(firstTextOf({ ...reference, isReference: false })).not.toContain(
        "HR",
      );
      expect(
        firstTextOf(withReason(kind, {}, { isReference: true })),
      ).not.toContain("HR");
    },
  );

  it("puts a level with no position in the singular and several in the plural", () => {
    const levels = (count: number) =>
      firstTextOf(
        withReason("LevelWithoutPosition", {
          details: {
            entries: Array.from({ length: count }, (_, index) =>
              objectOf("Level", `id-${index}`, `У ${index}`),
            ),
          },
        }),
      );

    expect(levels(1)).toMatch(/^У уровня /);
    expect(levels(2)).toMatch(/^У уровней /);
  });

  it("names the position and the unit that does not list it", () => {
    expect(
      firstTextOf(
        withReason("PositionOutsideList", {
          details: {
            entries: [
              objectOf("Position", "p", "Должность 1"),
              objectOf("OrgUnit", "u", "Отдел 1"),
            ],
          },
        }),
      ),
    ).toMatch(/«Должность 1».*«Отдел 1»/);
  });

  it("adds the missing-card line to an account without an employee only when it has the HR role", () => {
    expect(textsOf(withReason("AccountWithoutEmployee"))).toHaveLength(1);
    expect(
      textsOf(
        withReason("AccountWithoutEmployee", { details: { hasHrRole: true } }),
      ),
    ).toHaveLength(2);
  });

  it("dates the link in the sentence outside the invitation group, whose own column carries the term", () => {
    const link = (
      linkState: "Expired" | "ExpiringSoon" | "Revoked" | null,
      group: DeskGroup,
    ) =>
      firstTextOf(
        withReason(
          "InvitationNotAccepted",
          { occurredAt: NOON_UTC, details: { linkState } },
          { group },
        ),
      );

    expect(link("Expired", "UnitsToConnect")).toMatch(/истекла.*09.10.2026/);
    expect(link("ExpiringSoon", "UnitsToConnect")).toMatch(
      /истечёт.*09.10.2026/,
    );
    expect(link("Revoked", "UnitsToConnect")).toMatch(/отменена/);
    expect(link("ExpiringSoon", "Invitations")).toMatch(/скоро истечёт/);
    expect(link("ExpiringSoon", "Invitations")).not.toMatch(NOON_DATE);
    expect(link("Expired", "Invitations")).not.toMatch(NOON_DATE);
    expect(link(null, "Invitations")).toMatch(/истекла$/);
  });
});
