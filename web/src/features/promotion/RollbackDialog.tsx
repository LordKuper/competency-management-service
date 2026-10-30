import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Skeleton } from "antd";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useEditBase } from "../../app/useEditBase";
import {
  promotionQueryKey,
  type RollbackInput,
  rollbackPreview,
  rollbackWave,
  type Versioned,
  type Wave,
  waveQuery,
} from "./promotionApi";
import { STAGE_TITLES } from "./promotionTexts";
import { RollbackView } from "./RollbackView";

interface RollbackDialogProps {
  waveId: string;
  /** Default length of every stage in working days. */
  durations: readonly number[];
  /** Called with the wave as the return left it. */
  onRolledBack: (wave: Versioned<Wave>) => void;
  onClose: () => void;
}

/** Dialog of sending a wave back to a stage that has passed. Mount it only while it is shown, so every opening starts from the current wave. */
export function RollbackDialog({
  waveId,
  durations,
  onRolledBack,
  onClose,
}: RollbackDialogProps) {
  const { data, error, isPending, refetch } = useQuery(waveQuery(waveId));
  return (
    <AppModal
      open
      title="Откатить карьерную сессию"
      onCancel={onClose}
      footer={null}
    >
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить карьерную сессию"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {data && (
        <RollbackEditor
          latest={data}
          durations={durations}
          onRolledBack={onRolledBack}
        />
      )}
    </AppModal>
  );
}

interface RollbackEditorProps {
  latest: Versioned<Wave>;
  durations: readonly number[];
  onRolledBack: (wave: Versioned<Wave>) => void;
}

/** The return with the wave it was opened with; it takes the reread one only after a save is refused for a stale version. */
function RollbackEditor({
  latest,
  durations,
  onRolledBack,
}: RollbackEditorProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { base = latest, noteSaveFailure } = useEditBase(latest);
  const preview = useMutation({
    mutationFn: (input: RollbackInput) => rollbackPreview(base.id, input.stage),
  });
  const rollback = useMutation({
    mutationFn: (input: RollbackInput) =>
      rollbackWave(base.id, base.version, input),
    onSuccess: (saved, input) => {
      message.success(
        `Карьерная сессия откачена на этап «${STAGE_TITLES[input.stage - 1]}»`,
      );
      onRolledBack(saved);
    },
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  return (
    <>
      {preview.isError && (
        <ErrorAlert
          title="Не удалось узнать последствия отката"
          error={preview.error}
        />
      )}
      {rollback.isError && (
        <ErrorAlert
          title="Карьерная сессия не откачена"
          error={rollback.error}
        />
      )}
      <RollbackView
        key={base.version}
        wave={base}
        durations={durations}
        onPreview={preview.mutateAsync}
        onSubmit={rollback.mutateAsync}
      />
    </>
  );
}
