import { Alert } from "antd";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import type { PublicationBlocker } from "../career/catalogApi";
import { MatrixErrorAlert } from "../career/MatrixErrorAlert";
import { RefusalContent } from "../career/usePublishEntry";

const REFUSED_STATUS = 409;
const STALE_STATUS = 412;

interface DecisionErrorProps {
  /** What did not work, in a few words. */
  title: string;
  error: unknown;
}

/**
 * Why a decision did not go through. A refusal that names the records in the way says what blocks and what to do next;
 * a version changed by someone else is a warning, since the screen shows the new one and the user decides again.
 */
export function DecisionError({ title, error }: DecisionErrorProps) {
  if (error instanceof ApiError && error.status === STALE_STATUS) {
    return <Alert type="warning" showIcon title={describeApiError(error)} />;
  }
  if (
    error instanceof ApiError &&
    error.status === REFUSED_STATUS &&
    (error.blockers.length > 0 || error.moreCount > 0)
  ) {
    return (
      <Alert
        type="error"
        showIcon
        title={error.detail ?? title}
        description={
          <RefusalContent
            blockers={error.blockers as unknown as PublicationBlocker[]}
            moreCount={error.moreCount}
          />
        }
      />
    );
  }
  return <MatrixErrorAlert title={title} error={error} />;
}
