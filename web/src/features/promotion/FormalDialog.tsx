import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Skeleton } from "antd";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { isStaleVersion, useEditBase } from "../../app/useEditBase";
import { FormalView } from "./FormalView";
import {
  applicationQuery,
  type FormalInput,
  type PromotionApplication,
  promotionQueryKey,
  saveFormal,
  saveRaterEmail,
  settingsQuery,
  type Versioned,
} from "./promotionApi";
import { STAGE } from "./promotionTexts";

interface FormalDialogProps {
  applicationId: string;
  onClose: () => void;
}

/** Dialog of the formal check of an application. Mount it only while it is shown, so every opening starts from the current record. */
export function FormalDialog({ applicationId, onClose }: FormalDialogProps) {
  const { data, error, isPending, refetch } = useQuery(
    applicationQuery(applicationId),
  );
  const settings = useQuery(settingsQuery);
  return (
    <AppModal open title="Формальная проверка" onCancel={onClose} footer={null}>
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить заявку"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {data && (
        <FormalEditor
          latest={data}
          minIntervalMonths={settings.data?.minIntervalMonths}
        />
      )}
    </AppModal>
  );
}

interface FormalEditorProps {
  latest: Versioned<PromotionApplication>;
  minIntervalMonths?: number;
}

/** The check with the application it was opened with, which stays open after a save to show the result the data would give; it takes the reread record only after a save is refused for a stale version. */
function FormalEditor({ latest, minIntervalMonths }: FormalEditorProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { base = latest, noteSaveFailure, rebase } = useEditBase(latest);
  const settle = {
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  };
  const save = useMutation({
    mutationFn: (input: FormalInput) =>
      saveFormal(base.id, base.version, input),
    ...settle,
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Данные сохранены");
    },
  });
  const confirmEmail = useMutation({
    mutationFn: ({
      employeeId,
      email,
    }: {
      employeeId: string;
      email: string;
    }) => saveRaterEmail(base.id, base.version, employeeId, email),
    ...settle,
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Почта подтверждена");
    },
  });
  const canEnter =
    base.roles.includes("ResponsibleHr") &&
    base.stage <= STAGE.formalCheck &&
    base.status === "Submitted";
  return (
    <>
      {save.isError && (
        <ErrorAlert title="Данные не сохранены" error={save.error} />
      )}
      {confirmEmail.isError && isStaleVersion(confirmEmail.error) && (
        <ErrorAlert title="Почта не подтверждена" error={confirmEmail.error} />
      )}
      <FormalView
        application={base}
        canEnter={canEnter}
        minIntervalMonths={minIntervalMonths}
        onSubmit={save.mutateAsync}
        onConfirmEmail={(employeeId, email) =>
          confirmEmail.mutateAsync({ employeeId, email })
        }
      />
    </>
  );
}
