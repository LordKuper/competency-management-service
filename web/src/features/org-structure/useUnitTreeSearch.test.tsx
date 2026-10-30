import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useUnitTreeSearch } from "./useUnitTreeSearch";

interface Node {
  value: string;
  title: string;
  disabled?: boolean;
  children: Node[];
}

const node = (
  value: string,
  title: string,
  children: Node[] = [],
  disabled = false,
): Node => ({ value, title, disabled, children });

const TREE = [
  node("company", "Компания", [
    node("sales", "Продажи", [
      node("retail", "Розница"),
      node("wholesale", "Опт", [], true),
    ]),
    node("support", "Поддержка"),
  ]),
];

function search(text: string) {
  const hook = renderHook(() => useUnitTreeSearch(TREE));
  act(() => hook.result.current.showSearch.onSearch(text));
  return hook.result.current;
}

const valuesOf = (nodes: Node[]): string[] =>
  nodes.flatMap((item) => [item.value, ...valuesOf(item.children)]);

describe("search of a unit tree select", () => {
  it("leaves the tree and its expansion to the select while nothing is typed", () => {
    const { result } = renderHook(() => useUnitTreeSearch(TREE));

    expect(result.current.treeData).toBe(TREE);
    expect(result.current.treeExpandedKeys).toBeUndefined();
    expect(result.current.showSearch.searchValue).toBe("");
  });

  it("keeps a match with the units above it, and opens that path, whatever the case of the text", () => {
    const found = search("РОЗН");

    expect(valuesOf(found.treeData)).toEqual(["company", "sales", "retail"]);
    expect(found.treeExpandedKeys).toEqual(["company", "sales", "retail"]);
    expect(found.showSearch.searchValue).toBe("РОЗН");
  });

  it("keeps everything below a unit whose title matches, a unit that cannot be chosen included", () => {
    const found = search("прод");

    expect(valuesOf(found.treeData)).toEqual([
      "company",
      "sales",
      "retail",
      "wholesale",
    ]);
    expect(found.treeData[0]?.children[0]?.children[1]?.disabled).toBe(true);
  });

  it("offers nothing when no title matches", () => {
    expect(search("нет такого").treeData).toEqual([]);
  });

  it("gives the tree back when the text is cleared", () => {
    const hook = renderHook(() => useUnitTreeSearch(TREE));
    act(() => hook.result.current.showSearch.onSearch("розн"));

    act(() => hook.result.current.showSearch.onSearch(""));

    expect(hook.result.current.treeData).toBe(TREE);
    expect(hook.result.current.treeExpandedKeys).toBeUndefined();
  });
});
