import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Space } from "antd";
import { type ComponentProps, useEffect, useState } from "react";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import type { LeaveEdits } from "../../app/leaveGuard";
import { invalidateOrgStructure } from "../org-structure/orgStructureApi";
import { UserForm, type UserInput } from "../users/UserForm";
import { type UserResponse, usersQueryKey } from "../users/usersApi";

interface CreateAccountFormProps {
  /** The employee the account is created for and bound to. */
  employeeId: string;
  submitLabel: string;
  /** Receives the created account, with whether the invitation was sent. */
  onCreated: (account: UserResponse) => void;
  onCancel?: () => void;
  /** Told whether the e-mail is entered and not saved, and whether the account is being created. */
  onEditsChange?: (edits: LeaveEdits) => void;
  scenarioActions?: ComponentProps<typeof UserForm>["scenarioActions"];
}

/**
 * The e-mail of a new account for a given employee, with the role «Пользователь»: the account is created with an
 * invitation by e-mail. A refusal (a taken e-mail) is shown above the field and under it; the employee stays as it is.
 */
export function CreateAccountForm({
  employeeId,
  submitLabel,
  onCreated,
  onCancel,
  onEditsChange,
  scenarioActions,
}: CreateAccountFormProps) {
  const queryClient = useQueryClient();
  const [isDirty, setIsDirty] = useState(false);
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
  const isSaving = create.isPending;
  useEffect(
    () => onEditsChange?.({ isUnsaved: isDirty, isSaving }),
    [isDirty, isSaving, onEditsChange],
  );

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      {create.isError && (
        <ErrorAlert title="Учётная запись не создана" error={create.error} />
      )}
      <UserForm
        isNew
        lockedTo={{ employeeId }}
        submitLabel={submitLabel}
        onSubmit={(input) => create.mutateAsync(input)}
        onCancel={onCancel}
        onDirtyChange={setIsDirty}
        scenarioActions={scenarioActions}
      />
    </Space>
  );
}
