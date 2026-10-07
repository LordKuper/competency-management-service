import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { type UserResponse, usersQueryKey } from "./usersApi";

/**
 * Returns the action that asks for confirmation, then blocks the given account or lifts its block.
 * The accounts are reread whatever the outcome, so a stale version shows the current state.
 * The confirmation handlers return nothing: a toast is a thenable that settles when it closes, and returning it would
 * keep the dialog open, with a busy confirm button, for as long as the toast is shown.
 */
export function useBlockUser() {
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const change = useMutation({
    mutationFn: async (user: UserResponse) => {
      const params = {
        path: { id: user.id },
        header: { "If-Match": ifMatchOf(user.version) },
      };
      return unwrap(
        user.isBlocked
          ? await api.POST("/api/v1/users/{id}/unblock", { params })
          : await api.POST("/api/v1/users/{id}/block", { params }),
      );
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });

  return (user: UserResponse) =>
    modal.confirm({
      title: user.isBlocked
        ? `Разблокировать пользователя «${user.email}»?`
        : `Заблокировать пользователя «${user.email}»?`,
      content: user.isBlocked
        ? "Пользователь снова сможет войти в систему."
        : "Пользователь не сможет войти в систему, его действующие сессии будут завершены. Блокировку можно снять в любой момент.",
      okText: user.isBlocked ? "Разблокировать" : "Заблокировать",
      okButtonProps: { danger: !user.isBlocked },
      cancelText: "Отмена",
      onOk: () =>
        change.mutateAsync(user).then(
          () => {
            message.success(
              user.isBlocked
                ? "Пользователь разблокирован"
                : "Пользователь заблокирован",
            );
          },
          (error) => {
            message.error(describeApiError(error));
          },
        ),
    });
}
