import { theme as antdTheme, Breadcrumb, Flex, Typography } from "antd";
import type { ReactNode } from "react";
import { Link } from "react-router";
import { ApiError } from "../../api/ApiError";
import { ErrorAlert } from "../../app/ErrorAlert";
import { NotFoundPage } from "../../app/NotFoundPage";
import { contentMaxWidth } from "../../app/theme";
import { deskPaths } from "./deskTexts";

interface ScenarioPageProps {
  /** Name of the scenario, shown in the breadcrumb and as the page heading. */
  title: string;
  children: ReactNode;
}

/** Frame of a scenario screen: the way back to the desk, the heading and the content. */
export function ScenarioPage({ title, children }: ScenarioPageProps) {
  const { token } = antdTheme.useToken();
  return (
    <Flex
      vertical
      gap={token.marginLG}
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={deskPaths.desk}>Рабочий стол</Link> },
          { title },
        ]}
      />
      <Typography.Title level={1} style={{ margin: 0 }}>
        {title}
      </Typography.Title>
      {children}
    </Flex>
  );
}

interface LoadFailureProps {
  /** What did not load, in a few words. */
  title: string;
  error: unknown;
  onRetry: () => void;
}

/** A failed read: an object the user may not read looks like a missing page, any other failure can be retried. */
export function LoadFailure({ title, error, onRetry }: LoadFailureProps) {
  if (error instanceof ApiError && error.status === 404) {
    return <NotFoundPage />;
  }
  return <ErrorAlert title={title} error={error} onRetry={onRetry} />;
}
