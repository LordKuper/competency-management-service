import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import { useCallback } from "react";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import { invalidateOrgStructure, type OrgUnit } from "./orgStructureApi";

const FAILED_TITLE = "Не удалось удалить подразделение";

/**
 * Returns the action that asks for confirmation, then deletes the given unit for good; `onDeleted` receives the deleted unit,
 * so the screen can move its selection to the parent. A refusal because something still refers to the unit lists
 * every obstacle in a dialog that stays until dismissed. The queries are reread whatever the outcome, so a stale
 * version shows the current state; the desk is reread by every successful change.
 * The confirmation handler returns nothing but its own request: a toast is a thenable that settles when it closes.
 */
export function useUnitDeletion(onDeleted: (unit: OrgUnit) => void) {
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const { mutateAsync: remove } = useMutation({
    mutationFn: async (unit: OrgUnit) =>
      unwrap(
        await api.DELETE("/api/v1/org-units/{id}", {
          params: {
            path: { id: unit.id },
            header: { "If-Match": ifMatchOf(unit.version) },
          },
        }),
      ),
    onSettled: () => invalidateOrgStructure(queryClient),
  });

  return useCallback(
    (unit: OrgUnit) =>
      modal.confirm({
        title: `Удалить подразделение «${unit.name}»?`,
        content:
          "Подразделение будет удалено безвозвратно вместе с перечнем должностей и закреплением ответственного HR. Удалить можно только подразделение без дочерних подразделений, сотрудников (в том числе уволенных) и данных карьерной модели. Запись в журнале действий останется.",
        okText: "Удалить",
        ...destructiveConfirm(true),
        cancelText: "Отмена",
        onOk: () =>
          remove(unit).then(
            () => {
              onDeleted(unit);
              message.success("Подразделение удалено");
            },
            (error) => {
              const isRefusal =
                error instanceof ApiError &&
                error.status === 409 &&
                error.obstacles.length > 0;
              modal.error({
                title: FAILED_TITLE,
                content: isRefusal ? (
                  <>
                    <p>{error.detail}</p>
                    <ul>
                      {error.obstacles.map((line) => (
                        <li key={line}>{line}</li>
                      ))}
                    </ul>
                  </>
                ) : (
                  describeApiError(error)
                ),
                okText: "Понятно",
              });
            },
          ),
      }),
    [modal, message, remove, onDeleted],
  );
}
