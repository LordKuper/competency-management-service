import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { deskQueryKey } from "../../api/queryClient";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";

type DeskInvitation = components["schemas"]["DeskInvitation"];

const SENDING_KEY = "desk-resend-invitation";

/** What the desk needs to send invitations again: the action, and which account is being sent to while a send runs. */
export interface InvitationResend {
  send: (invitation: DeskInvitation) => void;
  /** The account a send is running for; every other send waits until it ends. */
  sendingAccountId: string | null;
}

/**
 * Sends an invitation again from the desk with the account version the desk read. The new link is issued whether or
 * not the mail server accepts the letter, so the item leaves the desk either way; a refused letter stays on screen
 * until closed because a passing toast is easy to miss. A refusal (stale version, blocked account) is reported and the
 * desk reread; nothing is retried by itself. One send at a time: the server answers only after the mail server does.
 */
export function useResendInvitation(): InvitationResend {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const resend = useMutation({
    mutationFn: async (invitation: DeskInvitation) =>
      unwrap(
        await api.POST("/api/v1/users/{id}/resend-invitation", {
          params: {
            path: { id: invitation.accountId },
            header: { "If-Match": ifMatchOf(invitation.version) },
          },
        }),
      ),
    onMutate: () => {
      message.loading({
        key: SENDING_KEY,
        content: "Отправка письма…",
        duration: 0,
      });
    },
    onSuccess: (account) => {
      if (account.mailSent === false) {
        modal.warning({
          title: "Письмо не отправлено",
          content: `Почтовый сервер не принял письмо, поэтому ${account.email} приглашения не получит. Ссылка приглашения уже обновлена, и пункт со стола исчез: отправьте приглашение повторно позже из раздела «Администрирование → Пользователи» (меню «⋮» в строке учётной записи).`,
        });
      } else {
        message.success(`Приглашение отправлено повторно: ${account.email}`);
      }
    },
    onError: (error) => {
      message.error(describeApiError(error));
      void queryClient.invalidateQueries({ queryKey: deskQueryKey });
    },
    onSettled: () => {
      message.destroy(SENDING_KEY);
    },
  });

  return {
    send: (invitation) => {
      if (!resend.isPending) resend.mutate(invitation);
    },
    sendingAccountId: resend.isPending
      ? (resend.variables?.accountId ?? null)
      : null,
  };
}
