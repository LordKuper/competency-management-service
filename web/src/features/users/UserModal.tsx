import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Modal, Skeleton, Space } from "antd";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { invalidateOrgStructure } from "../org-structure/orgStructureApi";
import { UserForm, type UserInput } from "./UserForm";
import {
  ifMatchOf,
  type UserResponse,
  userQuery,
  usersQueryKey,
} from "./usersApi";

interface UserModalProps {
  /** Account to change; omitted when an account is created. */
  userId?: string;
  onClose: () => void;
}

/**
 * Dialog that changes one account or creates one. Only administrators reach it. An existing account is read when the
 * dialog opens, so the change is based on the current version. Mount it only while it is shown, so every opening starts clean.
 */
export function UserModal({ userId, onClose }: UserModalProps) {
  return (
    <Modal
      open
      title={
        userId === undefined ? "Новый пользователь" : "Изменение пользователя"
      }
      onCancel={onClose}
      footer={null}
    >
      {userId === undefined ? (
        <UserEditor onClose={onClose} />
      ) : (
        <ChangedUser userId={userId} onClose={onClose} />
      )}
    </Modal>
  );
}

function ChangedUser({
  userId,
  onClose,
}: Pick<UserModalProps, "onClose"> & { userId: string }) {
  const { data: user, error, isPending, refetch } = useQuery(userQuery(userId));

  if (isPending) return <Skeleton active />;
  if (!user) {
    return (
      <ErrorAlert
        title="Не удалось загрузить пользователя"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <UserEditor existing={user} onClose={onClose} />;
}

/** A conflict that names a field, such as a taken e-mail, is shown under that field, so an alert would only repeat it. */
function isShownUnderField(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    error.status === 409 &&
    error.errors !== undefined
  );
}

interface UserEditorProps extends Pick<UserModalProps, "onClose"> {
  /** The account being changed, with the version the change is based on; omitted when an account is created. */
  existing?: UserResponse;
}

/**
 * The form with its save: a change is refused when the version is stale, and the accounts are reread whatever the outcome.
 * The employee cards show the account e-mail, so the org structure is reread too.
 */
function UserEditor({ existing, onClose }: UserEditorProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const save = useMutation({
    mutationFn: async ({ password, ...account }: UserInput) =>
      unwrap(
        existing
          ? await api.PUT("/api/v1/users/{id}", {
              params: {
                path: { id: existing.id },
                header: { "If-Match": ifMatchOf(existing.version) },
              },
              body: account,
            })
          : await api.POST("/api/v1/users", {
              body: { ...account, password: password ?? "" },
            }),
      ).data,
    onSuccess: () => {
      message.success(existing ? "Изменения сохранены" : "Пользователь создан");
      onClose();
    },
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: usersQueryKey }),
        invalidateOrgStructure(queryClient),
      ]),
  });

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      {save.isError && !isShownUnderField(save.error) && (
        <ErrorAlert
          title="Не удалось сохранить пользователя"
          error={save.error}
        />
      )}
      <UserForm
        key={existing?.version}
        initialValues={
          existing && {
            email: existing.email,
            role: existing.role,
            employeeId: existing.employeeId ?? undefined,
          }
        }
        employee={
          existing?.employeeId && existing.employeeName
            ? { id: existing.employeeId, name: existing.employeeName }
            : undefined
        }
        isNew={!existing}
        submitLabel={existing ? "Сохранить" : "Создать"}
        onSubmit={(input) => save.mutateAsync(input)}
        onCancel={onClose}
      />
    </Space>
  );
}
