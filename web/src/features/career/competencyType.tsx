import { WarningOutlined } from "@ant-design/icons";
import { Select, Tag } from "antd";
import type { CompetencyType } from "./catalogApi";

/** What each type of a competency in a matrix is called. */
export const COMPETENCY_TYPES: Record<CompetencyType, string> = {
  Critical: "Критическая",
  Profile: "Профильная",
  Additional: "Дополнительная",
};

/** The type a competency has in a matrix until the author changes it. */
export const DEFAULT_COMPETENCY_TYPE: CompetencyType = "Profile";

/** The type in words; a critical one with the warning sign, so it never stands on colour alone. */
export function CompetencyTypeLabel({ type }: { type: CompetencyType }) {
  return type === "Critical" ? (
    <Tag icon={<WarningOutlined />} color="error">
      {COMPETENCY_TYPES.Critical}
    </Tag>
  ) : (
    COMPETENCY_TYPES[type]
  );
}

const OPTIONS = (Object.keys(COMPETENCY_TYPES) as CompetencyType[]).map(
  (type) => ({ value: type, label: <CompetencyTypeLabel type={type} /> }),
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
