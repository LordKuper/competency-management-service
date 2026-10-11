import { useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import type { ReactNode } from "react";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import { invalidateCatalogs } from "./catalogApi";
import { invalidateMatrices } from "./matrixApi";

interface Deletion {
  /** The kind of record in the accusative: «направление», «матрицу». */
  kind: string;
  name: string;
  /** Extra line under the question. */
  content?: ReactNode;
  run: () => Promise<unknown>;
  onDeleted?: () => void;
}

/**
 * Asks to confirm a deletion, runs it and rereads the lists. A refusal (409) shows the server's own explanation;
 * the lists are reread after a failure too, since the refusal may mean they are stale.
 */
export function useConfirmDeletion() {
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const reread = () =>
    Promise.all([
      invalidateCatalogs(queryClient),
      invalidateMatrices(queryClient),
    ]);
  return ({ kind, name, content, run, onDeleted }: Deletion) =>
    modal.confirm({
      title: `Удалить ${kind} «${name}»?`,
      content,
      okText: "Удалить",
      okButtonProps: { danger: true },
      cancelText: "Отмена",
      onOk: () =>
        run().then(
          () => {
            onDeleted?.();
            return reread();
          },
          (failure) => {
            message.error(
              failure instanceof ApiError &&
                failure.status === 409 &&
                failure.detail
                ? failure.detail
                : describeApiError(failure),
            );
            void reread();
          },
        ),
    });
}
