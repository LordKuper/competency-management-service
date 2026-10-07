import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Typography } from "antd";
import { type ReactNode, useCallback } from "react";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { ifMatchOf, usersQueryKey } from "../users/usersApi";
import type { EmployeeAction } from "./EmployeeCard";
import { EmployeeImpactList } from "./EmployeeImpactList";
import {
  type Employee,
  fetchEmployeeImpact,
  invalidateOrgStructure,
} from "./orgStructureApi";

type LifecycleAction = Exclude<EmployeeAction, "edit">;

const WORDING = {
  dismiss: {
    title: "Уволить сотрудника",
    okText: "Уволить",
    done: "Сотрудник уволен",
    failed: "Не удалось уволить сотрудника",
  },
  rehire: {
    title: "Вернуть на работу сотрудника",
    okText: "Вернуть на работу",
    done: "Сотрудник возвращён на работу",
    failed: "Не удалось вернуть сотрудника на работу",
  },
  delete: {
    title: "Удалить сотрудника",
    okText: "Удалить",
    done: "Сотрудник удалён",
    failed: "Не удалось удалить сотрудника",
  },
} as const;

const REHIRE_NOTE =
  "Привязанная учётная запись останется заблокированной: разблокируйте её в разделе «Пользователи». Подразделения, которые сотрудник возглавлял, остаются без руководителя.";

/**
 * Returns the action that dismisses, rehires or deletes an employee after a confirmation. Dismissal and deletion first read
 * what they would change elsewhere and list every change in the confirmation, since the server applies exactly those;
 * when the server would refuse because the account is the last active administrator, the confirmation says so and cannot be confirmed.
 * A refusal is explained in a dialog that stays until dismissed. The queries are reread whatever the outcome,
 * so a stale version shows the current state; the accounts are reread too, because the cascade blocks and detaches them.
 */
export function useEmployeeLifecycle() {
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const { mutateAsync: perform } = useMutation({
    mutationFn: async ({
      action,
      id,
      version,
    }: {
      action: LifecycleAction;
      id: string;
      version: number;
    }) => {
      const params = {
        path: { id },
        header: { "If-Match": ifMatchOf(version) },
      };
      switch (action) {
        case "dismiss":
          return unwrap(
            await api.POST("/api/v1/employees/{id}/dismiss", { params }),
          );
        case "rehire":
          return unwrap(
            await api.POST("/api/v1/employees/{id}/rehire", { params }),
          );
        case "delete":
          return unwrap(await api.DELETE("/api/v1/employees/{id}", { params }));
      }
    },
    onSettled: () =>
      Promise.all([
        invalidateOrgStructure(queryClient),
        queryClient.invalidateQueries({ queryKey: usersQueryKey }),
      ]),
  });

  return useCallback(
    async (action: LifecycleAction, employee: Employee) => {
      const { id, version } = employee;
      if (version == null) return;
      const wording = WORDING[action];
      const showFailure = (error: unknown) =>
        modal.error({
          title: wording.failed,
          content: describeApiError(error),
          okText: "Понятно",
        });
      const confirm = (content: ReactNode, isRefused = false) =>
        modal.confirm({
          title: `${wording.title} «${employee.fullName}»?`,
          content,
          okText: wording.okText,
          okButtonProps: { danger: action === "delete", disabled: isRefused },
          cancelText: "Отмена",
          onOk: async () => {
            try {
              await perform({ action, id, version });
              message.success(wording.done);
            } catch (error) {
              showFailure(error);
            }
          },
        });

      if (action === "rehire") {
        confirm(<Typography.Paragraph>{REHIRE_NOTE}</Typography.Paragraph>);
        return;
      }
      const hideLoading = message.loading("Проверяем связанные изменения…", 0);
      try {
        const impact = await fetchEmployeeImpact(id);
        confirm(
          <EmployeeImpactList impact={impact} action={action} />,
          impact.account?.isLastActiveAdministrator,
        );
      } catch (error) {
        showFailure(error);
      } finally {
        hideLoading();
      }
    },
    [modal, message, perform],
  );
}
