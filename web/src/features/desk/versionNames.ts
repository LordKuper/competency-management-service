import { useQuery } from "@tanstack/react-query";
import { levelsQuery } from "../career/catalogApi";
import { useCompetencies } from "../career/MatrixEditor";
import type { MatrixVersion } from "../career/matrixApi";

/**
 * Names of the levels and competencies a version and the changes against it refer to: the version's entries, the track's
 * levels in the catalogue, and the catalogue entry of any competency the entries lack, such as one that was removed.
 */
export function useVersionNames(
  version: MatrixVersion,
  extraCompetencyIds: readonly string[] = [],
) {
  const catalogLevels = useQuery({
    ...levelsQuery(version.entries.track?.id ?? ""),
    enabled: version.entries.track !== null,
  });
  const levelNames = new Map<string, string>([
    ...(catalogLevels.data?.items ?? []).map(
      (level) => [level.id, level.name] as const,
    ),
    ...version.entries.levels.map((level) => [level.id, level.name] as const),
  ]);
  const competencies = useCompetencies(version.entries, [
    ...version.content.competencies.map((item) => item.competencyId),
    ...extraCompetencyIds,
  ]);
  const competencyNames = new Map(
    [...competencies].map(([id, competency]) => [id, competency.name] as const),
  );
  return { levelNames, competencyNames };
}
