import type { CareerLevel } from "../career/catalogApi";
import type { MatrixContent } from "../career/matrixApi";
import {
  addBlock,
  addCompetency,
  addLevel,
  removeCompetency,
  updateBlock,
} from "../career/matrixContent";

const DEFAULT_BLOCK_NAME = "Основной блок";

/**
 * The content of a version once its competencies are chosen: the chosen ones are in, the others are out with their
 * requirements. A version still empty also gets the levels of the track and one block to put the competencies in,
 * since a competency cannot be in a version without a block.
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
  if (next.blocks.length === 0) {
    next = addBlock(next);
    const [block] = next.blocks;
    if (block) {
      next = updateBlock(next, block.id, { name: DEFAULT_BLOCK_NAME });
    }
  }
  const blockId = next.blocks[0]?.id;
  if (blockId === undefined) return next;
  for (const competencyId of chosenIds) {
    if (!next.competencies.some((item) => item.competencyId === competencyId)) {
      next = addCompetency(next, competencyId, blockId);
    }
  }
  return next;
}

/** The ids of the competencies a version holds. */
export function competencyIdsOf(content: MatrixContent): Set<string> {
  return new Set(content.competencies.map((item) => item.competencyId));
}
