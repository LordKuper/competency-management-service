import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Flex, Skeleton } from "antd";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useLeaveGuard } from "../../app/leaveGuard";
import { contentMaxWidth } from "../../app/theme";
import { useEditBase } from "../../app/useEditBase";
import { useCurrentUser, useIsAdmin } from "../auth/useCurrentUser";
import { useAnswerableUnits } from "./answerableUnits";
import {
  createWave,
  promotionQueryKey,
  saveWave,
  settingsQuery,
  type Versioned,
  type Wave,
  type WaveInput,
  waveQuery,
} from "./promotionApi";
import { formatDate } from "./promotionTexts";
import { RollbackDialog } from "./RollbackDialog";
import { promotionPaths } from "./routes";
import { WaveApplications } from "./WaveApplications";
import { WaveView } from "./WaveView";

/** The career session screen: announcing a new session, or the dates, units, stage and applications of an existing one. */
export function WavePage() {
  const { id } = useParams();
  return id ? <ExistingWave id={id} /> : <WaveEditor />;
}

function ExistingWave({ id }: { id: string }) {
  const { data, error, isPending, refetch } = useQuery(waveQuery(id));
  if (isPending) return <Skeleton active />;
  if (error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить карьерную сессию"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <WaveEditor latest={data} />;
}

/** The screen with the wave it was opened with; it takes the reread one only after a save is refused for a stale version. */
function WaveEditor({ latest }: { latest?: Versioned<Wave> }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const { leaveAfterSave } = useLeaveGuard();
  const { data: user } = useCurrentUser();
  const isAdmin = useIsAdmin();
  const { base, noteSaveFailure, rebase } = useEditBase(latest);
  const settings = useQuery(settingsQuery);
  const answerable = useAnswerableUnits(base?.units.map((unit) => unit.id));
  const [isRollbackOpen, setIsRollbackOpen] = useState(false);
  const save = useMutation({
    mutationFn: (input: WaveInput) =>
      base ? saveWave(base.id, base.version, input) : createWave(input),
    onSuccess: (saved) => {
      if (base) {
        rebase(saved);
        message.success("Карьерная сессия сохранена");
      } else {
        message.success("Карьерная сессия объявлена");
        leaveAfterSave(() => void navigate(promotionPaths.wave(saved.id)));
      }
    },
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  const loadFailure = settings.error ?? answerable.error;

  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.waves}>Карьерные сессии</Link> },
          {
            title: base
              ? `Объявлена ${formatDate(base.announcedOn)}`
              : "Новая карьерная сессия",
          },
        ]}
      />
      {loadFailure && (
        <ErrorAlert
          title="Не удалось загрузить данные для карьерной сессии"
          error={loadFailure}
          onRetry={() => {
            void settings.refetch();
            answerable.refetch();
          }}
        />
      )}
      {save.isError && (
        <ErrorAlert title="Карьерная сессия не сохранена" error={save.error} />
      )}
      {settings.data && answerable.units ? (
        <WaveView
          key={base?.version}
          wave={base}
          units={answerable.units}
          durations={settings.data.stageDurationsDays}
          canEdit={!base || isAdmin || base.authorId === user?.id}
          onSubmit={save.mutateAsync}
          onRollback={() => setIsRollbackOpen(true)}
        />
      ) : (
        !loadFailure && <Skeleton active />
      )}
      {base && isRollbackOpen && settings.data && (
        <RollbackDialog
          waveId={base.id}
          durations={settings.data.stageDurationsDays}
          onRolledBack={(saved) => {
            rebase(saved);
            setIsRollbackOpen(false);
          }}
          onClose={() => setIsRollbackOpen(false)}
        />
      )}
      {base && base.stage >= 1 && <WaveApplications wave={base} />}
    </Flex>
  );
}
