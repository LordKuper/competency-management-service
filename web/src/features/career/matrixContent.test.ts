import { afterEach, describe, expect, it, vi } from "vitest";
import type {
  MatrixChange,
  MatrixContent,
  MatrixRequirement,
} from "./matrixApi";
import {
  addBlock,
  cellKey,
  changedPlaces,
  hasCompetencies,
  removeBlock,
  removeCompetency,
  removeLevel,
  setBlockWeight,
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
  requiredScore: number | null = 2,
): MatrixRequirement {
  return {
    levelId,
    competencyId,
    requiredScore,
    type: "Profile",
    weight: 1,
    isMandatory: false,
    expectedBehavior: null,
    evidenceExamples: null,
    validFrom: null,
  };
}

function content(): MatrixContent {
  return {
    levels: [
      { levelId: JUNIOR, minOverallPercent: 60 },
      { levelId: SENIOR, minOverallPercent: 70 },
    ],
    blocks: [
      { id: CORE, name: "Основные", isMandatory: true },
      { id: EXTRA, name: "Дополнительные", isMandatory: false },
    ],
    competencies: [
      { competencyId: DESIGN, blockId: CORE },
      { competencyId: TESTING, blockId: EXTRA },
    ],
    blockWeights: [
      { levelId: JUNIOR, blockId: CORE, percent: 100, minPercent: 50 },
      { levelId: SENIOR, blockId: CORE, percent: 70, minPercent: null },
      { levelId: SENIOR, blockId: EXTRA, percent: 30, minPercent: null },
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

  it("drops the coefficients of a removed block, and tells a block with competencies from an empty one", () => {
    const matrix = removeCompetency(content(), TESTING);

    expect(hasCompetencies(matrix, CORE)).toBe(true);
    expect(hasCompetencies(matrix, EXTRA)).toBe(false);
    const edited = removeBlock(matrix, EXTRA);
    expect(edited.blocks.map((block) => block.id)).toEqual([CORE]);
    expect(edited.blockWeights.map((weight) => weight.blockId)).not.toContain(
      EXTRA,
    );
  });
});

describe("block coefficients and thresholds of a level (AC-14)", () => {
  it("changes the coefficient of a pair without touching its threshold or the other pairs", () => {
    const edited = setBlockWeight(content(), JUNIOR, CORE, "percent", 80);

    expect(
      edited.blockWeights.find(
        (w) => w.levelId === JUNIOR && w.blockId === CORE,
      ),
    ).toEqual({ levelId: JUNIOR, blockId: CORE, percent: 80, minPercent: 50 });
    expect(edited.blockWeights).toHaveLength(3);
  });

  it("creates the pair when a threshold is set for a block the level had no coefficient for", () => {
    const edited = setBlockWeight(content(), JUNIOR, EXTRA, "minPercent", 40);

    expect(
      edited.blockWeights.find(
        (w) => w.levelId === JUNIOR && w.blockId === EXTRA,
      ),
    ).toEqual({ levelId: JUNIOR, blockId: EXTRA, percent: 0, minPercent: 40 });
  });

  it("removes the pair once both the coefficient and the threshold are cleared, and not before", () => {
    const withoutPercent = setBlockWeight(
      content(),
      JUNIOR,
      CORE,
      "percent",
      null,
    );
    expect(
      withoutPercent.blockWeights.some(
        (w) => w.levelId === JUNIOR && w.blockId === CORE,
      ),
    ).toBe(true);

    const cleared = setBlockWeight(
      withoutPercent,
      JUNIOR,
      CORE,
      "minPercent",
      null,
    );
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

describe("a new block gets an identifier on a page served over plain HTTP (AC-14)", () => {
  afterEach(() => vi.unstubAllGlobals());

  it.each([
    [0x00, "00000000-0000-4000-8000-000000000000"],
    [0xff, "ffffffff-ffff-4fff-bfff-ffffffffffff"],
  ])(
    "builds a UUID v4 from random values alone, without crypto.randomUUID (bytes of %i)",
    (byte, expected) => {
      vi.stubGlobal("crypto", {
        getRandomValues: (values: Uint8Array) => values.fill(byte),
      });

      const added = addBlock(content()).blocks.at(-1);

      expect(added?.id).toBe(expected);
    },
  );
});
