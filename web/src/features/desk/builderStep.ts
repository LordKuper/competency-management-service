import type { LeaveEdits } from "../../app/leaveGuard";
import type { MatrixVersion } from "../career/matrixApi";

/** What a screen of the matrix wizard is given for its step of an existing version. */
export interface StepProps {
  version: MatrixVersion;
  /** The step shown. */
  step: number;
  onBack: () => void;
  /** Called once a save that asked to go on or to leave succeeded. */
  onSaved: (then: "next" | "exit") => void;
  /** Goes on without saving, for a version the user may not edit. */
  onSkip: () => void;
  /** Told whether the step holds edits that are not saved yet and whether a save is in flight. */
  onEditsChange: (edits: LeaveEdits) => void;
}
