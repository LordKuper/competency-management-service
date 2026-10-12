import { describe, expect, it, vi } from "vitest";
import { choiceFilter, onFilterChange } from "./columnFilters";

describe("column filters of the career tables (AC-24)", () => {
  it("shows the filter value the server was asked for and one value at a time", () => {
    const options = [{ text: "Действует", value: "Active" }];

    const chosen = choiceFilter("status", "Active", options);
    const none = choiceFilter("status", undefined, options);

    expect(chosen).toMatchObject({
      key: "status",
      filters: options,
      filteredValue: ["Active"],
      filterMultiple: false,
    });
    expect(none.filteredValue).toBeNull();
  });

  it("keeps a boolean filter value as a boolean", () => {
    expect(choiceFilter("isDefault", false, []).filteredValue).toEqual([false]);
  });

  it("copies a changed filter into the query params under the column key and goes back to the first page", () => {
    const setParams = vi.fn();
    const change = onFilterChange<{
      page: number;
      q: string;
      status?: string;
      isDefault?: boolean;
    }>(setParams);

    change(null, { status: ["Active"], isDefault: null }, null, {
      action: "filter",
    });

    const update = setParams.mock.calls[0]?.[0] as (previous: object) => object;
    expect(
      update({ page: 3, q: "навык", status: "Draft", isDefault: true }),
    ).toEqual({
      page: 1,
      q: "навык",
      status: "Active",
      isDefault: undefined,
    });
  });

  it("leaves paging and sorting events to the table's own handlers", () => {
    const setParams = vi.fn();
    const change = onFilterChange<{ page: number }>(setParams);

    change(null, { status: ["Active"] }, null, { action: "paginate" });
    change(null, { status: ["Active"] }, null, { action: "sort" });

    expect(setParams).not.toHaveBeenCalled();
  });

  it("does not add a page to the params of a table that has none", () => {
    const setParams = vi.fn();
    const change = onFilterChange<{ kind?: string }>(setParams);

    change(null, { kind: ["Track"] }, null, { action: "filter" });

    const update = setParams.mock.calls[0]?.[0] as (previous: object) => object;
    expect(update({})).toEqual({ kind: "Track" });
  });
});
