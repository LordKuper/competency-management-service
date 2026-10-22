import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import { paths as orgPaths } from "../org-structure/paths";
import { DeskScenarios, type SessionRoles } from "./DeskScenarios";
import type {
  Desk,
  DeskGroup,
  DeskItem,
  DeskKind,
  DeskPrerequisite,
} from "./deskApi";
import { deskPaths } from "./deskTexts";

const NO_ROLES: SessionRoles = {
  isAdmin: false,
  isHead: false,
  isHr: false,
  isUnitHr: false,
  isBound: false,
};
const FIRST_UNIT = "Добавить первое подразделение";
const FIRST_POSITION = "Добавить первую должность";
const ASSIGN_HR = "Назначить HR";
const CONNECT = "Подключить подразделение";
const ONBOARD = "Принять сотрудника";
const LINK = "Связать должности с треком";
const BUILD = "Построить матрицу компетенций";
const MY_PLACE = "Моё место в компании";
const REWORK = "Доработать возвращённую версию";
const REVIEW = "Согласовать версию";
const DRAFTS = "Утвердить черновые записи";
const CATALOG = "Привести каталог в порядок";
const ORG = "Навести порядок в оргструктуре";
const NOTHING_TO_START = "Сейчас начать нечего";
const UNBOUND = "Сценариев для вашей учётной записи пока нет";
const ALL_PREREQUISITES: DeskPrerequisite[] = [
  "ActiveUnit",
  "ActivePosition",
  "WorkingEmployee",
  "VisibleTrack",
  "HrAccount",
];

const WAITING_ITEM_KINDS: Partial<Record<DeskGroup, DeskKind>> = {
  ReturnedVersions: "VersionReturned",
  VersionsToApprove: "VersionAwaitsHeadApproval",
};

function waitingItem(group: DeskGroup): DeskItem[] {
  const kind = WAITING_ITEM_KINDS[group];
  return kind
    ? [
        {
          object: { type: "Matrix", id: "matrix", name: "Трек" },
          group,
          isReference: false,
          reasons: [{ kind }],
          target: { type: "MatrixVersion", id: "version", name: "Трек" },
        },
      ]
    : [];
}

function deskWith(
  prerequisites: DeskPrerequisite[],
  waiting: Partial<Record<DeskGroup, number>> = {},
): Desk {
  return {
    actionCount: 0,
    prerequisites,
    groups: Object.entries(waiting).map(([group, total]) => ({
      group: group as DeskGroup,
      isReference: false,
      total,
      items: waitingItem(group as DeskGroup),
    })),
  };
}

function titlesShownFor(roles: SessionRoles, desk: Desk | undefined) {
  render(
    <MemoryRouter>
      <DeskScenarios roles={roles} desk={desk} />
    </MemoryRouter>,
  );
  return screen
    .queryAllByRole("heading", { level: 4 })
    .map((h) => h.textContent);
}

const hrefOf = (title: string) =>
  screen.getByRole("link", { name: title }).getAttribute("href");

