import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** What reading a file gave: the full list of errors that make it unusable, or the plan when there are none. */
export type OrgImportPreview =
  components["schemas"]["OrgImportPreviewResponse"];

/** What applying the file would change, by group, and the units and employees the file does not name. */
export type OrgImportPlan = components["schemas"]["OrgImportPlanResponse"];

/** The changes an applied import made, by group. */
export type OrgImportResult = components["schemas"]["OrgImportResultResponse"];

/** One problem of the file; a row or a column is empty when the problem is not about a single one. */
export type ImportError = components["schemas"]["ImportErrorResponse"];

/** An active unit the file does not name. */
export type MissingUnit = components["schemas"]["MissingUnitResponse"];

/** A working employee the file does not name. */
export type MissingEmployee = components["schemas"]["MissingEmployeeResponse"];

/** The decision about one missing unit or employee, as the apply request carries it. */
export interface ImportDecision {
  id: string;
  decision: "keep" | "deactivate" | "dismiss";
}

/** The apply request: the file and the fingerprint of the plan shown, with a decision for every missing unit and employee. */
export interface ApplyInput {
  file: File;
  fingerprint: string;
  decisions: readonly ImportDecision[];
}

/** The plan still held and was applied, or it changed and the file was read again: the fresh preview takes its place. */
export type ApplyOutcome =
  | { applied: OrgImportResult }
  | { recomputed: OrgImportPreview };

const PLAN_CHANGED_CODE = "planChanged";

function isPlanChanged(problem: unknown): boolean {
  return (
    typeof problem === "object" &&
    problem !== null &&
    "code" in problem &&
    problem.code === PLAN_CHANGED_CODE
  );
}

/** The generated client types a file part as a string; the form is built by hand, so the typed body only names the fields. */
function formOf(fields: Record<string, string | File>): FormData {
  const form = new FormData();
  for (const [name, value] of Object.entries(fields)) form.append(name, value);
  return form;
}

/** Reads the file and plans it against the organization; the server keeps nothing. */
export async function previewOrgImport(file: File): Promise<OrgImportPreview> {
  const form = formOf({ file });
  return unwrap(
    await api.POST("/api/v1/org-import/preview", {
      body: { file: "" },
      bodySerializer: () => form,
    }),
  );
}

/**
 * Applies the plan the fingerprint names. When the file or the organization no longer give that plan, the server
 * refuses with a "plan changed" conflict and the file is previewed again, so the user sees what would apply now; any
 * other refusal is raised as an `ApiError` with its reason.
 */
export async function applyOrgImport({
  file,
  fingerprint,
  decisions,
}: ApplyInput): Promise<ApplyOutcome> {
  const form = formOf({
    file,
    fingerprint,
    decisions: JSON.stringify(decisions),
  });
  const result = await api.POST("/api/v1/org-import/apply", {
    body: { file: "", fingerprint },
    bodySerializer: () => form,
  });
  if (result.response.status === 409 && isPlanChanged(result.error)) {
    return { recomputed: await previewOrgImport(file) };
  }
  return { applied: unwrap(result) };
}
