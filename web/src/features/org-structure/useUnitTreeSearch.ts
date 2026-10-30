import { theme } from "antd";
import { useDeferredValue, useMemo, useState } from "react";

/** Popup width, in control heights; the global popup max-width still caps it. */
const POPUP_WIDTH_IN_CONTROLS = 20;

interface SearchableNode<TNode> {
  value: string;
  title: string;
  children: TNode[];
}

/** The nodes whose title contains the needle (lower-cased) with everything below them, and the nodes above such a match. */
function keep<TNode extends SearchableNode<TNode>>(
  nodes: readonly TNode[],
  needle: string,
): TNode[] {
  const kept: TNode[] = [];
  for (const node of nodes) {
    if (node.title.toLowerCase().includes(needle)) {
      kept.push(node);
      continue;
    }
    const children = keep(node.children, needle);
    if (children.length > 0) kept.push({ ...node, children });
  }
  return kept;
}

function keysOf<TNode extends SearchableNode<TNode>>(
  nodes: readonly TNode[],
  keys: string[] = [],
): string[] {
  for (const node of nodes) {
    keys.push(node.value);
    keysOf(node.children, keys);
  }
  return keys;
}

/**
 * Props that make a unit TreeSelect filter its own tree. rc-tree-select would filter and re-expand the whole tree
 * synchronously on every keystroke; here the typed text shows at once and the tree follows from a deferred render.
 * The popup gets a fixed width, because the app-wide `popupMatchSelectWidth={false}` would make the list non-virtual.
 * `treeNodeFilterProp` stays so the tree still marks matches although `filterTreeNode` is off; while searching the expansion is
 * controlled so every unit of the found tree is open.
 */
export function useUnitTreeSearch<TNode extends SearchableNode<TNode>>(
  treeData: TNode[],
) {
  const { token } = theme.useToken();
  const [text, setText] = useState("");
  const needle = useDeferredValue(text.toLowerCase());
  const { shown, expandedKeys } = useMemo(() => {
    if (!needle) return { shown: treeData, expandedKeys: [] };
    const found = keep(treeData, needle);
    return { shown: found, expandedKeys: keysOf(found) };
  }, [treeData, needle]);
  return {
    treeData: shown,
    popupMatchSelectWidth: token.controlHeight * POPUP_WIDTH_IN_CONTROLS,
    showSearch: {
      searchValue: text,
      onSearch: setText,
      filterTreeNode: false,
      treeNodeFilterProp: "title",
    },
    treeExpandedKeys: text ? expandedKeys : undefined,
  };
}
