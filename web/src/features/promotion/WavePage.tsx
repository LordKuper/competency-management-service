import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Flex, Skeleton, Typography } from "antd";
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
import { promotionPaths } from "./routes";
import { WaveApplications } from "./WaveApplications";
import { WaveView } from "./WaveView";

/** The wave screen: announcing a new wave, or the dates, units, stage and applications of an existing one. */
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
        title="Не удалось загрузить волну"
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
  const save = useMutation({
    mutationFn: (input: WaveInput) =>
      base ? saveWave(base.id, base.version, input) : createWave(input),
    onSuccess: (saved) => {
      if (base) {
        rebase(saved);
        message.success("Волна сохранена");
      } else {
        message.success("Волна объявлена");
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
          { title: <Link to={promotionPaths.waves}>Волны</Link> },
          { title: base ? `с ${formatDate(base.startsOn)}` : "Новая волна" },
        ]}
      />
      <Typography.Title level={1} style={{ margin: 0 }}>
        {base ? `Волна с ${formatDate(base.startsOn)}` : "Новая волна"}
      </Typography.Title>
      {loadFailure && (
        <ErrorAlert
          title="Не удалось загрузить данные для волны"
          error={loadFailure}
          onRetry={() => {
            void settings.refetch();
            answerable.refetch();
          }}
        />
      )}
      {save.isError && (
        <ErrorAlert title="Волна не сохранена" error={save.error} />
      )}
      {settings.data && answerable.units ? (
        <WaveView
          key={base?.version}
          wave={base}
          units={answerable.units}
          durations={settings.data.stageDurationsDays}
          canEdit={!base || isAdmin || base.authorId === user?.id}
          onSubmit={save.mutateAsync}
        />
      ) : (
        !loadFailure && <Skeleton active />
      )}
      {base && base.stage >= 1 && <WaveApplications waveId={base.id} />}
    </Flex>
  );
}
