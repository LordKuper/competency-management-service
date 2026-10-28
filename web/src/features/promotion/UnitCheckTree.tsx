import { Flex, Tree, Typography } from "antd";
import { useMemo } from "react";
import type { OrgUnitTreeNode } from "../org-structure/orgStructureApi";
import { unitsWithin } from "../org-structure/orgTree";
import { unitTreeItems } from "./answerableUnits";

interface UnitCheckTreeProps {
  /** Element id that associates the form item label with the control; supplied by the surrounding form item. */
  id?: string;
  /** Identifiers of the checked units; supplied by the surrounding form item. */
  value?: string[];
  onChange?: (value: string[]) => void;
  /** The units offered, arranged by their parent links. */
  units: readonly OrgUnitTreeNode[];
  disabled?: boolean;
}

/**
 * Units with a check box each. Checking a unit checks the units below it that are offered; each of them can be
 * unchecked on its own, and unchecking a unit leaves the ones below it as they are.
 */
export function UnitCheckTree({
  id,
  value = [],
  onChange,
  units,
  disabled,
}: UnitCheckTreeProps) {
  const items = useMemo(() => unitTreeItems(units), [units]);
  if (units.length === 0) {
    return (
      <Typography.Text type="secondary">
        Нет подразделений, за которые вы отвечаете как HR.
      </Typography.Text>
    );
  }
  return (
    <Flex vertical gap="small" id={id}>
      <Typography.Text type="secondary">
        Отмечено: {value.length}
      </Typography.Text>
      <fieldset
        aria-label="Подразделения волны"
        style={{
          border: 0,
          margin: 0,
          padding: 0,
          minWidth: 0,
          maxHeight: "calc(var(--ant-control-height) * 12)",
          overflow: "auto",
        }}
      >
        <Tree
          checkable
          checkStrictly
          selectable={false}
          defaultExpandAll
          disabled={disabled}
          treeData={items}
          checkedKeys={value}
          onCheck={(_, info) => {
            const unitId = String(info.node.key);
            const checked = new Set(value);
            if (info.checked) {
              for (const below of unitsWithin(units, [unitId])) {
                checked.add(below);
              }
            } else {
              checked.delete(unitId);
            }
            onChange?.([...checked]);
          }}
        />
      </fieldset>
    </Flex>
  );
}
