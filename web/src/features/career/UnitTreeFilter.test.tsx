import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { FilterDropdownProps } from "antd/es/table/interface";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { unitFilter } from "./UnitTreeFilter";

const unit = (id: string, name: string, parentId: string | null) => ({
  id,
  name,
  parentId,
  headEmployeeId: null,
  isActive: true,
  version: 1,
});

const TREE = [
  unit("company", "Компания", null),
  unit("sales", "Продажи", "company"),
  unit("support", "Поддержка", "company"),
  unit("retail", "Розница", "sales"),
];

function openDropdown(selected?: string) {
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json(TREE));
  const setSelectedKeys = vi.fn();
  const confirm = vi.fn();
  const column = unitFilter("orgUnitId", selected);
  render(
    <Providers>
      {column.filterDropdown({
        selectedKeys: selected ? [selected] : [],
        setSelectedKeys,
        confirm,
      } as unknown as FilterDropdownProps)}
    </Providers>,
  );
  return { setSelectedKeys, confirm, user: userEvent.setup() };
}

beforeEach(() => {
  queryClient.clear();
});

describe("unit filter of the career tables (AC-24)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("shows the filter value the server was asked for, one unit at a time, under the name of the param", () => {
    expect(unitFilter("orgUnitId", undefined)).toMatchObject({
      key: "orgUnitId",
      filteredValue: null,
      filterMultiple: false,
    });
    expect(unitFilter("orgUnitId", "sales").filteredValue).toEqual(["sales"]);
  });

  it("offers the units as a tree, children under their parents, expanded", async () => {
    const { user } = openDropdown();

    await user.click(screen.getByRole("combobox", { name: "Подразделение" }));

    const names = await screen.findAllByText(
      /^(Компания|Продажи|Поддержка|Розница)$/,
      {
        selector: ".ant-select-tree-title",
      },
    );
    expect(names.map((name) => name.textContent)).toEqual([
      "Компания",
      "Продажи",
      "Розница",
      "Поддержка",
    ]);
  });

  it("applies the filter at once with the unit that is chosen", async () => {
    const { setSelectedKeys, confirm, user } = openDropdown();
    await user.click(screen.getByRole("combobox", { name: "Подразделение" }));

    await user.click(
      await screen.findByText("Розница", {
        selector: ".ant-select-tree-title",
      }),
    );

    expect(setSelectedKeys).toHaveBeenCalledWith(["retail"]);
    expect(confirm).toHaveBeenCalledOnce();
  });

  it("finds a unit by a part of its name", async () => {
    const { user } = openDropdown();
    await user.click(screen.getByRole("combobox", { name: "Подразделение" }));
    await screen.findByText("Поддержка", {
      selector: ".ant-select-tree-title",
    });

    await user.keyboard("розн");

    await waitFor(() =>
      expect(
        screen.queryByText("Поддержка", { selector: ".ant-select-tree-title" }),
      ).not.toBeInTheDocument(),
    );
    expect(
      screen.getByText("Розница", { selector: ".ant-select-tree-title" }),
    ).toBeInTheDocument();
  });

  it("removes the filter and applies that when the chosen unit is cleared", async () => {
    const { setSelectedKeys, confirm, user } = openDropdown("sales");
    const field = await screen.findByRole("combobox", {
      name: "Подразделение",
    });
    await waitFor(() =>
      expect(
        within(field.closest(".ant-select") as HTMLElement).getByText(
          "Продажи",
        ),
      ).toBeInTheDocument(),
    );

    await user.hover(field.closest(".ant-select") as HTMLElement);
    await user.click(
      document.querySelector(".ant-select-clear") as HTMLElement,
    );

    expect(setSelectedKeys).toHaveBeenCalledWith([]);
    expect(confirm).toHaveBeenCalledOnce();
  });
});
