import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, App, Breadcrumb, Flex, Skeleton, Typography } from "antd";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useLeaveGuard } from "../../app/leaveGuard";
import { contentMaxWidth } from "../../app/theme";
import { useEditBase } from "../../app/useEditBase";
import { useCurrentUser } from "../auth/useCurrentUser";
import { ApplyView } from "./ApplyView";
import {
  type ApplicationInput,
  applicationQuery,
  createApplication,
  myApplicationsQuery,
  type OpenWave,
  openWavesQuery,
  type PromotionApplication,
  promotionQueryKey,
  saveApplication,
  targetFormQuery,
  type Versioned,
} from "./promotionApi";
import { formatDate, STAGE } from "./promotionTexts";
import { promotionPaths } from "./routes";
import { useEmployeeOptions } from "./useEmployeeOptions";

/** Filing an application in a wave, or changing the one already filed while the wave still takes applications. */
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
          { title: <Link to={promotionPaths.home}>Повышение</Link> },
          { title: "Заявка" },
        ]}
      />
      <Typography.Title level={1} style={{ margin: 0 }}>
        Заявка на повышение
      </Typography.Title>
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
        title="Не удалось загрузить волну"
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
        title="Заявки в этой волне сейчас не принимаются"
        description={
          wave && wave.stage < STAGE.submission
            ? `Приём заявок начнётся ${formatDate(wave.startsOn)}.`
            : "Приём заявок в этой волне закончился или волны нет."
        }
      />
    );
  }
  const filed = mine.data.find(
    (application) =>
      application.waveId === waveId && application.status === "Submitted",
  );
  return filed ? (
    <FiledApplication wave={wave} applicationId={filed.id} />
  ) : (
    <ApplyForm wave={wave} />
  );
}

function FiledApplication({
  wave,
  applicationId,
}: {
  wave: OpenWave;
  applicationId: string;
}) {
  const { data, error, isPending, refetch } = useQuery(
    applicationQuery(applicationId),
  );
  if (isPending) return <Skeleton active />;
  if (error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить заявку"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <ApplyForm wave={wave} latest={data} />;
}

/** The form with the application it was opened with; it takes the reread one only after a save is refused for a stale version. */
function ApplyForm({
  wave,
  latest,
}: {
  wave: OpenWave;
  latest?: Versioned<PromotionApplication>;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const { leaveAfterSave } = useLeaveGuard();
  const { base, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: (input: ApplicationInput) =>
      base
        ? saveApplication(base.id, base.version, input)
        : createApplication(wave.id, input),
    onSuccess: (saved) => {
      message.success(base ? "Заявка сохранена" : "Заявка подана");
      leaveAfterSave(() => void navigate(promotionPaths.application(saved.id)));
    },
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  return (
    <Flex vertical gap="middle">
      {save.isError && (
        <ErrorAlert title="Заявка не сохранена" error={save.error} />
      )}
      <ApplyEditor
        key={base?.version}
        wave={wave}
        application={base}
        onSubmit={save.mutateAsync}
      />
    </Flex>
  );
}

interface ApplyEditorProps {
  wave: OpenWave;
  application?: PromotionApplication;
  onSubmit: (input: ApplicationInput) => Promise<unknown>;
}

/** The form with the target and the people it offers; the target chosen belongs to the application the form was opened with. */
function ApplyEditor({ wave, application, onSubmit }: ApplyEditorProps) {
  const { data: user } = useCurrentUser();
  const employees = useEmployeeOptions("rater-picker");
  const [matrixId, setMatrixId] = useState(application?.target.matrixId);
  const targetForm = useQuery({
    ...targetFormQuery(wave.id, matrixId ?? ""),
    enabled: matrixId !== undefined,
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
        onSearch={employees.onSearch}
        onTargetChange={setMatrixId}
        onSubmit={onSubmit}
      />
    </Flex>
  );
}
