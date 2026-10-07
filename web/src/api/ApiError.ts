/** Failed API call: the HTTP status plus the problem-details fields the server sent. */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string | undefined;
  readonly detail: string | undefined;
  /** Validation messages per field name, present only on validation problems. */
  readonly errors: Readonly<Record<string, readonly string[]>> | undefined;

  /** @param problem Response body of the failed call; any shape is tolerated, unknown fields are ignored. */
  constructor(status: number, problem: unknown) {
    const fields = isRecord(problem) ? problem : {};
    const title = readString(fields.title);
    super(title ?? `HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.title = title;
    this.detail = readString(fields.detail);
    this.errors = readFieldErrors(fields.errors);
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readFieldErrors(value: unknown): Record<string, string[]> | undefined {
  if (!isRecord(value)) return undefined;
  const entries = Object.entries(value).filter(
    (entry): entry is [string, string[]] =>
      Array.isArray(entry[1]) &&
      entry[1].every((message) => typeof message === "string"),
  );
  return entries.length > 0 ? Object.fromEntries(entries) : undefined;
}
