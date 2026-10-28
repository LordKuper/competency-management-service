import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Skeleton } from "antd";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useEditBase } from "../../app/useEditBase";
import { FormalView } from "./FormalView";
import {
  applicationQuery,
  type FormalInput,
  type PromotionApplication,
  promotionQueryKey,
  saveFormal,
  settingsQuery,
  type Versioned,
} from "./promotionApi";

const FORMAL_CHECK_STAGE = 2;

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
          onSaved={onClose}
        />
      )}
    </AppModal>
  );
}

interface FormalEditorProps {
  latest: Versioned<PromotionApplication>;
  minIntervalMonths?: number;
  onSaved: () => void;
}

/** The check with the application it was opened with; it takes the reread one only after a save is refused for a stale version. */
function FormalEditor({
  latest,
  minIntervalMonths,
  onSaved,
}: FormalEditorProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { base = latest, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: (input: FormalInput) =>
      saveFormal(base.id, base.version, input),
    onSuccess: () => {
      message.success("Дата внесена");
      onSaved();
    },
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  const canEnter =
    base.roles.includes("ResponsibleHr") &&
    base.stage === FORMAL_CHECK_STAGE &&
    base.status === "Submitted";
  return (
    <>
      {save.isError && (
        <ErrorAlert title="Дата не внесена" error={save.error} />
      )}
      <FormalView
        key={base.version}
        application={base}
        canEnter={canEnter}
        minIntervalMonths={minIntervalMonths}
        onSubmit={save.mutateAsync}
      />
    </>
  );
}
