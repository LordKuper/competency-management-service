import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Space } from "antd";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { invalidateOrgStructure } from "../org-structure/orgStructureApi";
import { UserForm, type UserInput } from "../users/UserForm";
import { type UserResponse, usersQueryKey } from "../users/usersApi";

interface CreateAccountFormProps {
  /** The employee the account is created for and bound to. */
  employeeId: string;
  submitLabel: string;
  cancelLabel?: string;
  /** Receives the created account, with whether the invitation was sent. */
  onCreated: (account: UserResponse) => void;
  onCancel: () => void;
}

/**
 * The e-mail of a new account for a given employee, with the role «Пользователь»: the account is created with an
 * invitation by e-mail. A refusal (a taken e-mail) is shown above the field and under it; the employee stays as it is.
 */
export function CreateAccountForm({
  employeeId,
  submitLabel,
  cancelLabel,
  onCreated,
  onCancel,
}: CreateAccountFormProps) {
  const queryClient = useQueryClient();
  const create = useMutation({
    mutationFn: async (input: UserInput) =>
      unwrap(await api.POST("/api/v1/users", { body: input })),
    onSuccess: onCreated,
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: usersQueryKey }),
        invalidateOrgStructure(queryClient),
      ]),
  });

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      {create.isError && (
        <ErrorAlert title="Учётная запись не создана" error={create.error} />
      )}
      <UserForm
        isNew
        lockedTo={{ role: "User", employeeId }}
        submitLabel={submitLabel}
        cancelLabel={cancelLabel}
        onSubmit={(input) => create.mutateAsync(input)}
        onCancel={onCancel}
      />
    </Space>
  );
}
