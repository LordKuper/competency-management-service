import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Flex, Skeleton, Typography } from "antd";
import { Link, useParams } from "react-router";
import { describeApiError } from "../../app/apiErrors";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { useEditBase } from "../../app/useEditBase";
import { ApplicationView } from "./ApplicationView";
import {
  type ArtifactInput,
  applicationQuery,
  markFeedbackReceived,
  type PromotionApplication,
  promotionQueryKey,
  saveArtifacts,
  type Versioned,
  withdrawApplication,
} from "./promotionApi";
import { promotionPaths } from "./routes";

/** An application: its status, stage and result, and what the stage asks the candidate to do with it. */
export function ApplicationPage() {
  const { id } = useParams();
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
      {id && <LoadedApplication id={id} />}
    </Flex>
  );
}

function LoadedApplication({ id }: { id: string }) {
  const { data, error, isPending, refetch } = useQuery(applicationQuery(id));
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
  return <ApplicationActions latest={data} />;
}

/** The application with the version it was opened with; it takes the reread one only after a change is refused for a stale version. */
function ApplicationActions({
  latest,
}: {
  latest: Versioned<PromotionApplication>;
}) {
  const queryClient = useQueryClient();
  const { message, modal } = App.useApp();
  const { base = latest, noteSaveFailure, rebase } = useEditBase(latest);
  const settle = {
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  };
  const withdraw = useMutation({
    mutationFn: () => withdrawApplication(base.id, base.version),
    ...settle,
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Заявка отозвана");
    },
  });
  const feedback = useMutation({
    mutationFn: () => markFeedbackReceived(base.id, base.version),
    ...settle,
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Отметка поставлена");
    },
  });
  const artifacts = useMutation({
    mutationFn: (items: ArtifactInput[]) =>
      saveArtifacts(base.id, base.version, items),
    ...settle,
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Артефакты сохранены");
    },
  });

  function confirmWithdrawal() {
    modal.confirm({
      title: "Отозвать заявку?",
      content: "Отозванная заявка дальше не рассматривается.",
      okText: "Отозвать заявку",
      ...destructiveConfirm(true),
      cancelText: "Отмена",
      onOk: () =>
        withdraw.mutateAsync().then(undefined, (failure) => {
          message.error(describeApiError(failure));
        }),
    });
  }

  return (
    <Flex vertical gap="middle">
      {feedback.isError && (
        <ErrorAlert title="Отметка не поставлена" error={feedback.error} />
      )}
      {artifacts.isError && (
        <ErrorAlert title="Артефакты не сохранены" error={artifacts.error} />
      )}
      <ApplicationView
        key={base.version}
        application={base}
        isBusy={withdraw.isPending || feedback.isPending}
        onWithdraw={confirmWithdrawal}
        onMarkFeedback={() => feedback.mutate()}
        onSaveArtifacts={artifacts.mutateAsync}
      />
    </Flex>
  );
}
