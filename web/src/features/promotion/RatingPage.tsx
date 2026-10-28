import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Flex, Skeleton, Typography } from "antd";
import { Link, useParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import {
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
  const { message } = App.useApp();
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
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.home}>Повышение</Link> },
          { title: <Link to={promotionPaths.ratings}>Оценка кандидатов</Link> },
          { title: data?.candidateName ?? "Кандидат" },
        ]}
      />
      <Typography.Title level={1} style={{ margin: 0 }}>
        {data ? `Оценка: ${data.candidateName}` : "Оценка кандидата"}
      </Typography.Title>
      {isPending && <Skeleton active />}
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
      {data && <RatingView rating={data} onSubmit={save.mutateAsync} />}
    </Flex>
  );
}
