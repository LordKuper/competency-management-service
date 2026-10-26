import { Select } from "antd";
import type { CompetencyType } from "./catalogApi";

/** What each type of a competency in a matrix is called. */
export const COMPETENCY_TYPES: Record<CompetencyType, string> = {
  Critical: "Критическая",
  Profile: "Профильная",
  Additional: "Дополнительная",
};

/** The type a competency has in a matrix until the author changes it. */
export const DEFAULT_COMPETENCY_TYPE: CompetencyType = "Profile";

const OPTIONS = (Object.keys(COMPETENCY_TYPES) as CompetencyType[]).map(
  (type) => ({ value: type, label: COMPETENCY_TYPES[type] }),
);

interface CompetencyTypeSelectProps {
  value: CompetencyType;
  onChange: (type: CompetencyType) => void;
  label: string;
}

/** The compact choice of the type a competency has in a matrix. */
export function CompetencyTypeSelect({
  value,
  onChange,
  label,
}: CompetencyTypeSelectProps) {
  return (
    <Select<CompetencyType>
      size="small"
      aria-label={label}
      value={value}
      options={OPTIONS}
      onChange={onChange}
    />
  );
}
