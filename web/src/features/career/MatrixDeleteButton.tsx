import { Button } from "antd";
import { useIsAdmin } from "../auth/useCurrentUser";
import { useProcessRoles } from "./careerApi";
import { deleteMatrix, type Matrix } from "./matrixApi";
import { useConfirmDeletion } from "./useConfirmDeletion";

/**
 * «Удалить» of a matrix with no published version, for the head of its unit and administrators. The server decides
 * again and explains a refusal.
 */
export function MatrixDeleteButton({
  matrix,
  onDeleted,
}: {
  matrix: Matrix;
  onDeleted?: () => void;
}) {
  const isAdmin = useIsAdmin();
  const { data } = useProcessRoles();
  const confirmDeletion = useConfirmDeletion();
  const isHead = !!data?.headedUnits.some(
    (unit) => unit.id === matrix.orgUnitId,
  );
  if (
    !(isAdmin || isHead) ||
    matrix.publishedVersion ||
    !matrix.inWorkVersion
  ) {
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
          run: () => deleteMatrix(matrix),
          onDeleted,
        })
      }
    >
      Удалить
    </Button>
  );
}
