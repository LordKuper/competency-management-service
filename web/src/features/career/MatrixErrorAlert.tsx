import { Alert } from "antd";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";

const FORBIDDEN_STATUS = 403;
const PROBLEMS_FIELD = "content";

interface MatrixErrorAlertProps {
  /** What failed, in a few words. */
  title: string;
  error: unknown;
}

/**
 * Failure of a matrix call. A refusal by turn and the list of everything wrong with a version are the server's own
 * words and are shown as written; any other failure gets the common explanation.
 */
export function MatrixErrorAlert({ title, error }: MatrixErrorAlertProps) {
  const problems =
    error instanceof ApiError ? (error.errors?.[PROBLEMS_FIELD] ?? []) : [];
  const isForbidden =
    error instanceof ApiError && error.status === FORBIDDEN_STATUS;
  if (!(error instanceof ApiError) || (problems.length === 0 && !isForbidden)) {
    return <ErrorAlert title={title} error={error} />;
  }

  const summary =
    error.detail ?? (problems.length === 0 ? describeApiError(error) : null);
  return (
    <Alert
      type="error"
      showIcon
      title={title}
      description={
        <>
          {summary}
          {problems.length > 0 && (
            <ul>
              {problems.map((problem) => (
                <li key={problem}>{problem}</li>
              ))}
            </ul>
          )}
        </>
      }
    />
  );
}
