import type { CareerLevel } from "../career/catalogApi";
import type { MatrixContent } from "../career/matrixApi";
import {
  addCompetency,
  addLevel,
  addSharedBlock,
  removeCompetency,
  removeSharedBlock,
  sharedBlocksOf,
} from "../career/matrixContent";

/**
 * The content of a version once its own and outside competencies are chosen: the chosen ones are in, the others are out
 * with their requirements. A new one stands outside every block until the author puts it in one. A version still empty
 * also gets the levels of the track.
 */
export function withChosenCompetencies(
  content: MatrixContent,
  chosenIds: ReadonlySet<string>,
  trackLevels: readonly CareerLevel[],
): MatrixContent {
  let next = content;
  for (const { competencyId } of content.competencies) {
    if (!chosenIds.has(competencyId)) {
      next = removeCompetency(next, competencyId);
    }
  }
  if (next.levels.length === 0) {
    for (const level of trackLevels) {
      if (level.status !== "Inactive") next = addLevel(next, level.id);
    }
  }
  for (const competencyId of chosenIds) {
    if (!next.competencies.some((item) => item.competencyId === competencyId)) {
      next = addCompetency(next, competencyId, null);
    }
  }
  return next;
}

/**
 * The content with exactly the given shared blocks included: new ones by reference, removed ones with their coefficients
 * and the requirements only they brought. `compositions` holds the composition of every block named here or in the content.
 */
export function withSharedBlocks(
  content: MatrixContent,
  blockIds: readonly string[],
  compositions: ReadonlyMap<string, readonly string[]>,
): MatrixContent {
  let next = content;
  for (const { blockId } of sharedBlocksOf(content)) {
    if (!blockIds.includes(blockId)) {
      next = removeSharedBlock(next, blockId, compositions);
    }
  }
  for (const blockId of blockIds) {
    if (!sharedBlocksOf(next).some((item) => item.blockId === blockId)) {
      next = addSharedBlock(next, blockId);
    }
  }
  return next;
}

/** The ids of the competencies a version holds of its own, in blocks of its own or outside every block. */
export function competencyIdsOf(content: MatrixContent): Set<string> {
  return new Set(content.competencies.map((item) => item.competencyId));
}

/** The ids of the shared blocks a version includes, in order. */
export function sharedBlockIdsOf(content: MatrixContent): string[] {
  return sharedBlocksOf(content).map((item) => item.blockId);
}
