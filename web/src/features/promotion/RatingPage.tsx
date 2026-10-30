import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Flex, Skeleton, Typography } from "antd";
import { Link, useNavigate, useParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import {
  myRatingsQuery,
  promotionQueryKey,
  type RatingAnswer,
  ratingQuery,
  saveRating,
} from "./promotionApi";
import { RatingView } from "./RatingView";
import { promotionPaths } from "./routes";

/** The rating of one candidate by the signed-in rater. */
export function RatingPage() {
  const { applicationId } = useParams();
  return applicationId ? <LoadedRating applicationId={applicationId} /> : null;
}

function LoadedRating({ applicationId }: { applicationId: string }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const queue = useQuery(myRatingsQuery);
  const { data, error, isPending, refetch } = useQuery(
    ratingQuery(applicationId),
  );
  const save = useMutation({
    mutationFn: (answers: RatingAnswer[]) => saveRating(applicationId, answers),
    onSuccess: (saved) => {
      queryClient.setQueryData(ratingQuery(applicationId).queryKey, saved);
      message.success("Оценка сохранена");
    },
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  const tasks = queue.data ?? [];
  const currentIndex = tasks.findIndex(
    (task) => task.applicationId === applicationId,
  );
  const nextTask = currentIndex >= 0 ? tasks[currentIndex + 1] : undefined;
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.home}>Карьерные переходы</Link> },
          { title: <Link to={promotionPaths.ratings}>Оценка кандидатов</Link> },
          { title: data?.candidateName ?? "Кандидат" },
        ]}
      />
      <Typography.Title level={1} style={{ margin: 0 }}>
        Круговая оценка
      </Typography.Title>
      {(isPending || queue.isPending) && <Skeleton active />}
      {queue.error && (
        <ErrorAlert
          title="Не удалось загрузить список кандидатов"
          error={queue.error}
          onRetry={() => void queue.refetch()}
        />
      )}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить оценку"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {save.isError && (
        <ErrorAlert title="Оценка не сохранена" error={save.error} />
      )}
      {data && queue.data && (
        <RatingView
          key={data.applicationId}
          rating={data}
          tasks={queue.data}
          onSubmit={save.mutateAsync}
          onOpenNext={
            nextTask
              ? () =>
                  void navigate(promotionPaths.rating(nextTask.applicationId))
              : undefined
          }
        />
      )}
    </Flex>
  );
}
