import { Alert, Button } from "antd";
import { describeApiError } from "./apiErrors";

interface ErrorAlertProps {
  /** What failed, in a few words. */
  title: string;
  /** The failure; its explanation is derived from it. */
  error: unknown;
  /** Repeats the failed call; the retry button is shown only when given. */
  onRetry?: () => void;
}

/** Failure notice that says what failed, then why and what to do next. */
export function ErrorAlert({ title, error, onRetry }: ErrorAlertProps) {
  return (
    <Alert
      type="error"
      showIcon
      title={title}
      description={describeApiError(error)}
      action={onRetry && <Button onClick={onRetry}>Повторить</Button>}
    />
  );
}
