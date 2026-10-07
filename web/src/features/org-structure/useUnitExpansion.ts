import { useCallback, useMemo, useState } from "react";
import { searchUnitTree, type UnitNode, type UnitSearch } from "./orgTree";
import { type EmployeeSearch, useEmployeeSearch } from "./useEmployeeSearch";

interface ExpansionState {
  text: string;
  /** Units the user opened while not searching. */
  browsed: ReadonlySet<string>;
  /** Units the user flipped, relative to the search's own openings, since the search text last changed. */
  flipped: ReadonlySet<string>;
}

/** Which units are open and what the search shows; clearing the search brings back the units that were open before it. */
export interface UnitExpansion {
  text: string;
  /** Null when the search text is blank. */
  search: UnitSearch | null;
  /** The server side of the search, whose matches `search` has merged in. */
  employeeSearch: EmployeeSearch;
  openIds: ReadonlySet<string>;
  setText: (text: string) => void;
  toggle: (unitId: string) => void;
  /** Opens the given units outside of a search. */
  open: (unitIds: readonly string[]) => void;
}

function flip(ids: ReadonlySet<string>, flippedIds: Iterable<string>) {
  const next = new Set(ids);
  for (const id of flippedIds) if (!next.delete(id)) next.add(id);
  return next;
}

/** Keeps the open units of the hierarchy and the search over it: by unit name here, by employee on the server. */
export function useUnitExpansion(roots: readonly UnitNode[]): UnitExpansion {
  const [state, setState] = useState<ExpansionState>({
    text: "",
    browsed: new Set(),
    flipped: new Set(),
  });
  const { text } = state;
  const employeeSearch = useEmployeeSearch(text);
  const { employees } = employeeSearch;
  const search = useMemo(
    () => (text.trim() === "" ? null : searchUnitTree(roots, text, employees)),
    [roots, text, employees],
  );
  const openIds = useMemo(
    () => (search ? flip(search.pathIds, state.flipped) : state.browsed),
    [search, state.flipped, state.browsed],
  );

  const setText = useCallback(
    (next: string) =>
      setState((previous) => ({
        ...previous,
        text: next,
        flipped: new Set(),
      })),
    [],
  );
  const toggle = useCallback(
    (unitId: string) =>
      setState((previous) =>
        previous.text.trim() === ""
          ? { ...previous, browsed: flip(previous.browsed, [unitId]) }
          : { ...previous, flipped: flip(previous.flipped, [unitId]) },
      ),
    [],
  );
  const open = useCallback(
    (unitIds: readonly string[]) =>
      setState((previous) => ({
        ...previous,
        browsed: new Set([...previous.browsed, ...unitIds]),
      })),
    [],
  );

  return { text, search, employeeSearch, openIds, setText, toggle, open };
}
