import { useQuery } from "@tanstack/react-query";
import { TreeSelect, type TreeSelectProps } from "antd";
import { useMemo } from "react";
import { describeApiError } from "../../app/apiErrors";
import { orgUnitTreeQuery } from "./orgStructureApi";
import { buildUnitTree, type UnitNode } from "./orgTree";

interface UnitOption {
  value: string;
  title: string;
  disabled: boolean;
  children: UnitOption[];
}

type OrgUnitSelectProps = Pick<
  TreeSelectProps<string | undefined>,
  "id" | "value" | "onChange" | "allowClear" | "placeholder"
> & {
  /** Unit to leave out together with everything below it, so a unit cannot be offered as its own parent. */
  excludedSubtreeRoot?: string;
  /** Whether inactive units are listed but cannot be chosen, for fields that need a working unit. */
  isActiveRequired?: boolean;
};

function toOptions(
  nodes: readonly UnitNode[],
  excludedId: string | undefined,
  isActiveRequired: boolean,
): UnitOption[] {
  return nodes
    .filter(({ unit }) => unit.id !== excludedId)
    .map(({ unit, children }) => ({
      value: unit.id,
      title: unit.isActive ? unit.name : `${unit.name} (неактивно)`,
      disabled: isActiveRequired && !unit.isActive,
      children: toOptions(children, excludedId, isActiveRequired),
    }));
}

/** Searchable tree select over every visible unit; the server still decides whether the choice is allowed. */
export function OrgUnitSelect({
  excludedSubtreeRoot,
  isActiveRequired = false,
  ...selectProps
}: OrgUnitSelectProps) {
  const { data: units, error, isPending } = useQuery(orgUnitTreeQuery);
  const options = useMemo(
    () =>
      toOptions(
        buildUnitTree(units ?? []),
        excludedSubtreeRoot,
        isActiveRequired,
      ),
    [units, excludedSubtreeRoot, isActiveRequired],
  );

  return (
    <TreeSelect<string | undefined, UnitOption>
      {...selectProps}
      showSearch={{ treeNodeFilterProp: "title" }}
      treeData={options}
      loading={isPending}
      notFoundContent={
        error ? describeApiError(error) : "Подразделения не найдены."
      }
    />
  );
}
