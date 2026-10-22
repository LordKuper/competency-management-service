import { describe, expect, it } from "vitest";
import type { CareerLevel } from "../career/catalogApi";
import type { MatrixContent } from "../career/matrixApi";
import {
  competencyIdsOf,
  sharedBlockIdsOf,
  withChosenCompetencies,
  withSharedBlocks,
} from "./builderContent";

const FIRST_LEVEL = "level-1";
const INACTIVE_LEVEL = "level-2";
const THIRD_LEVEL = "level-3";
const OWN = "competency-own";
const DROPPED = "competency-dropped";
const ADDED = "competency-added";
const BLOCK_A = "block-a";
const BLOCK_B = "block-b";
const BLOCK_C = "block-c";
const ONLY_IN_A = "member-only-a";
const IN_A_AND_B = "member-a-b";
const ONLY_IN_B = "member-only-b";
const ONLY_IN_C = "member-only-c";
const COMPOSITIONS = new Map([
  [BLOCK_A, [ONLY_IN_A, IN_A_AND_B]],
  [BLOCK_B, [IN_A_AND_B, ONLY_IN_B]],
  [BLOCK_C, [ONLY_IN_C]],
]);

const levelOf = (id: string, status: CareerLevel["status"]): CareerLevel => ({
  id,
  name: id,
  directionId: "direction",
  trackId: "track",
  position: 1,
  status,
  version: 1,
  canEdit: true,
  canDelete: true,
});
const TRACK_LEVELS = [
  levelOf(FIRST_LEVEL, "Active"),
  levelOf(INACTIVE_LEVEL, "Inactive"),
  levelOf(THIRD_LEVEL, "Active"),
];

const EMPTY: MatrixContent = {
  levels: [],
  competencies: [],
  blockWeights: [],
  requirements: [],
};

const requirement = (competencyId: string) => ({
  levelId: FIRST_LEVEL,
  competencyId,
  requiredScore: 3,
});

describe("the competencies chosen in step two of the matrix builder", () => {
  it("gives a version that is still empty the active levels of the track, and no inactive one", () => {
    const next = withChosenCompetencies(EMPTY, new Set([OWN]), TRACK_LEVELS);

    expect(next.levels.map((level) => level.levelId)).toEqual([
      FIRST_LEVEL,
      THIRD_LEVEL,
    ]);
  });

  it("leaves the levels of a version that already has some", () => {
    const content = {
      ...EMPTY,
      levels: [{ levelId: THIRD_LEVEL, minOverallPercent: 50 }],
    };

    const next = withChosenCompetencies(content, new Set([OWN]), TRACK_LEVELS);

    expect(next.levels).toEqual(content.levels);
  });

  it("takes out a competency that is no longer chosen with its requirements, and keeps the ones that are", () => {
    const content: MatrixContent = {
      ...EMPTY,
      levels: [{ levelId: FIRST_LEVEL, minOverallPercent: null }],
      competencies: [
        { competencyId: OWN, blockId: "own-block" },
        { competencyId: DROPPED, blockId: null },
      ],
      requirements: [requirement(OWN), requirement(DROPPED)],
    };

    const next = withChosenCompetencies(content, new Set([OWN]), TRACK_LEVELS);

    expect(next.competencies).toEqual([
      { competencyId: OWN, blockId: "own-block" },
    ]);
    expect(next.requirements).toEqual([requirement(OWN)]);
  });

  it("puts a newly chosen competency outside every block", () => {
    const content: MatrixContent = {
      ...EMPTY,
      levels: [{ levelId: FIRST_LEVEL, minOverallPercent: null }],
      competencies: [{ competencyId: OWN, blockId: "own-block" }],
    };

    const next = withChosenCompetencies(
      content,
      new Set([OWN, ADDED]),
      TRACK_LEVELS,
    );

    expect(
      next.competencies.map((item) => [item.competencyId, item.blockId]),
    ).toEqual([
      [OWN, "own-block"],
      [ADDED, null],
    ]);
  });

  it("changes nothing when the choice is what the version already holds", () => {
    const content: MatrixContent = {
      ...EMPTY,
      levels: [{ levelId: FIRST_LEVEL, minOverallPercent: null }],
      competencies: [{ competencyId: OWN, blockId: null }],
      requirements: [requirement(OWN)],
    };

    expect(
      withChosenCompetencies(content, new Set([OWN]), TRACK_LEVELS),
    ).toEqual(content);
  });
});

describe("the shared blocks chosen in step two of the matrix builder", () => {
  const withBlocks: MatrixContent = {
    ...EMPTY,
    levels: [{ levelId: FIRST_LEVEL, minOverallPercent: null }],
    competencies: [{ competencyId: OWN, blockId: null }],
    sharedBlocks: [{ blockId: BLOCK_A }, { blockId: BLOCK_B }],
    blockWeights: [
      { levelId: FIRST_LEVEL, blockId: BLOCK_A, percent: 40 },
      { levelId: FIRST_LEVEL, blockId: BLOCK_B, percent: 60 },
    ],
    requirements: [
      requirement(OWN),
      requirement(ONLY_IN_A),
      requirement(IN_A_AND_B),
      requirement(ONLY_IN_B),
    ],
  };

  it("takes a block out with its coefficients and the requirements only it brought", () => {
    const next = withSharedBlocks(withBlocks, [BLOCK_B], COMPOSITIONS);

    expect(sharedBlockIdsOf(next)).toEqual([BLOCK_B]);
    expect(next.blockWeights.map((weight) => weight.blockId)).toEqual([
      BLOCK_B,
    ]);
    expect(next.requirements.map((item) => item.competencyId).sort()).toEqual(
      [IN_A_AND_B, ONLY_IN_B, OWN].sort(),
    );
  });

  it("includes a new block by reference after the ones already there", () => {
    const next = withSharedBlocks(
      withBlocks,
      [BLOCK_A, BLOCK_B, BLOCK_C],
      COMPOSITIONS,
    );

    expect(sharedBlockIdsOf(next)).toEqual([BLOCK_A, BLOCK_B, BLOCK_C]);
    expect(next.competencies).toEqual(withBlocks.competencies);
  });

  it("changes nothing when the choice is what the version already holds", () => {
    expect(
      withSharedBlocks(withBlocks, [BLOCK_A, BLOCK_B], COMPOSITIONS),
    ).toEqual(withBlocks);
  });

  it("reads a document written before shared blocks existed as one without blocks", () => {
    expect(sharedBlockIdsOf(EMPTY)).toEqual([]);
    expect(
      sharedBlockIdsOf(withSharedBlocks(EMPTY, [BLOCK_C], COMPOSITIONS)),
    ).toEqual([BLOCK_C]);
    expect(competencyIdsOf(withBlocks)).toEqual(new Set([OWN]));
  });
});