describe("scenario cards of the desk (AC-204…AC-211, AC-218)", () => {
  it("shows an administrator the first-step cards for what the system lacks, and no scenario that needs it", () => {
    const titles = titlesShownFor({ ...NO_ROLES, isAdmin: true }, deskWith([]));

    expect(titles).toEqual([FIRST_UNIT, FIRST_POSITION, ASSIGN_HR]);
  });

  it("drops each first-step card as its prerequisite holds, and opens the scenarios that needed it", () => {
    const admin = { ...NO_ROLES, isAdmin: true };

    const withUnit = titlesShownFor(admin, deskWith(["ActiveUnit"]));

    expect(withUnit).toEqual([FIRST_POSITION, ASSIGN_HR]);
  });

  it("opens 'Принять сотрудника' with an active unit and an active position, and 'Назначить HR' goes once an HR account is there", () => {
    const titles = titlesShownFor(
      { ...NO_ROLES, isAdmin: true },
      deskWith(["ActiveUnit", "ActivePosition", "HrAccount"]),
    );

    expect(titles).toEqual([ONBOARD]);
  });

  it("opens 'Подключить подразделение' only with a unit, a working employee and a unit waiting to be connected", () => {
    const admin = { ...NO_ROLES, isAdmin: true };
    const prerequisites: DeskPrerequisite[] = [
      "ActiveUnit",
      "ActivePosition",
      "HrAccount",
    ];

    const nobodyWorks = titlesShownFor(
      admin,
      deskWith(prerequisites, { UnitsToConnect: 2 }),
    );
    document.body.innerHTML = "";
    const worksButNoneWaits = titlesShownFor(
      admin,
      deskWith([...prerequisites, "WorkingEmployee"]),
    );
    document.body.innerHTML = "";
    const connectable = titlesShownFor(
      admin,
      deskWith([...prerequisites, "WorkingEmployee"], { UnitsToConnect: 2 }),
    );

    expect(nobodyWorks).not.toContain(CONNECT);
    expect(worksButNoneWaits).not.toContain(CONNECT);
    expect(connectable).toEqual([CONNECT, ONBOARD, ORG]);
  });

  it("opens 'Связать должности с треком' for whoever may edit tracks, with a position, a track they may open and something to link", () => {
    for (const roles of [
      { ...NO_ROLES, isHead: true },
      { ...NO_ROLES, isUnitHr: true },
      { ...NO_ROLES, isAdmin: true },
    ]) {
      document.body.innerHTML = "";
      const titles = titlesShownFor(
        roles,
        deskWith(ALL_PREREQUISITES, { PositionsToLink: 3 }),
      );
      document.body.innerHTML = "";
      const withoutTrack = titlesShownFor(
        roles,
        deskWith(["ActivePosition"], { PositionsToLink: 3 }),
      );
      document.body.innerHTML = "";
      const nothingToLink = titlesShownFor(roles, deskWith(ALL_PREREQUISITES));

      expect(titles).toContain(LINK);
      expect(withoutTrack).not.toContain(LINK);
      expect(nothingToLink).not.toContain(LINK);
    }
  });

  it("shows a head the card to build a matrix always, and the ones to rework and review only while something waits", () => {
    const head = { ...NO_ROLES, isHead: true };

    const idle = titlesShownFor(head, deskWith([]));
    document.body.innerHTML = "";
    const busy = titlesShownFor(
      head,
      deskWith([], { ReturnedVersions: 1, VersionsToApprove: 4 }),
    );

    expect(idle).toEqual([BUILD]);
    expect(busy).toEqual([BUILD, REWORK, REVIEW]);
  });

  it("offers HR and the administrator the queues of drafts, catalogue and organisation by what waits in them", () => {
    const hr = { ...NO_ROLES, isHr: true };
    const admin = { ...NO_ROLES, isAdmin: true };
    const waiting = {
      DraftsToApprove: 2,
      UnusedCatalogEntries: 5,
      OrgCleanup: 1,
    };

    const forHr = titlesShownFor(hr, deskWith([], waiting));
    document.body.innerHTML = "";
    const forAdmin = titlesShownFor(
      admin,
      deskWith(["ActiveUnit", "ActivePosition", "HrAccount"], waiting),
    );

    expect(forHr).toEqual([DRAFTS, CATALOG]);
    expect(forAdmin).toEqual([DRAFTS, ONBOARD, CATALOG, ORG]);
  });

  it("puts 'Моё место в компании' first for anyone bound to an employee, whatever else they are", () => {
    const titles = titlesShownFor(
      { ...NO_ROLES, isBound: true, isHead: true },
      deskWith([]),
    );

    expect(titles).toEqual([MY_PLACE, BUILD]);
  });

  it("sends each first-step card to the screen that closes its gap (AC-204, AC-209, AC-210)", () => {
    titlesShownFor({ ...NO_ROLES, isAdmin: true }, deskWith([]));

    expect(hrefOf(FIRST_UNIT)).toBe(orgPaths.tree);
    expect(hrefOf(FIRST_POSITION)).toBe(deskPaths.positions);
    expect(hrefOf(ASSIGN_HR)).toBe(deskPaths.users);
  });

  it("sends each scenario card to its own screen, and the ones that work on a version to the first version waiting (AC-218)", () => {
    titlesShownFor(
      {
        isAdmin: true,
        isHead: true,
        isHr: true,
        isUnitHr: true,
        isBound: true,
      },
      deskWith(ALL_PREREQUISITES, {
        ReturnedVersions: 1,
        VersionsToApprove: 1,
        UnitsToConnect: 1,
        PositionsToLink: 1,
        DraftsToApprove: 1,
        UnusedCatalogEntries: 1,
        OrgCleanup: 1,
      }),
    );

    expect(hrefOf(MY_PLACE)).toBe(deskPaths.myPlace);
    expect(hrefOf(BUILD)).toBe(deskPaths.matrixBuilder);
    expect(hrefOf(REWORK)).toBe(deskPaths.rework("version"));
    expect(hrefOf(REVIEW)).toBe(deskPaths.review("version"));
    expect(hrefOf(DRAFTS)).toBe(deskPaths.drafts);
    expect(hrefOf(CONNECT)).toBe(deskPaths.connectUnit);
    expect(hrefOf(ONBOARD)).toBe(deskPaths.onboardEmployee);
    expect(hrefOf(LINK)).toBe(deskPaths.trackPositions);
    expect(hrefOf(CATALOG)).toBe(deskPaths.catalogCleanup);
    expect(hrefOf(ORG)).toBe(deskPaths.orgCleanup);
  });

  it("counts a queue as waiting in the queue and the rest as waiting for the user", () => {
    titlesShownFor(
      { ...NO_ROLES, isAdmin: true },
      deskWith(["ActiveUnit", "ActivePosition", "HrAccount"], {
        DraftsToApprove: 2,
        UnusedCatalogEntries: 5,
      }),
    );

    expect(screen.getByText("Ждут вас: 2")).toBeInTheDocument();
    expect(screen.getByText("В очереди: 5")).toBeInTheDocument();
  });

  it("shows no first-step or prerequisite card before the desk has answered", () => {
    const titles = titlesShownFor({ ...NO_ROLES, isAdmin: true }, undefined);

    expect(titles).toEqual([]);
    expect(screen.queryByText(NOTHING_TO_START)).not.toBeInTheDocument();
  });

  it("explains an account bound to no one and holding no role, without claiming the desk is empty", () => {
    titlesShownFor(NO_ROLES, deskWith([]));

    expect(screen.getByText(UNBOUND)).toBeInTheDocument();
    expect(screen.queryByText(NOTHING_TO_START)).not.toBeInTheDocument();
  });

  it("says neutrally that nothing can be started when every card of the roles is hidden on a loaded desk", () => {
    titlesShownFor({ ...NO_ROLES, isHr: true }, deskWith([]));

    expect(screen.getByText(NOTHING_TO_START)).toBeInTheDocument();
    expect(screen.queryByText(UNBOUND)).not.toBeInTheDocument();
  });
});
