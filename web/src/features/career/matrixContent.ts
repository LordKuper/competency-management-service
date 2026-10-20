import type { CompetencyType } from "./catalogApi";
import { DEFAULT_COMPETENCY_TYPE } from "./competencyType";
import type {
  MatrixChange,
  MatrixContent,
  MatrixRequirement,
} from "./matrixApi";

/** Key of a cell: the level and the competency or block it lies under. */
export function cellKey(levelId: string, rowId: string) {
  return `${levelId}:${rowId}`;
}

/** The id a competency outside every block carries in `blockId`; the group is implicit. */
export const OUTSIDE_BLOCK = null;

export function addLevel(content: MatrixContent, levelId: string) {
  return {
    ...content,
    levels: [...content.levels, { levelId, minOverallPercent: null }],
  };
}

export function removeLevel(content: MatrixContent, levelId: string) {
  return {
    ...content,
    levels: content.levels.filter((level) => level.levelId !== levelId),
    blockWeights: content.blockWeights.filter(
      (weight) => weight.levelId !== levelId,
    ),
    requirements: content.requirements.filter(
      (requirement) => requirement.levelId !== levelId,
    ),
  };
}

export function setOverallMin(
  content: MatrixContent,
  levelId: string,
  minOverallPercent: number | null,
) {
  return {
    ...content,
    levels: content.levels.map((level) =>
      level.levelId === levelId ? { ...level, minOverallPercent } : level,
    ),
  };
}

/** Sets the coefficient of a group at a level; a zero coefficient leaves no entry. */
export function setBlockWeight(
  content: MatrixContent,
  levelId: string,
  blockId: string | null,
  percent: number | null,
) {
  const rest = content.blockWeights.filter(
    (weight) => weight.levelId !== levelId || weight.blockId !== blockId,
  );
  return {
    ...content,
    blockWeights: percent ? [...rest, { levelId, blockId, percent }] : rest,
  };
}

export function addCompetency(
  content: MatrixContent,
  competencyId: string,
  blockId: string | null,
) {
  return {
    ...content,
    competencies: [
      ...content.competencies,
      { competencyId, blockId, type: DEFAULT_COMPETENCY_TYPE },
    ],
  };
}

/** The type the competency has in the version: set on its row or in its shared block, else the default. */
export function typeOfCompetency(
  content: MatrixContent,
  competencyId: string,
): CompetencyType {
  const own = content.competencies.find(
    (item) => item.competencyId === competencyId,
  );
  if (own) return own.type ?? DEFAULT_COMPETENCY_TYPE;
  for (const block of sharedBlocksOf(content)) {
    const set = block.competencyTypes?.find(
      (item) => item.competencyId === competencyId,
    );
    if (set) return set.type;
  }
  return DEFAULT_COMPETENCY_TYPE;
}

/**
 * Sets the type of a competency of the version: on its row, or, for a member of a shared block, in that block's list;
 * `compositions` says which block a member belongs to.
 */
export function setCompetencyType(
  content: MatrixContent,
  competencyId: string,
  type: CompetencyType,
  compositions: ReadonlyMap<string, readonly string[]>,
): MatrixContent {
  if (content.competencies.some((item) => item.competencyId === competencyId)) {
    return {
      ...content,
      competencies: content.competencies.map((item) =>
        item.competencyId === competencyId ? { ...item, type } : item,
      ),
    };
  }
  return {
    ...content,
    sharedBlocks: sharedBlocksOf(content).map((block) =>
      compositions.get(block.blockId)?.includes(competencyId)
        ? {
            ...block,
            competencyTypes: [
              ...(block.competencyTypes ?? []).filter(
                (item) => item.competencyId !== competencyId,
              ),
              { competencyId, type },
            ],
          }
        : block,
    ),
  };
}

/** Drops the coefficients of the «Вне блоков» group once no competency stands in it. */
function withoutEmptyOutsideGroup(content: MatrixContent): MatrixContent {
  const isOutsideUsed = content.competencies.some(
    (competency) => competency.blockId === OUTSIDE_BLOCK,
  );
  return isOutsideUsed
    ? content
    : {
        ...content,
        blockWeights: content.blockWeights.filter(
          (weight) => weight.blockId !== OUTSIDE_BLOCK,
        ),
      };
}

export function removeCompetency(content: MatrixContent, competencyId: string) {
  return withoutEmptyOutsideGroup({
    ...content,
    competencies: content.competencies.filter(
      (competency) => competency.competencyId !== competencyId,
    ),
    requirements: content.requirements.filter(
      (requirement) => requirement.competencyId !== competencyId,
    ),
  });
}

