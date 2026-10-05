import {
  theme as antdTheme,
  Card,
  Empty,
  Input,
  Space,
  Tree,
  type TreeDataNode,
  Typography,
} from "antd";
import { type Key, useEffect, useMemo, useState } from "react";
import type { OrgUnit } from "./orgStructureApi";
import {
  ancestorIds,
  buildUnitTree,
  searchUnits,
  type UnitNode,
} from "./orgTree";
import { UnitStatusTag } from "./StatusTags";

const TREE_VISIBLE_ROWS = 15;

interface UnitTreeNode extends TreeDataNode {
  unit: OrgUnit;
  children: UnitTreeNode[];
}

function toTreeNodes(nodes: readonly UnitNode[]): UnitTreeNode[] {
  return nodes.map(({ unit, children }) => ({
    key: unit.id,
    title: unit.name,
    unit,
    children: toTreeNodes(children),
  }));
}

function rootIds(units: readonly OrgUnit[]): string[] {
  return units.filter((unit) => unit.parentId === null).map((unit) => unit.id);
}

function UnitTitle({ unit }: { unit: OrgUnit }) {
  if (unit.isActive) return unit.name;
  return (
    <Space size="small">
      <Typography.Text type="secondary">{unit.name}</Typography.Text>
      <UnitStatusTag isActive={false} />
    </Space>
  );
}

interface UnitTreePanelProps {
  units: readonly OrgUnit[];
  /** Identifier of the unit shown in the card, if any. */
  selectedId: string | null;
  onSelect: (unitId: string) => void;
}

/**
 * Searchable tree of the units. The name search hides every unit that neither matches nor leads to a match;
 * the tree scrolls inside a fixed height so thousands of units stay cheap to render.
 */
export function UnitTreePanel({
  units,
  selectedId,
  onSelect,
}: UnitTreePanelProps) {
  const { token } = antdTheme.useToken();
  const [search, setSearch] = useState("");
  const [expanded, setExpanded] = useState<Key[]>(() => rootIds(units));
  const treeData = useMemo(
    () =>
      toTreeNodes(
        buildUnitTree(search ? searchUnits(units, search).visible : units),
      ),
    [units, search],
  );

  useEffect(() => {
    if (selectedId === null) return;
    setExpanded((previous) => [
      ...new Set([...previous, ...ancestorIds(units, selectedId)]),
    ]);
  }, [units, selectedId]);

  function applySearch(text: string) {
    const trimmed = text.trim();
    setSearch(trimmed);
    setExpanded(
      trimmed ? searchUnits(units, trimmed).expandedIds : rootIds(units),
    );
  }

  return (
    <Card title="Подразделения">
      <Space orientation="vertical" size="middle" style={{ display: "flex" }}>
        <Input.Search
          allowClear
          aria-label="Поиск подразделения по названию"
          placeholder="Название подразделения"
          onSearch={applySearch}
        />
        {treeData.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Подразделения не найдены. Измените запрос."
          />
        ) : (
          <Tree<UnitTreeNode>
            blockNode
            aria-label="Дерево подразделений"
            height={token.controlHeight * TREE_VISIBLE_ROWS}
            treeData={treeData}
            expandedKeys={expanded}
            onExpand={setExpanded}
            selectedKeys={selectedId ? [selectedId] : []}
            onSelect={([key]) => {
              if (key !== undefined) onSelect(String(key));
            }}
            titleRender={(node) => <UnitTitle unit={node.unit} />}
          />
        )}
      </Space>
    </Card>
  );
}
