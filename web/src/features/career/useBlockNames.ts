import { useQueries } from "@tanstack/react-query";
import { blockQuery } from "./catalogApi";
import type { MatrixDiff, MatrixVersion } from "./matrixApi";
import { blockNamesOf } from "./matrixGroups";

/**
 * The names of the shared blocks a diff speaks of: those the version still includes from its entries, the others
 * (removed from the version) from the catalogue; a name is missing while it loads or when the read fails.
 */
export function useBlockNames(
  version: Pick<MatrixVersion, "entries">,
  ...diffs: (MatrixDiff | undefined)[]
): ReadonlyMap<string, string> {
  const names = blockNamesOf(version);
  const missing = [
    ...new Set(
      diffs
        .flatMap((diff) => diff?.changes ?? [])
        .flatMap((change) => (change.blockId ? [change.blockId] : []))
        .filter((id) => !names.has(id)),
    ),
  ];
  const reads = useQueries({
    queries: missing.map((id) => blockQuery(id)),
  });
  for (const read of reads) {
    if (read.data) names.set(read.data.id, read.data.name);
  }
  return names;
}
