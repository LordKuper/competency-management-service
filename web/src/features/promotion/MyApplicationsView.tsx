import { Card, Empty, Flex, Tag, Typography } from "antd";
import { Link } from "react-router";
import type { WaveApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  DECISION_TAGS,
  formatDate,
} from "./promotionTexts";
import { promotionPaths } from "./routes";

type Decision = keyof typeof DECISION_TAGS;

interface MyApplicationsViewProps {
  applications: readonly WaveApplication[];
  /** The decision on an application by its id, for those whose decision the viewer may already see. */
  decisions: ReadonlyMap<string, Decision>;
}

/** The applications of the viewer, active and past, as cards with the status and the decision. */
export function MyApplicationsView({
  applications,
  decisions,
}: MyApplicationsViewProps) {
  if (applications.length === 0) {
    return (
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description="Вы ещё не подавали заявок на повышение."
      />
    );
  }
  return (
    <Flex vertical gap="middle">
      {applications.map((application) => {
        const status = APPLICATION_STATUS_TAGS[application.status];
        const decision = decisions.get(application.id);
        return (
          <Card
            key={application.id}
            size="small"
            title={`${application.target.unitName}: ${application.target.trackName}, ${application.target.levelName}`}
            extra={
              <Link
                to={promotionPaths.application(application.id)}
                aria-label={`Открыть заявку: ${application.target.trackName}, ${application.target.levelName}, волна с ${formatDate(application.waveStartsOn)}`}
              >
                Открыть
              </Link>
            }
          >
            <Flex align="center" gap="small" wrap>
              <Tag color={status.color}>{status.label}</Tag>
              {decision && (
                <Tag color={DECISION_TAGS[decision].color}>
                  {DECISION_TAGS[decision].label}
                </Tag>
              )}
              <Typography.Text type="secondary">
                Волна с {formatDate(application.waveStartsOn)}, подана{" "}
                {formatDate(application.submittedAt)}
              </Typography.Text>
            </Flex>
          </Card>
        );
      })}
    </Flex>
  );
}
