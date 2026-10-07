import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import { useNavigate } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { LOGIN_PATH } from "../../app/featureContract";

/** Ends the session on the server, then drops every cached answer and opens the sign-in screen; a failed call keeps the user where they are. */
export function useSignOut() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { message } = App.useApp();
  return useMutation({
    mutationFn: async () => unwrap(await api.POST("/api/v1/auth/logout")),
    onSuccess: () => {
      queryClient.clear();
      void navigate(LOGIN_PATH, { replace: true });
    },
    onError: (error) => {
      message.error(describeApiError(error));
    },
  });
}
