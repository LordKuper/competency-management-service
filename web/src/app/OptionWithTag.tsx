import type { ReactNode } from "react";

interface OptionWithTagProps {
  /** The name, cut with an ellipsis when it does not fit; the full text is the tooltip. */
  label: string;
  /** The status tag, which keeps its full width. */
  tag: ReactNode;
}

/** The content of a select option or of a selected value that is a name with a status tag. */
export function OptionWithTag({ label, tag }: OptionWithTagProps) {
  return (
    <span className="option-with-tag">
      <span className="option-with-tag__name option-truncate" title={label}>
        {label}
      </span>
      <span className="option-with-tag__tag">{tag}</span>
    </span>
  );
}
