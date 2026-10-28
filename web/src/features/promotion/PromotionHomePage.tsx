import { useQuery } from "@tanstack/react-query";
import { Alert, Flex, Skeleton, Typography } from "antd";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { useCurrentUser } from "../auth/useCurrentUser";
import { MyApplicationsView } from "./MyApplicationsView";
import { OpenWavesView } from "./OpenWavesView";
import {
  myApplicationsQuery,
  myRatingsQuery,
  openWavesQuery,
} from "./promotionApi";
import { promotionPaths } from "./routes";

const UNFINISHED_STATUSES = ["Submitted", "Admitted", "UnderReview"];

/** The promotion screen of every signed-in user: the waves one can apply to, their own applications, and the candidates they are asked to rate. */
export function PromotionHomePage() {
  const { data: user } = useCurrentUser();
  const waves = useQuery(openWavesQuery);
  const mine = useQuery(myApplicationsQuery);
  const ratings = useQuery(myRatingsQuery);
  const applicationIdByWave = new Map(
    (mine.data ?? [])
      .filter((application) => UNFINISHED_STATUSES.includes(application.status))
      .map((application) => [application.waveId, application.id] as const),
  );
  const toRate = (ratings.data ?? []).filter(
    (task) => task.canRate && !task.isRated,
  ).length;

  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Typography.Title level={1} style={{ margin: 0 }}>
        Повышение
      </Typography.Title>
      {toRate > 0 && (
        <Alert
          type="info"
          showIcon
          title={`Вас просят оценить кандидатов: ${toRate}`}
          action={<Link to={promotionPaths.ratings}>Перейти к оценке</Link>}
        />
      )}
      <Flex vertical gap="middle">
        <Typography.Title level={2} style={{ margin: 0 }}>
          Волны
        </Typography.Title>
        {waves.isPending && <Skeleton active />}
        {waves.error && (
          <ErrorAlert
            title="Не удалось загрузить волны"
            error={waves.error}
            onRetry={() => void waves.refetch()}
          />
        )}
        {waves.data && (
          <OpenWavesView
            waves={waves.data}
            applicationIdByWave={applicationIdByWave}
            canApply={user?.employeeId != null}
          />
        )}
      </Flex>
      <Flex vertical gap="middle">
        <Typography.Title level={2} style={{ margin: 0 }}>
          Мои заявки
        </Typography.Title>
        {mine.isPending && <Skeleton active />}
        {mine.error && (
          <ErrorAlert
            title="Не удалось загрузить заявки"
            error={mine.error}
            onRetry={() => void mine.refetch()}
          />
        )}
        {mine.data && <MyApplicationsView applications={mine.data} />}
      </Flex>
    </Flex>
  );
}
