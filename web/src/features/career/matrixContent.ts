import type {
  MatrixBlock,
  MatrixChange,
  MatrixContent,
  MatrixRequirement,
} from "./matrixApi";

/** Key of a cell: the level and the competency or block it lies under. */
export function cellKey(levelId: string, rowId: string) {
  return `${levelId}:${rowId}`;
}

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

export function addBlock(content: MatrixContent) {
  const block: MatrixBlock = {
    id: crypto.randomUUID(),
    name: "",
    isMandatory: false,
  };
  return { ...content, blocks: [...content.blocks, block] };
}

export function updateBlock(
  content: MatrixContent,
  blockId: string,
  patch: Partial<Omit<MatrixBlock, "id">>,
) {
  return {
    ...content,
    blocks: content.blocks.map((block) =>
      block.id === blockId ? { ...block, ...patch } : block,
    ),
  };
}

export function hasCompetencies(content: MatrixContent, blockId: string) {
  return content.competencies.some(
    (competency) => competency.blockId === blockId,
  );
}

/** Removes a block with its coefficients; the caller offers it only for a block no competency belongs to. */
export function removeBlock(content: MatrixContent, blockId: string) {
  return {
    ...content,
    blocks: content.blocks.filter((block) => block.id !== blockId),
    blockWeights: content.blockWeights.filter(
      (weight) => weight.blockId !== blockId,
    ),
  };
}

/** Sets the coefficient or the block threshold of a level; the pair disappears once both are cleared. */
export function setBlockWeight(
  content: MatrixContent,
  levelId: string,
  blockId: string,
  field: "percent" | "minPercent",
  value: number | null,
) {
  const rest = content.blockWeights.filter(
    (weight) => weight.levelId !== levelId || weight.blockId !== blockId,
  );
  const current = content.blockWeights.find(
    (weight) => weight.levelId === levelId && weight.blockId === blockId,
  ) ?? { levelId, blockId, percent: 0, minPercent: null };
  const next =
    field === "percent"
      ? { ...current, percent: value ?? 0 }
      : { ...current, minPercent: value };
  const isCleared = next.percent === 0 && next.minPercent === null;
  return {
    ...content,
    blockWeights: isCleared ? rest : [...rest, next],
  };
}

export function addCompetency(
  content: MatrixContent,
  competencyId: string,
  blockId: string,
) {
  return {
    ...content,
    competencies: [...content.competencies, { competencyId, blockId }],
  };
}

export function removeCompetency(content: MatrixContent, competencyId: string) {
  return {
    ...content,
    competencies: content.competencies.filter(
      (competency) => competency.competencyId !== competencyId,
    ),
    requirements: content.requirements.filter(
      (requirement) => requirement.competencyId !== competencyId,
    ),
  };
}

export function setCompetencyBlock(
  content: MatrixContent,
  competencyId: string,
  blockId: string,
) {
  return {
    ...content,
    competencies: content.competencies.map((competency) =>
      competency.competencyId === competencyId
        ? { ...competency, blockId }
        : competency,
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
