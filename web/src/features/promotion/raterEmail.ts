import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";

/** An address as it is compared and sent: without surrounding blanks, and absent when empty. */
export function normalizedEmail(
  email: string | null | undefined,
): string | null {
  const trimmed = email?.trim();
  return trimmed ? trimmed : null;
}

/** Whether two addresses are the same one; the case of letters does not tell addresses apart. */
export function isSameEmail(
  first: string | null | undefined,
  second: string | null | undefined,
): boolean {
  return (
    normalizedEmail(first)?.toLowerCase() ===
    normalizedEmail(second)?.toLowerCase()
  );
}

/** Why an address was refused: the reason the server gives for the address itself, otherwise the usual description of the failure. */
export function describeEmailFailure(error: unknown): string {
  return error instanceof ApiError && error.errors?.email
    ? error.errors.email.join(" ")
    : describeApiError(error);
}
