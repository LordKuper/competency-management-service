import { Breadcrumb, Flex } from "antd";
import type { ReactNode } from "react";
import { Link } from "react-router";
import { ApiError } from "../../api/ApiError";
import { ErrorAlert } from "../../app/ErrorAlert";
import { NotFoundPage } from "../../app/NotFoundPage";
import { contentMaxWidth } from "../../app/theme";
import { deskPaths } from "./deskTexts";
import { useReturnLink } from "./returnLink";

interface ScenarioPageProps {
  /** Name of the scenario, shown as the current item of the breadcrumb. */
  title: string;
  children: ReactNode;
}

/** Frame of a scenario screen: the breadcrumb back to the desk (through the scenario it was opened from) and the content; the breadcrumb is the only heading. */
export function ScenarioPage({ title, children }: ScenarioPageProps) {
  const origin = useReturnLink();
  return (
    <Flex
      vertical
      gap="middle"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        data-tour="scenario-path"
        items={[
          { title: <Link to={deskPaths.desk}>Рабочий стол</Link> },
          ...(origin
            ? [{ title: <Link to={origin.to}>{origin.crumb}</Link> }]
            : []),
          { title },
        ]}
      />
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
