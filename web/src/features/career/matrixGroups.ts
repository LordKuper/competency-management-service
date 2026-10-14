import type {
  MatrixContent,
  MatrixDiff,
  MatrixSharedBlockEntry,
  MatrixVersion,
} from "./matrixApi";

/** The name of the implicit group of competencies that stand in no block. */
export const OUTSIDE_LABEL = "Вне блоков";

/** The name of the single requirements column of a position matrix. */
export const POSITION_COLUMN_LABEL = "Требуемый балл";

type Scale = NonNullable<MatrixVersion["entries"]["scale"]>;

const NO_SCALE: Scale = { id: "", name: "", points: [], status: "Active" };

/** The shared blocks of a version as its entries describe them: their scale and the composition they have now. */
export function sharedEntriesOf(
  version: Pick<MatrixVersion, "entries">,
): readonly MatrixSharedBlockEntry[] {
  return version.entries.sharedBlocks ?? [];
}

/** The composition of each shared block of a version, by block id. */
export function compositionsOf(
  version: Pick<MatrixVersion, "entries">,
): Map<string, readonly string[]> {
  return new Map(
    sharedEntriesOf(version).map((block) => [block.id, block.competencyIds]),
  );
}

/** The shared block a competency comes with, if it comes with one. */
export function sharedBlockOf(
  version: Pick<MatrixVersion, "entries">,
  competencyId: string,
) {
  return sharedEntriesOf(version).find((block) =>
    block.competencyIds.includes(competencyId),
  );
}

/** The scale a requirement for the competency is a score of: its own, else the scale of a version published with one. */
export function scaleOfCompetency(
  version: Pick<MatrixVersion, "entries">,
  competencyId: string,
): Scale {
  const { entries } = version;
  const scaleId = entries.competencies.find(
    (competency) => competency.id === competencyId,
  )?.scaleId;
  return (
    entries.scales?.find((scale) => scale.id === scaleId) ??
    entries.scale ??
    NO_SCALE
  );
}

/** Every competency a version holds: its own and outside ones in content order, then those its shared blocks bring. */
export function versionCompetencyIds(
  version: Pick<MatrixVersion, "entries" | "content">,
  content: MatrixContent = version.content,
): string[] {
  const ids = content.competencies.map((item) => item.competencyId);
  const brought = (content.sharedBlocks ?? []).flatMap(
    (block) =>
      version.entries.sharedBlocks?.find((entry) => entry.id === block.blockId)
        ?.competencyIds ?? [],
  );
  return [...new Set([...ids, ...brought])];
}

/** What changed in the composition of a shared block against the published version. */
export interface CompositionChange {
  added: readonly string[];
  removed: readonly string[];
}

/**
 * The shared blocks whose composition differs from the one in the published version, by block id; a block the published
 * version does not hold is not compared.
 */
export function compositionChanges(
  diff: MatrixDiff | undefined,
): Map<string, CompositionChange> {
  const changes = new Map<string, CompositionChange>();
  if (!diff) return changes;
  const published = new Set(
    (diff.before.sharedBlocks ?? []).map((block) => block.blockId),
  );
  const membersOf = (content: MatrixContent, blockId: string) =>
    content.competencies
      .filter((item) => item.blockId === blockId)
      .map((item) => item.competencyId);
  for (const { blockId } of diff.after.sharedBlocks ?? []) {
    if (!published.has(blockId)) continue;
    const before = membersOf(diff.before, blockId);
    const after = membersOf(diff.after, blockId);
    const added = after.filter((id) => !before.includes(id));
    const removed = before.filter((id) => !after.includes(id));
    if (added.length > 0 || removed.length > 0) {
      changes.set(blockId, { added, removed });
    }
  }
  return changes;
}

const PLURAL = new Intl.PluralRules("ru-RU");
const COMPETENCY_FORMS: Record<string, string> = {
  one: "компетенция",
  few: "компетенции",
};

/** «1 компетенция» / «4 компетенции» / «5 компетенций». */
export function competenciesText(count: number): string {
  return `${count} ${COMPETENCY_FORMS[PLURAL.select(count)] ?? "компетенций"}`;
}
