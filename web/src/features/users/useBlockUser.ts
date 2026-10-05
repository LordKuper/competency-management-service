import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { ifMatchOf, type UserResponse, usersQueryKey } from "./usersApi";

/**
 * Returns the action that asks for confirmation, then blocks the given account or lifts its block.
 * The accounts are reread whatever the outcome, so a stale version shows the current state.
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
      ).data;
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });

  return (user: UserResponse) =>
    modal.confirm({
      title: user.isBlocked
        ? `Разблокировать пользователя «${user.userName}»?`
        : `Заблокировать пользователя «${user.userName}»?`,
      content: user.isBlocked
        ? "Пользователь снова сможет войти в систему."
        : "Пользователь не сможет войти в систему, его действующие сессии будут завершены. Блокировку можно снять в любой момент.",
      okText: user.isBlocked ? "Разблокировать" : "Заблокировать",
      okButtonProps: { danger: !user.isBlocked },
      cancelText: "Отмена",
      onOk: () =>
        change.mutateAsync(user).then(
          () =>
            message.success(
              user.isBlocked
                ? "Пользователь разблокирован"
                : "Пользователь заблокирован",
            ),
          (error) => message.error(describeApiError(error)),
        ),
    });
}
