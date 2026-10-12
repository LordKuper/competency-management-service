import { Button } from "antd";
import { useIsAdmin } from "../auth/useCurrentUser";
import { useProcessRoles } from "./careerApi";
import { deleteMatrix, type Matrix } from "./matrixApi";
import { useConfirmDeletion } from "./useConfirmDeletion";

interface MatrixDeleteButtonProps {
  matrix: Matrix;
  /** Tag of the version in work as the page shows it; omitted, the one of the list row. */
  inWorkTag?: number;
  onDeleted?: () => void;
}

/**
 * «Удалить» of a matrix with no published version, for the head of its unit and administrators. The deletion carries the
 * tag of the version in work the user saw, so a change made since then is refused. The server decides again and explains a refusal.
 */
export function MatrixDeleteButton({
  matrix,
  inWorkTag,
  onDeleted,
}: MatrixDeleteButtonProps) {
  const isAdmin = useIsAdmin();
  const { data } = useProcessRoles();
  const confirmDeletion = useConfirmDeletion();
  const isHead = !!data?.headedUnits.some(
    (unit) => unit.id === matrix.orgUnitId,
  );
  const { inWorkVersion } = matrix;
  if (!(isAdmin || isHead) || matrix.publishedVersion || !inWorkVersion) {
    return null;
  }
  return (
    <Button
      danger
      type="link"
      size="small"
      onClick={() =>
        confirmDeletion({
          kind: "матрицу",
          name: matrix.trackName,
          run: () =>
            deleteMatrix(matrix.id, inWorkTag ?? inWorkVersion.version),
          onDeleted,
        })
      }
    >
      Удалить
    </Button>
  );
}
