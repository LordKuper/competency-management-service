import { Typography } from "antd";
import { COMPETENCY_TYPES } from "./CompetencyModal";
import type {
  MatrixChange,
  MatrixContent,
  MatrixDiff,
  MatrixVersion,
} from "./matrixApi";
import { CHANGED_NOTE } from "./matrixLabels";

const NO_VALUE = "—";

const DATE = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeZone: "UTC",
});

const CHANGE_TYPES: Record<MatrixChange["type"], string> = {
  Added: "Добавлено",
  Removed: "Удалено",
  Changed: "Изменено",
};

interface MatrixChangesProps {
  diff: MatrixDiff;
  levelNames: ReadonlyMap<string, string>;
  competencyNames: ReadonlyMap<string, string>;
  scalePoints: MatrixVersion["entries"]["scale"]["points"];
}

function quoted(name: string | undefined) {
  return `«${name ?? NO_VALUE}»`;
}

function percent(value: number | null) {
  return value == null ? NO_VALUE : `${value} %`;
}

/**
 * The approver's changes in words, one line per added, removed or changed item, with the value before and after, so a
 * removal is seen as well as an addition or a change.
 */
export function MatrixChanges({
  diff,
  levelNames,
  competencyNames,
  scalePoints,
}: MatrixChangesProps) {
  if (diff.changes.length === 0) return null;

  const blockName = (content: MatrixContent, blockId: string | null) =>
    content.blocks.find((block) => block.id === blockId)?.name;
  const levelName = (levelId: string | null) =>
    quoted(levelId ? levelNames.get(levelId) : undefined);
  const competencyName = (competencyId: string | null) =>
    quoted(competencyId ? competencyNames.get(competencyId) : undefined);

  function place(change: MatrixChange) {
    switch (change.kind) {
      case "Level":
        return `уровень ${levelName(change.levelId)}`;
      case "Block":
        return `блок ${quoted(blockName(diff.after, change.blockId) ?? blockName(diff.before, change.blockId))}`;
      case "Competency":
        return `компетенция ${competencyName(change.competencyId)}`;
      case "BlockWeight":
        return `коэффициент блока ${quoted(blockName(diff.after, change.blockId) ?? blockName(diff.before, change.blockId))}, уровень ${levelName(change.levelId)}`;
      case "Requirement":
        return `требование ${competencyName(change.competencyId)}, уровень ${levelName(change.levelId)}`;
    }
  }

  function value(content: MatrixContent, change: MatrixChange) {
    const { levelId, blockId, competencyId } = change;
    switch (change.kind) {
      case "Level": {
        const level = content.levels.find((item) => item.levelId === levelId);
        return (
          level &&
          `Минимальный общий порог: ${percent(level.minOverallPercent)}`
        );
      }
      case "Block": {
        const block = content.blocks.find((item) => item.id === blockId);
        return (
          block &&
          `${quoted(block.name)}${block.isMandatory ? ", обязательный блок" : ""}`
        );
      }
      case "Competency": {
        const competency = content.competencies.find(
          (item) => item.competencyId === competencyId,
        );
        return (
          competency && `Блок ${quoted(blockName(content, competency.blockId))}`
        );
      }
      case "BlockWeight": {
        const weight = content.blockWeights.find(
          (item) => item.levelId === levelId && item.blockId === blockId,
        );
        return (
          weight &&
          `Коэффициент, %: ${weight.percent}; Минимальный порог блока: ${percent(weight.minPercent)}`
        );
      }
      case "Requirement": {
        const requirement = content.requirements.find(
          (item) =>
            item.levelId === levelId && item.competencyId === competencyId,
        );
        if (!requirement) return undefined;
        const score = requirement.requiredScore;
        const label = scalePoints.find((point) => point.score === score)?.label;
        return [
          `Требуемый уровень: ${score == null ? NO_VALUE : label ? `${score} (${label})` : score}`,
          `Тип: ${COMPETENCY_TYPES[requirement.type]}`,
          `Вес: ${requirement.weight}`,
          requirement.isMandatory && "Обязательная",
          requirement.expectedBehavior &&
            `Ожидаемое поведение: ${requirement.expectedBehavior}`,
          requirement.evidenceExamples &&
            `Примеры подтверждений: ${requirement.evidenceExamples}`,
          requirement.validFrom &&
            `Действует с: ${DATE.format(new Date(requirement.validFrom))}`,
        ]
          .filter(Boolean)
          .join("; ");
      }
    }
  }

  return (
    <section>
      <Typography.Title level={2}>{CHANGED_NOTE}</Typography.Title>
      <ul>
        {diff.changes.map((change) => {
          const before = value(diff.before, change);
          const after = value(diff.after, change);
          return (
            <li
              key={`${change.kind}:${change.levelId}:${change.blockId}:${change.competencyId}`}
            >
              {`${CHANGE_TYPES[change.type]}: ${place(change)}`}
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
