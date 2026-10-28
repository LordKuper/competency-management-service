import { useQuery } from "@tanstack/react-query";
import { Breadcrumb, Flex, Skeleton, Typography } from "antd";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { myRatingsQuery } from "./promotionApi";
import { RatingsView } from "./RatingsView";
import { promotionPaths } from "./routes";

/** The candidates the signed-in employee is asked to rate. */
export function RatingsPage() {
  const { data, error, isPending, refetch } = useQuery(myRatingsQuery);
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.home}>Повышение</Link> },
          { title: "Оценка кандидатов" },
        ]}
      />
      <Typography.Title level={1} style={{ margin: 0 }}>
        Оценка кандидатов
      </Typography.Title>
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить список"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {data && <RatingsView tasks={data} />}
    </Flex>
  );
}
