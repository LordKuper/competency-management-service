import { describe, expect, it } from "vitest";
import type {
  MatrixChange,
  MatrixContent,
  MatrixRequirement,
} from "./matrixApi";
import {
  areAllCritical,
  cellKey,
  changedPlaces,
  distributeBlockWeights,
  evenPercents,
  hasOwnWeights,
  ownWeightSum,
  removeCompetency,
  removeLevel,
  setBlockWeight,
  setCompetencyWeight,
  setOwnWeights,
  withEvenBlockWeights,
  withoutOverallMins,
  withRequirement,
} from "./matrixContent";

const JUNIOR = "junior";
const SENIOR = "senior";
const CORE = "core";
const EXTRA = "extra";
const DESIGN = "design";
const TESTING = "testing";

function requirement(
  levelId: string,
  competencyId: string,
  requiredScore = 2,
): MatrixRequirement {
  return {
    levelId,
    competencyId,
    requiredScore,
  };
}

function content(): MatrixContent {
  return {
    levels: [
      { levelId: JUNIOR, minOverallPercent: 60 },
      { levelId: SENIOR, minOverallPercent: 70 },
    ],
    competencies: [
      { competencyId: DESIGN, blockId: CORE },
      { competencyId: TESTING, blockId: EXTRA },
    ],
    blockWeights: [
      { levelId: JUNIOR, blockId: CORE, percent: 100 },
      { levelId: SENIOR, blockId: CORE, percent: 70 },
      { levelId: SENIOR, blockId: EXTRA, percent: 30 },
    ],
    requirements: [
      requirement(JUNIOR, DESIGN),
      requirement(SENIOR, DESIGN),
      requirement(SENIOR, TESTING),
    ],
  };
}

const referencedLevels = (matrix: MatrixContent) => [
  ...matrix.blockWeights.map((weight) => weight.levelId),
  ...matrix.requirements.map((item) => item.levelId),
];

describe("editing the matrix document leaves no reference to what was removed (AC-14)", () => {
  it("drops the weights and the requirements of a removed level, and nothing of the other level", () => {
    const edited = removeLevel(content(), JUNIOR);

    expect(edited.levels.map((level) => level.levelId)).toEqual([SENIOR]);
    expect(referencedLevels(edited)).not.toContain(JUNIOR);
    expect(edited.blockWeights).toHaveLength(2);
    expect(edited.requirements).toHaveLength(2);
  });

  it("drops the requirements of a removed competency but keeps the weights of its block", () => {
    const edited = removeCompetency(content(), TESTING);

    expect(edited.competencies.map((item) => item.competencyId)).toEqual([
      DESIGN,
    ]);
    expect(
      edited.requirements.map((item) => `${item.levelId}:${item.competencyId}`),
    ).toEqual([`${JUNIOR}:${DESIGN}`, `${SENIOR}:${DESIGN}`]);
    expect(edited.blockWeights).toHaveLength(3);
  });
});

describe("block coefficients of a level (AC-14)", () => {
  it("changes the coefficient of a pair without touching the other pairs", () => {
    const edited = setBlockWeight(content(), JUNIOR, CORE, 80);

    expect(
      edited.blockWeights.find(
        (w) => w.levelId === JUNIOR && w.blockId === CORE,
      ),
    ).toEqual({ levelId: JUNIOR, blockId: CORE, percent: 80 });
    expect(edited.blockWeights).toHaveLength(3);
  });

  it("removes the pair once the coefficient is cleared", () => {
    const cleared = setBlockWeight(content(), JUNIOR, CORE, null);

    expect(
      cleared.blockWeights.some(
        (w) => w.levelId === JUNIOR && w.blockId === CORE,
      ),
    ).toBe(false);
    expect(cleared.blockWeights).toHaveLength(2);
  });
});

describe("requirements of a level and a competency (AC-14)", () => {
  it("replaces the requirement of a cell instead of adding a second one", () => {
    const edited = withRequirement(content(), requirement(SENIOR, DESIGN, 4));

    const cell = edited.requirements.filter(
      (item) => item.levelId === SENIOR && item.competencyId === DESIGN,
    );
    expect(cell).toHaveLength(1);
    expect(cell[0]?.requiredScore).toBe(4);
    expect(edited.requirements).toHaveLength(3);
  });
});

describe("the places an approver changed (AC-19)", () => {
  const change = (fields: Partial<MatrixChange>): MatrixChange => ({
    kind: "Level",
    type: "Changed",
    levelId: null,
    blockId: null,
    competencyId: null,
    ...fields,
  });

  it("marks a changed requirement by its cell and by its competency row", () => {
    const places = changedPlaces([
      change({ kind: "Requirement", levelId: JUNIOR, competencyId: DESIGN }),
    ]);

    expect(places.requirementCells).toEqual(new Set([cellKey(JUNIOR, DESIGN)]));
    expect(places.competencies).toEqual(new Set([DESIGN]));
    expect(places.weightCells.size).toBe(0);
  });

  it("marks levels, blocks, competencies and coefficient cells each in its own set", () => {
    const places = changedPlaces([
      change({ kind: "Level", levelId: SENIOR }),
      change({ kind: "Block", blockId: EXTRA }),
      change({ kind: "Competency", competencyId: TESTING }),
      change({ kind: "BlockWeight", levelId: SENIOR, blockId: CORE }),
    ]);

    expect(places.levels).toEqual(new Set([SENIOR]));
    expect(places.blocks).toEqual(new Set([EXTRA]));
    expect(places.competencies).toEqual(new Set([TESTING]));
    expect(places.weightCells).toEqual(new Set([cellKey(SENIOR, CORE)]));
    expect(places.requirementCells.size).toBe(0);
  });

  it("ignores a change that does not name the place it concerns", () => {
    const places = changedPlaces([
      change({ kind: "Requirement", levelId: JUNIOR }),
      change({ kind: "BlockWeight", blockId: CORE }),
    ]);

    expect(places.requirementCells.size).toBe(0);
    expect(places.weightCells.size).toBe(0);
  });
});

