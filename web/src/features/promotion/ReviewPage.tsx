import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Flex, Skeleton, Typography } from "antd";
import { Link, useParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { useEditBase } from "../../app/useEditBase";
import {
  promotionQueryKey,
  type Review,
  type ReviewInput,
  reviewQuery,
  saveReview,
  type Versioned,
} from "./promotionApi";
import { ReviewView } from "./ReviewView";
import { promotionPaths } from "./routes";

/** The card of a candidate for the commission, in which the chair enters the final scores and the decision. */
export function ReviewPage() {
  const { applicationId } = useParams();
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.home}>Повышение</Link> },
          { title: "Карточка кандидата" },
        ]}
      />
      {applicationId && <LoadedReview id={applicationId} />}
    </Flex>
  );
}

function LoadedReview({ id }: { id: string }) {
  const { data, error, isPending, refetch } = useQuery(reviewQuery(id));
  if (isPending) return <Skeleton active />;
  if (error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить карточку кандидата"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <ReviewEditor latest={data} />;
}

/** The card with the version it was opened with; it takes the reread one only after a save is refused for a stale version. */
function ReviewEditor({ latest }: { latest: Versioned<Review> }) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { base = latest, noteSaveFailure, rebase } = useEditBase(latest);
  const save = useMutation({
    mutationFn: (input: ReviewInput) =>
      saveReview(base.id, base.version, input),
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Оценки и решение сохранены");
    },
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  return (
    <Flex vertical gap="large">
      <Typography.Title level={1} style={{ margin: 0 }}>
        {base.candidateName}
      </Typography.Title>
      {save.isError && (
        <ErrorAlert title="Оценки не сохранены" error={save.error} />
      )}
      <ReviewView
        key={base.version}
        review={base}
        onSubmit={save.mutateAsync}
      />
    </Flex>
  );
}
