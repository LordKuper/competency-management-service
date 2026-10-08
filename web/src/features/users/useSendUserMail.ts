import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { type UserResponse, usersQueryKey } from "./usersApi";

/**
 * Returns what tells the administrator whether an account e-mail went out. A failed send leaves the saved change in
 * place and has to be repeated, so it stays on screen until closed rather than in a passing toast.
 */
export function useMailReport() {
  const { message, modal } = App.useApp();
  return (mailSent: boolean | null, sentText: string) => {
    if (mailSent === false) {
      modal.warning({
        title: "Письмо не отправлено",
        content:
          "Почтовый сервер не принял письмо. Учётная запись сохранена; отправьте письмо повторно позже из меню пользователя.",
      });
    } else {
      message.success(sentText);
    }
  };
}

/**
 * Returns the action that e-mails an account a new link: the invitation again to an invited account, or a password
 * reset link to a registered one. The new link voids the previous one; the server refuses an account in the wrong state.
 * The accounts are reread whatever the outcome.
 */
export function useSendUserMail() {
  const { message } = App.useApp();
  const reportMail = useMailReport();
  const queryClient = useQueryClient();
  const send = useMutation({
    mutationFn: async (user: UserResponse) => {
      const params = {
        path: { id: user.id },
        header: { "If-Match": ifMatchOf(user.version) },
      };
      return unwrap(
        user.isInvited
          ? await api.POST("/api/v1/users/{id}/resend-invitation", { params })
          : await api.POST("/api/v1/users/{id}/send-password-reset", {
              params,
            }),
      );
    },
    onSuccess: (sent) => {
      reportMail(
        sent.mailSent,
        sent.isInvited
          ? "Приглашение отправлено повторно"
          : "Ссылка для сброса пароля отправлена",
      );
    },
    onError: (error) => {
      message.error(describeApiError(error));
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });

  return (user: UserResponse) => send.mutate(user);
}