describe("an even split of 100 % (AC-93)", () => {
  it("gives the remainder one percent each to the first parts, so the parts always make 100", () => {
    expect(evenPercents(3)).toEqual([34, 33, 33]);
    expect(evenPercents(4)).toEqual([25, 25, 25, 25]);
    expect(evenPercents(1)).toEqual([100]);
    expect(evenPercents(7).reduce((sum, part) => sum + part, 0)).toBe(100);
    expect(evenPercents(0)).toEqual([]);
  });

  it("replaces the coefficients of one level and leaves the other level alone", () => {
    const result = distributeBlockWeights(content(), JUNIOR, [CORE, EXTRA]);

    expect(
      result.blockWeights.filter((weight) => weight.levelId === JUNIOR),
    ).toEqual([
      { levelId: JUNIOR, blockId: CORE, percent: 50 },
      { levelId: JUNIOR, blockId: EXTRA, percent: 50 },
    ]);
    expect(
      result.blockWeights.filter((weight) => weight.levelId === SENIOR),
    ).toEqual(content().blockWeights.filter((w) => w.levelId === SENIOR));
  });

  it("splits only for a level that has no coefficient yet, and returns the same document when every level has one", () => {
    const bare = { ...content(), blockWeights: [] };
    const full = content();

    const result = withEvenBlockWeights(bare, [JUNIOR, SENIOR], [CORE, EXTRA]);

    expect(result.blockWeights).toHaveLength(4);
    expect(withEvenBlockWeights(full, [JUNIOR, SENIOR], [CORE])).toBe(full);
  });
});

describe("own weights of the competencies of a group (AC-93)", () => {
  const members = [DESIGN, TESTING];
  const levels = [JUNIOR, SENIOR];

  it("turns on as an even split at every level, and off back to equal weights without touching other groups", () => {
    const other = { levelId: JUNIOR, competencyId: "other", percent: 100 };
    const base = { ...content(), competencyWeights: [other] };

    const on = setOwnWeights(base, levels, members, true);
    const off = setOwnWeights(on, levels, members, false);

    expect(on.competencyWeights).toHaveLength(5);
    expect(ownWeightSum(on, JUNIOR, members)).toBe(100);
    expect(ownWeightSum(on, SENIOR, members)).toBe(100);
    expect(hasOwnWeights(on, members)).toBe(true);
    expect(off.competencyWeights).toEqual([other]);
    expect(hasOwnWeights(off, members)).toBe(false);
  });

  it("sets and clears the weight of one competency at one level", () => {
    const on = setOwnWeights(content(), levels, members, true);

    const changed = setCompetencyWeight(on, JUNIOR, DESIGN, 70);
    const cleared = setCompetencyWeight(changed, JUNIOR, DESIGN, null);

    expect(ownWeightSum(changed, JUNIOR, members)).toBe(120);
    expect(ownWeightSum(cleared, JUNIOR, members)).toBe(50);
    expect(ownWeightSum(cleared, SENIOR, members)).toBe(100);
  });

  it("sums the weights free of float noise", () => {
    let result = setCompetencyWeight(content(), JUNIOR, DESIGN, 0.1);
    result = setCompetencyWeight(result, JUNIOR, TESTING, 0.2);

    expect(ownWeightSum(result, JUNIOR, members)).toBe(0.3);
  });

  it("treats a document written before the weights existed as one without them", () => {
    expect(hasOwnWeights(content(), members)).toBe(false);
    expect(ownWeightSum(content(), JUNIOR, members)).toBe(0);
  });
});

describe("a version of critical competencies only has no overall threshold (AC-93)", () => {
  const withTypes = (
    design: "Critical" | "Additional",
    testing: "Critical" | "Additional",
  ) => ({
    ...content(),
    competencies: [
      { competencyId: DESIGN, blockId: null, type: design },
      { competencyId: TESTING, blockId: null, type: testing },
    ],
  });

  it("is true only when every competency of the group is critical, and false for none", () => {
    expect(
      areAllCritical(withTypes("Critical", "Critical"), [DESIGN, TESTING]),
    ).toBe(true);
    expect(
      areAllCritical(withTypes("Critical", "Additional"), [DESIGN, TESTING]),
    ).toBe(false);
    expect(areAllCritical(withTypes("Critical", "Critical"), [])).toBe(false);
  });

  it("clears the threshold of every level, and returns the same document when none is set", () => {
    const cleared = withoutOverallMins(content());
    const bare = withoutOverallMins(cleared);

    expect(cleared.levels.map((level) => level.minOverallPercent)).toEqual([
      null,
      null,
    ]);
    expect(withoutOverallMins(bare)).toBe(bare);
  });
});
