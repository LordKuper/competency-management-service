import { useQuery } from "@tanstack/react-query";
import { TreeSelect, theme } from "antd";
import type { FilterDropdownProps } from "antd/es/table/interface";
import { useMemo } from "react";
import { orgUnitTreeQuery } from "../org-structure/orgStructureApi";
import { buildUnitTree, type UnitNode } from "../org-structure/orgTree";
import { FILTER_WIDTH_IN_CONTROLS } from "./CatalogSelect";

interface TreeItem {
  value: string;
  title: string;
  children: TreeItem[];
}

function items(nodes: readonly UnitNode[]): TreeItem[] {
  return nodes.map(({ unit, children }) => ({
    value: unit.id,
    title: unit.name,
    children: items(children),
  }));
}

/** Filter dropdown with the unit hierarchy and a search box; choosing a unit applies the filter at once. */
function UnitTreeFilter({
  selectedKeys,
  setSelectedKeys,
  confirm,
}: FilterDropdownProps) {
  const { token } = theme.useToken();
  const { data, isFetching } = useQuery(orgUnitTreeQuery);
  const treeData = useMemo(() => items(buildUnitTree(data ?? [])), [data]);
  return (
    <div style={{ padding: token.paddingXS }}>
      <TreeSelect
        showSearch
        allowClear
        treeDefaultExpandAll
        treeNodeFilterProp="title"
        aria-label="Подразделение"
        loading={isFetching}
        treeData={treeData}
        value={selectedKeys[0] as string | undefined}
        style={{ width: token.controlHeight * FILTER_WIDTH_IN_CONTROLS }}
        popupMatchSelectWidth={false}
        // The tree opens inside the filter dropdown: a click in a popup outside it would close the dropdown.
        getPopupContainer={(node) => node.parentElement ?? document.body}
        onChange={(value: string | undefined) => {
          setSelectedKeys(value ? [value] : []);
          confirm();
        }}
      />
    </div>
  );
}

/**
 * Column props of a unit filter whose state lives in the query params, like `choiceFilter`: the dropdown holds the
 * unit tree and one unit matches exactly. The column `key` must be the name of the param.
 */
export function unitFilter(key: string, current: string | undefined) {
  return {
    key,
    filteredValue: current === undefined ? null : [current],
    filterMultiple: false,
    filterDropdown: (props: FilterDropdownProps) => (
      <UnitTreeFilter {...props} />
    ),
  };
}
