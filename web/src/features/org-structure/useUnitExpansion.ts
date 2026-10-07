import { useCallback, useMemo, useState } from "react";
import {
  opennessByUnit,
  searchUnitTree,
  type UnitNode,
  type UnitOpenness,
  type UnitSearch,
} from "./orgTree";
import { type EmployeeSearch, useEmployeeSearch } from "./useEmployeeSearch";

interface ExpansionState {
  text: string;
  /** Units the user opened while not searching. */
  browsed: ReadonlySet<string>;
  /** Units the user opened (true) or closed (false) since the search text last changed, whatever the search opened itself. */
  chosen: ReadonlyMap<string, boolean>;
}

/** Which units are open and what the search shows; clearing the search brings back the units that were open before it. */
export interface UnitExpansion {
  text: string;
  /** Null when the search text is blank. */
  search: UnitSearch | null;
  /** The server side of the search, whose matches `search` has merged in. */
  employeeSearch: EmployeeSearch;
  /** How much each open unit lists; a unit absent from the map is closed. */
  openness: ReadonlyMap<string, UnitOpenness>;
  setText: (text: string) => void;
  /** Opens or closes a unit; the choice is absolute, so a search answer arriving later cannot reverse it. */
  toggle: (unitId: string, isOpen: boolean) => void;
  /** Opens the given units outside of a search. */
  open: (unitIds: readonly string[]) => void;
}

function withMembership(
  ids: ReadonlySet<string>,
  id: string,
  isMember: boolean,
) {
  const next = new Set(ids);
  if (isMember) next.add(id);
  else next.delete(id);
  return next;
}

/** Keeps the open units of the hierarchy and the search over it: by unit name here, by employee on the server. */
export function useUnitExpansion(roots: readonly UnitNode[]): UnitExpansion {
  const [state, setState] = useState<ExpansionState>({
    text: "",
    browsed: new Set(),
    chosen: new Map(),
  });
  const { text } = state;
  const employeeSearch = useEmployeeSearch(text);
  const { employees } = employeeSearch;
  const search = useMemo(
    () => (text.trim() === "" ? null : searchUnitTree(roots, text, employees)),
    [roots, text, employees],
  );
  const openness = useMemo(
    () => opennessByUnit(search, state.browsed, state.chosen),
    [search, state.browsed, state.chosen],
  );

  const setText = useCallback(
    (next: string) =>
      setState((previous) => ({
        ...previous,
        text: next,
        chosen: new Map(),
      })),
    [],
  );
  const toggle = useCallback(
    (unitId: string, isOpen: boolean) =>
      setState((previous) =>
        previous.text.trim() === ""
          ? {
              ...previous,
              browsed: withMembership(previous.browsed, unitId, isOpen),
            }
          : {
              ...previous,
              chosen: new Map(previous.chosen).set(unitId, isOpen),
            },
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

  return { text, search, employeeSearch, openness, setText, toggle, open };
}