/** The shared blocks of a version; a document written before they existed has none. */
export function sharedBlocksOf(content: MatrixContent) {
  return content.sharedBlocks ?? [];
}

/** Includes a shared block; each of its members starts with the default type. */
export function addSharedBlock(
  content: MatrixContent,
  blockId: string,
  memberIds: readonly string[] = [],
) {
  return {
    ...content,
    sharedBlocks: [
      ...sharedBlocksOf(content),
      {
        blockId,
        competencyTypes: memberIds.map((competencyId) => ({
          competencyId,
          type: DEFAULT_COMPETENCY_TYPE,
        })),
      },
    ],
  };
}

/**
 * Takes a shared block out of a version with its coefficients and the requirements of the competencies only it
 * brought; `compositions` holds the composition of every shared block of the version, this one included.
 */
export function removeSharedBlock(
  content: MatrixContent,
  blockId: string,
  compositions: ReadonlyMap<string, readonly string[]>,
) {
  const remaining = sharedBlocksOf(content).filter(
    (block) => block.blockId !== blockId,
  );
  const kept = new Set<string>([
    ...content.competencies.map((competency) => competency.competencyId),
    ...remaining.flatMap((block) => compositions.get(block.blockId) ?? []),
  ]);
  const dropped = new Set(
    (compositions.get(blockId) ?? []).filter((id) => !kept.has(id)),
  );
  return {
    ...content,
    sharedBlocks: remaining,
    blockWeights: content.blockWeights.filter(
      (weight) => weight.blockId !== blockId,
    ),
    requirements: content.requirements.filter(
      (requirement) => !dropped.has(requirement.competencyId),
    ),
  };
}

export function requirementOf(
  content: MatrixContent,
  levelId: string,
  competencyId: string,
) {
  return content.requirements.find(
    (requirement) =>
      requirement.levelId === levelId &&
      requirement.competencyId === competencyId,
  );
}

export function withoutRequirement(
  content: MatrixContent,
  levelId: string,
  competencyId: string,
) {
  return {
    ...content,
    requirements: content.requirements.filter(
      (requirement) =>
        requirement.levelId !== levelId ||
        requirement.competencyId !== competencyId,
    ),
  };
}

export function withRequirement(
  content: MatrixContent,
  requirement: MatrixRequirement,
) {
  const rest = withoutRequirement(
    content,
    requirement.levelId,
    requirement.competencyId,
  );
  return { ...rest, requirements: [...rest.requirements, requirement] };
}

/** What an approver changed, by the place it shows in the editor. */
export interface ChangedPlaces {
  levels: ReadonlySet<string>;
  blocks: ReadonlySet<string>;
  competencies: ReadonlySet<string>;
  requirementCells: ReadonlySet<string>;
  weightCells: ReadonlySet<string>;
}

export const NO_CHANGES: ChangedPlaces = {
  levels: new Set(),
  blocks: new Set(),
  competencies: new Set(),
  requirementCells: new Set(),
  weightCells: new Set(),
};

export function changedPlaces(changes: readonly MatrixChange[]): ChangedPlaces {
  const places = {
    levels: new Set<string>(),
    blocks: new Set<string>(),
    competencies: new Set<string>(),
    requirementCells: new Set<string>(),
    weightCells: new Set<string>(),
  };
  for (const change of changes) {
    const { levelId, blockId, competencyId } = change;
    if (change.kind === "Level" && levelId) places.levels.add(levelId);
    if (change.kind === "Block" && blockId) places.blocks.add(blockId);
    if (change.kind === "Competency" && competencyId) {
      places.competencies.add(competencyId);
    }
    if (change.kind === "Requirement" && levelId && competencyId) {
      places.requirementCells.add(cellKey(levelId, competencyId));
      places.competencies.add(competencyId);
    }
    if (change.kind === "BlockWeight" && levelId && blockId) {
      places.weightCells.add(cellKey(levelId, blockId));
    }
  }
  return places;
}

/** Number of shades the rating chip has: `rating-chip-0` … `rating-chip-4`. */
const MAX_CHIP_LEVEL = 4;

/**
 * The shade of the chip of a score: by the share of the largest score of its scale, rounded to the nearest of the five
 * shades, so a scale of 0–5 needs no shade of its own. The digit on the chip is always the score.
 */
export function chipLevel(
  score: number,
  points: ReadonlyArray<{ score: number }>,
) {
  const largest = Math.max(0, ...points.map((point) => point.score));
  if (largest === 0) return 0;
  return Math.min(
    Math.max(Math.round((score / largest) * MAX_CHIP_LEVEL), 0),
    MAX_CHIP_LEVEL,
  );
}
