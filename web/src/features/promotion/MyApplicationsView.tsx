import { Card, Empty, Flex, Tag, Typography } from "antd";
import { Link } from "react-router";
import type { WaveApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  DECISION_TAGS,
  formatDate,
} from "./promotionTexts";
import { promotionPaths } from "./routes";

interface MyApplicationsViewProps {
  applications: readonly WaveApplication[];
}

/** The applications of the viewer, active and past, as cards with the status and the decision. */
export function MyApplicationsView({ applications }: MyApplicationsViewProps) {
  if (applications.length === 0) {
    return (
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description="Вы ещё не подавали заявок на карьерный переход."
      />
    );
  }
  return (
    <Flex vertical gap="middle">
      {applications.map((application) => {
        const status = APPLICATION_STATUS_TAGS[application.status];
        const decision = application.decision;
        const isDraft = application.status === "Draft";
        return (
          <Card
            key={application.id}
            size="small"
            title={`${application.target.unitName}: ${application.target.trackName}, ${application.target.levelName}`}
            extra={
              <Link
                to={
                  isDraft
                    ? promotionPaths.apply(application.waveId)
                    : promotionPaths.application(application.id)
                }
                aria-label={`${isDraft ? "Продолжить черновик" : "Открыть заявку"}: ${application.target.trackName}, ${application.target.levelName}, карьерная сессия с ${formatDate(application.waveStartsOn)}`}
              >
                {isDraft ? "Продолжить" : "Открыть"}
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
                {isDraft
                  ? `Карьерная сессия с ${formatDate(application.waveStartsOn)}, не отправлена`
                  : `Карьерная сессия с ${formatDate(application.waveStartsOn)}, подана ${formatDate(application.submittedAt)}`}
              </Typography.Text>
            </Flex>
          </Card>
        );
      })}
    </Flex>
  );
}
