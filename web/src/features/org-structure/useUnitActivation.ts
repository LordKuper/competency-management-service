import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import { useCallback } from "react";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { ifMatchOf } from "../users/usersApi";
import { invalidateOrgStructure, type OrgUnit } from "./orgStructureApi";

/**
 * Returns the action that asks for confirmation, then deactivates the given unit or activates it again.
 * A refusal is explained in a dialog that stays until dismissed, since the reason is long and says what to do first.
 * The queries are reread whatever the outcome, so a stale version shows the current state.
 */
export function useUnitActivation() {
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const { mutateAsync: change } = useMutation({
    mutationFn: async (unit: OrgUnit) => {
      const params = {
        path: { id: unit.id },
        header: { "If-Match": ifMatchOf(unit.version) },
      };
      return unwrap(
        unit.isActive
          ? await api.POST("/api/v1/org-units/{id}/deactivate", { params })
          : await api.POST("/api/v1/org-units/{id}/activate", { params }),
      ).data;
    },
    onSettled: () => invalidateOrgStructure(queryClient),
  });

  return useCallback(
    (unit: OrgUnit) =>
      modal.confirm({
        title: unit.isActive
          ? `Деактивировать подразделение «${unit.name}»?`
          : `Активировать подразделение «${unit.name}»?`,
        content: unit.isActive
          ? "Подразделение исчезнет из дерева у обычных пользователей. Деактивировать можно только подразделение без активных дочерних подразделений и работающих сотрудников. Активация вернёт всё как было."
          : "Подразделение снова появится в дереве у всех пользователей. Его родительское подразделение и руководитель должны быть активными.",
        okText: unit.isActive ? "Деактивировать" : "Активировать",
        okButtonProps: { danger: unit.isActive },
        cancelText: "Отмена",
        onOk: () =>
          change(unit).then(
            () =>
              message.success(
                unit.isActive
                  ? "Подразделение деактивировано"
                  : "Подразделение активировано",
              ),
            (error) => {
              modal.error({
                title: unit.isActive
                  ? "Не удалось деактивировать подразделение"
                  : "Не удалось активировать подразделение",
                content: describeApiError(error),
                okText: "Понятно",
              });
            },
          ),
      }),
    [modal, message, change],
  );
}
