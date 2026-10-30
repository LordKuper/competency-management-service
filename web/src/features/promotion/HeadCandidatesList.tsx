import { Flex, Tag, Typography } from "antd";
import { Link } from "react-router";
import { Panel } from "./Panel";
import type { HeadCandidate } from "./promotionApi";
import { raterCountText } from "./promotionTexts";
import { promotionPaths } from "./routes";

function CandidateNote({ candidate }: { candidate: HeadCandidate }) {
  const counts = candidate.raterCounts;
  return (
    <Flex wrap gap="small" align="center">
      <Typography.Text type="secondary">
        {candidate.target.levelName} ·{" "}
        {counts.missingTotal > 0
          ? `${counts.total} из ${counts.requiredTotal}, нужен ещё ${counts.missingTotal}`
          : raterCountText(counts.total)}
      </Typography.Text>
      {counts.pendingEmailCount > 0 && (
        <Tag color="warning">
          {counts.pendingEmailCount} почта ждёт подтверждения
        </Tag>
      )}
    </Flex>
  );
}

interface HeadCandidatesListProps {
  candidates: readonly HeadCandidate[];
  /** The application whose list of raters is open, marked in the list. */
  currentId: string;
}

/** The candidates of the units a head leads, each with the level aimed at and how far the list of raters is from complete, so the head can go through them one by one. */
export function HeadCandidatesList({
  candidates,
  currentId,
}: HeadCandidatesListProps) {
  return (
    <Panel title="Кандидаты" level={3}>
      <Flex
        vertical
        gap="middle"
        component="ul"
        aria-label="Кандидаты"
        style={{ margin: 0, padding: 0 }}
      >
        {candidates.map((candidate) => (
          <li key={candidate.applicationId} style={{ listStyle: "none" }}>
            <Flex vertical>
              <Link
                to={promotionPaths.raters(candidate.applicationId)}
                aria-current={
                  candidate.applicationId === currentId || undefined
                }
              >
                {candidate.applicationId === currentId ? (
                  <b>{candidate.candidateName}</b>
                ) : (
                  candidate.candidateName
                )}
              </Link>
              <CandidateNote candidate={candidate} />
            </Flex>
          </li>
        ))}
      </Flex>
    </Panel>
  );
}
