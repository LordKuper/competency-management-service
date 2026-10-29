import { Typography } from "antd";
import { COMPETENCY_TYPES } from "./competencyType";
import type { MatrixChange, MatrixContent, MatrixDiff } from "./matrixApi";
import { competencyWeightsOf, typeOfCompetency } from "./matrixContent";
import { OUTSIDE_LABEL } from "./matrixGroups";
import { CHANGED_NOTE, EDUCATION_LABELS } from "./matrixLabels";

const NO_VALUE = "—";

/** How each kind of change is called. */
export const CHANGE_TYPES: Record<MatrixChange["type"], string> = {
  Added: "Добавлено",
  Removed: "Удалено",
  Changed: "Изменено",
};

interface MatrixChangesProps {
  diff: MatrixDiff;
  levelNames: ReadonlyMap<string, string>;
  competencyNames: ReadonlyMap<string, string>;
  blockNames: ReadonlyMap<string, string>;
  pointsOf: (
    competencyId: string,
  ) => readonly { score: number; label: string }[];
}

function quoted(name: string | undefined) {
  return `«${name ?? NO_VALUE}»`;
}

function percent(value: number | null) {
  return value == null ? NO_VALUE : `${value} %`;
}

/** One change in words: where it is, and the value there before and after it. */
export interface ChangeDescription {
  place: string;
  before: string | undefined;
  after: string | undefined;
}

/** Words for the changes of a diff; a removal is described by its value before, an addition by its value after. */
export function describeChanges(
  diff: MatrixDiff,
  levelNames: ReadonlyMap<string, string>,
  competencyNames: ReadonlyMap<string, string>,
  blockNames: ReadonlyMap<string, string>,
  pointsOf: (
    competencyId: string,
  ) => readonly { score: number; label: string }[],
): (change: MatrixChange) => ChangeDescription {
  const blockName = (blockId: string | null) =>
    blockId === null ? OUTSIDE_LABEL : blockNames.get(blockId);
  const levelName = (levelId: string | null) =>
    quoted(levelId ? levelNames.get(levelId) : undefined);
  const competencyName = (competencyId: string | null) =>
    quoted(competencyId ? competencyNames.get(competencyId) : undefined);

  const blockOf = (content: MatrixContent, competencyId: string | null) =>
    content.competencies.find((item) => item.competencyId === competencyId)
      ?.blockId;

  function isOnlyTypeChange(change: MatrixChange) {
    const id = change.competencyId;
    return (
      change.type === "Changed" &&
      id !== null &&
      blockOf(diff.before, id) === blockOf(diff.after, id) &&
      typeOfCompetency(diff.before, id) !== typeOfCompetency(diff.after, id)
    );
  }

  function place(change: MatrixChange) {
    switch (change.kind) {
      case "Level":
        return `уровень ${levelName(change.levelId)}`;
      case "Block":
        return `блок ${quoted(blockName(change.blockId))}`;
      case "Competency":
        return isOnlyTypeChange(change)
          ? `Тип компетенции ${competencyName(change.competencyId)}`
          : `компетенция ${competencyName(change.competencyId)}`;
      case "BlockWeight":
        return `коэффициент блока ${quoted(blockName(change.blockId))}, уровень ${levelName(change.levelId)}`;
      case "CompetencyWeight":
        return `вес компетенции ${competencyName(change.competencyId)}, уровень ${levelName(change.levelId)}`;
      case "Requirement":
        return `требуемый балл ${competencyName(change.competencyId)}, уровень ${levelName(change.levelId)}`;
    }
  }

  function value(content: MatrixContent, change: MatrixChange) {
    const { levelId, blockId, competencyId } = change;
    switch (change.kind) {
      case "Level": {
        const level = content.levels.find((item) => item.levelId === levelId);
        return (
          level &&
          `Минимальный общий порог: ${percent(level.minOverallPercent)}${level.requiredEducation ? `; Требуемое образование: ${EDUCATION_LABELS[level.requiredEducation]}` : ""}`
        );
      }
      case "Block": {
        const isIncluded = content.sharedBlocks?.some(
          (item) => item.blockId === blockId,
        );
        return isIncluded ? `Блок ${quoted(blockName(blockId))}` : undefined;
      }
      case "Competency": {
        const competency = content.competencies.find(
          (item) => item.competencyId === competencyId,
        );
        if (isOnlyTypeChange(change)) {
          return COMPETENCY_TYPES[
            typeOfCompetency(content, competencyId ?? "")
          ];
        }
        return (
          competency &&
          `Блок ${quoted(blockName(competency.blockId))}; Тип: ${COMPETENCY_TYPES[typeOfCompetency(content, competency.competencyId)]}`
        );
      }
      case "BlockWeight": {
        const weight = content.blockWeights.find(
          (item) => item.levelId === levelId && item.blockId === blockId,
        );
        return weight && `Коэффициент, %: ${weight.percent}`;
      }
      case "CompetencyWeight": {
        const weight = competencyWeightsOf(content).find(
          (item) =>
            item.levelId === levelId && item.competencyId === competencyId,
        );
        return weight && `Вес, %: ${weight.percent}`;
      }
      case "Requirement": {
        const requirement = content.requirements.find(
          (item) =>
            item.levelId === levelId && item.competencyId === competencyId,
        );
        if (!requirement) return undefined;
        const score = requirement.requiredScore;
        const label = pointsOf(competencyId ?? "").find(
          (point) => point.score === score,
        )?.label;
        return `Требуемый балл: ${score == null ? NO_VALUE : label ? `${score} (${label})` : score}`;
      }
    }
  }

  return (change) => ({
    place: place(change),
    before: value(diff.before, change),
    after: value(diff.after, change),
  });
}

/**
 * The approver's changes in words, one line per added, removed or changed item, with the value before and after, so a
 * removal is seen as well as an addition or a change.
 */
export function MatrixChanges({
  diff,
  levelNames,
  competencyNames,
  blockNames,
  pointsOf,
}: MatrixChangesProps) {
  if (diff.changes.length === 0) return null;

  const describe = describeChanges(
    diff,
    levelNames,
    competencyNames,
    blockNames,
    pointsOf,
  );
  return (
    <section>
      <Typography.Title level={2}>{CHANGED_NOTE}</Typography.Title>
      <ul>
        {diff.changes.map((change) => {
          const { place, before, after } = describe(change);
          return (
            <li
              key={`${change.kind}:${change.levelId}:${change.blockId}:${change.competencyId}`}
            >
              {`${CHANGE_TYPES[change.type]}: ${place}`}
              {before && (
                <Typography.Paragraph type="secondary">{`Было: ${before}`}</Typography.Paragraph>
              )}
              {after && (
                <Typography.Paragraph type="secondary">{`Стало: ${after}`}</Typography.Paragraph>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
