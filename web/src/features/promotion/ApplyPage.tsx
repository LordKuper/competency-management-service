import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, App, Breadcrumb, Flex, Skeleton, Tag, Typography } from "antd";
import { useEffect, useRef, useState } from "react";
import { Link, Navigate, useNavigate, useParams } from "react-router";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useLeaveGuard } from "../../app/leaveGuard";
import { contentMaxWidth } from "../../app/theme";
import { isStaleVersion, useEditBase } from "../../app/useEditBase";
import { useCurrentUser } from "../auth/useCurrentUser";
import { type ApplicationDraft, ApplyView, type TargetRef } from "./ApplyView";
import {
  applicationQuery,
  createApplication,
  deleteApplication,
  myApplicationsQuery,
  type OpenWave,
  openWavesQuery,
  type PromotionApplication,
  promotionQueryKey,
  saveApplication,
  saveArtifacts,
  submitApplication,
  targetFormQuery,
  type Versioned,
  type WaveApplication,
} from "./promotionApi";
import { formatDate, STAGE } from "./promotionTexts";
import { promotionPaths } from "./routes";
import { useRaterOptions } from "./useRaterOptions";

/** Drafting an application in a wave and filing it while the wave takes applications. */
export function ApplyPage() {
  const { waveId } = useParams();
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.home}>Карьерные переходы</Link> },
          { title: "Заявка" },
        ]}
      />
      <Flex align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Заявка на карьерный переход
        </Typography.Title>
        <Tag>Черновик</Tag>
      </Flex>
      {waveId && <ApplyContent waveId={waveId} />}
    </Flex>
  );
}

function ApplyContent({ waveId }: { waveId: string }) {
  const waves = useQuery(openWavesQuery);
  const mine = useQuery(myApplicationsQuery);
  const failure = waves.error ?? mine.error;
  if (failure) {
    return (
      <ErrorAlert
        title="Не удалось загрузить карьерную сессию"
        error={failure}
        onRetry={() => {
          void waves.refetch();
          void mine.refetch();
        }}
      />
    );
  }
  if (!waves.data || !mine.data) return <Skeleton active />;

  const wave = waves.data.find((candidate) => candidate.id === waveId);
  if (!wave || wave.stage !== STAGE.submission) {
    return (
      <Alert
        type="info"
        showIcon
        title="Заявки в этой карьерной сессии сейчас не принимаются"
        description={
          wave && wave.stage < STAGE.submission
            ? `Приём заявок начнётся ${formatDate(wave.startsOn)}.`
            : "Приём заявок в этой карьерной сессии закончился или сессии нет."
        }
      />
    );
  }
  return (
    <ApplyEntry
      wave={wave}
      existing={mine.data.find(
        (application) =>
          application.waveId === waveId && application.status !== "Withdrawn",
      )}
    />
  );
}

interface ApplyEntryProps {
  wave: OpenWave;
  existing?: WaveApplication;
}

/** Chooses once, from the applications the viewer had when the screen opened, between a new draft, the draft they have and the filed application; saving a draft later does not switch it. */
function ApplyEntry({ wave, existing }: ApplyEntryProps) {
  const [opened] = useState(existing);
  if (opened && opened.status !== "Draft") {
    return <Navigate replace to={promotionPaths.application(opened.id)} />;
  }
  return opened ? (
    <LoadedDraft wave={wave} draftId={opened.id} />
  ) : (
    <ApplyForm wave={wave} />
  );
}

