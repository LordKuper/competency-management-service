import { Button } from "antd";
import { useIsAdmin } from "../auth/useCurrentUser";
import { useProcessRoles } from "./careerApi";
import { deleteMatrix, type Matrix } from "./matrixApi";
import { useConfirmDeletion } from "./useConfirmDeletion";

interface MatrixDeleteProps {
  matrix: Matrix;
  /** Tag of the version in work as the page shows it; omitted, the one of the list row. */
  inWorkTag?: number;
  onDeleted?: () => void;
}

/**
 * Starts the deletion of a matrix with no published version, for the head of its unit and administrators; undefined
 * for anyone else. The deletion carries the tag of the version in work the user saw, so a change made since then is
 * refused. The server decides again and explains a refusal.
 */
export function useMatrixDeletion({
  matrix,
  inWorkTag,
  onDeleted,
}: MatrixDeleteProps): (() => void) | undefined {
  const isAdmin = useIsAdmin();
  const { data } = useProcessRoles();
  const confirmDeletion = useConfirmDeletion();
  const isHead = !!data?.headedUnits.some(
    (unit) => unit.id === matrix.orgUnitId,
  );
  const { inWorkVersion } = matrix;
  if (!(isAdmin || isHead) || matrix.publishedVersion || !inWorkVersion) {
    return undefined;
  }
  return () =>
    confirmDeletion({
      kind: "матрицу",
      name: matrix.trackName,
      run: () => deleteMatrix(matrix.id, inWorkTag ?? inWorkVersion.version),
      onDeleted,
    });
}

/** «Удалить» of a matrix in a row, where the deletion is allowed. */
export function MatrixDeleteButton(props: MatrixDeleteProps) {
  const start = useMatrixDeletion(props);
  if (!start) return null;
  return (
    <Button danger type="link" size="small" onClick={start}>
      Удалить
    </Button>
  );
}