function LoadedDraft({ wave, draftId }: { wave: OpenWave; draftId: string }) {
  const { data, error, isPending, refetch } = useQuery(
    applicationQuery(draftId),
  );
  if (isPending) return <Skeleton active />;
  if (error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить черновик"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <ApplyForm wave={wave} latest={data} />;
}

/** The form with the draft it was opened with; it takes the reread one only after a save is refused for a stale version, and shows it then. */
function ApplyForm({
  wave,
  latest,
}: {
  wave: OpenWave;
  latest?: Versioned<PromotionApplication>;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { message, modal } = App.useApp();
  const { leaveAfterSave } = useLeaveGuard();
  const { base, noteSaveFailure, rebase } = useEditBase(latest);
  const [reloads, setReloads] = useState(0);
  const isRefused = useRef(false);
  useEffect(() => {
    if (base && isRefused.current) {
      isRefused.current = false;
      setReloads((count) => count + 1);
    }
  }, [base]);
  const settle = {
    onError: (error: unknown) => {
      isRefused.current = isStaleVersion(error);
      noteSaveFailure(error);
    },
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  };

  async function persist({ input, artifacts }: ApplicationDraft) {
    const saved = base
      ? await saveApplication(base.id, base.version, input)
      : await createApplication(wave.id, input);
    const withArtifacts =
      artifacts.length > 0 || (saved.artifacts?.length ?? 0) > 0
        ? await saveArtifacts(saved.id, saved.version, artifacts)
        : saved;
    rebase(withArtifacts);
    return withArtifacts;
  }

  const saveDraft = useMutation({
    mutationFn: persist,
    ...settle,
    onSuccess: () => message.success("Черновик сохранён"),
  });
  const send = useMutation({
    mutationFn: async (draft: ApplicationDraft) => {
      const saved = await persist(draft);
      return submitApplication(saved.id, saved.version);
    },
    ...settle,
    onSuccess: (sent) => {
      message.success("Заявка отправлена");
      leaveAfterSave(() => void navigate(promotionPaths.application(sent.id)));
    },
  });
  const remove = useMutation({
    mutationFn: (draft: Versioned<PromotionApplication>) =>
      deleteApplication(draft.id, draft.version),
    ...settle,
    onSuccess: () => {
      message.success("Черновик удалён");
      leaveAfterSave(() => void navigate(promotionPaths.home));
    },
  });

  function confirmDeletion(draft: Versioned<PromotionApplication>) {
    modal.confirm({
      title: "Удалить черновик?",
      content: "Всё, что в нём введено, будет удалено.",
      okText: "Удалить черновик",
      ...destructiveConfirm(true),
      cancelText: "Отмена",
      onOk: () => remove.mutateAsync(draft).then(undefined, () => undefined),
    });
  }

  return (
    <Flex vertical gap="middle">
      {saveDraft.isError && (
        <ErrorAlert title="Черновик не сохранён" error={saveDraft.error} />
      )}
      {send.isError && (
        <ErrorAlert title="Заявка не отправлена" error={send.error} />
      )}
      {remove.isError && (
        <ErrorAlert title="Черновик не удалён" error={remove.error} />
      )}
      <ApplyEditor
        key={reloads}
        wave={wave}
        application={base}
        isBusy={remove.isPending}
        onSaveDraft={saveDraft.mutateAsync}
        onSend={send.mutateAsync}
        onDelete={base ? () => confirmDeletion(base) : undefined}
      />
    </Flex>
  );
}

interface ApplyEditorProps {
  wave: OpenWave;
  application?: PromotionApplication;
  isBusy: boolean;
  onSaveDraft: (draft: ApplicationDraft) => Promise<unknown>;
  onSend: (draft: ApplicationDraft) => Promise<unknown>;
  onDelete?: () => void;
}

/** The form with the target and the people it offers; the target chosen belongs to the application the form was opened with. */
function ApplyEditor({
  wave,
  application,
  isBusy,
  onSaveDraft,
  onSend,
  onDelete,
}: ApplyEditorProps) {
  const { data: user } = useCurrentUser();
  const employees = useRaterOptions("rater-picker");
  const [target, setTarget] = useState<TargetRef | undefined>(
    application && {
      orgUnitId: application.target.unitId,
      matrixId: application.target.matrixId,
    },
  );
  const targetForm = useQuery({
    ...targetFormQuery(
      wave.id,
      target?.orgUnitId ?? "",
      target?.matrixId ?? "",
    ),
    enabled: target !== undefined,
  });
  return (
    <Flex vertical gap="middle">
      {targetForm.error && (
        <ErrorAlert
          title="Не удалось загрузить компетенции"
          error={targetForm.error}
          onRetry={() => void targetForm.refetch()}
        />
      )}
      <ApplyView
        wave={wave}
        application={application}
        targetForm={targetForm.data}
        isTargetLoading={targetForm.isFetching}
        candidates={employees.candidates}
        candidateEmployeeId={user?.employeeId ?? undefined}
        isSearching={employees.isSearching}
        isBusy={isBusy}
        onSearch={employees.onSearch}
        onTargetChange={setTarget}
        onSaveDraft={onSaveDraft}
        onSend={onSend}
        onDelete={onDelete}
      />
    </Flex>
  );
}
