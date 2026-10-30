import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { OrgUnitSelect } from "./OrgUnitSelect";

const unit = (
  id: string,
  name: string,
  parentId: string | null,
  isActive = true,
) => ({
  id,
  name,
  parentId,
  headEmployeeId: null,
  isActive,
  version: 1,
});

const TREE = [
  unit("company", "Компания", null),
  unit("sales", "Продажи", "company"),
  unit("support", "Поддержка", "company"),
  unit("retail", "Розница", "sales"),
  unit("archive", "Архив розницы", "company", false),
];

const TITLE = { selector: ".ant-select-tree-title" };

async function openSelect(isActiveRequired = false) {
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json(TREE));
  const onChange = vi.fn();
  render(
    <Providers>
      <OrgUnitSelect
        id="unit"
        onChange={onChange}
        isActiveRequired={isActiveRequired}
      />
    </Providers>,
  );
  const user = userEvent.setup();
  await user.click(screen.getByRole("combobox"));
  await screen.findByText("Компания", TITLE);
  return { onChange, user };
}

beforeEach(() => {
  queryClient.clear();
});

describe("unit select search", { timeout: LONG_FLOW_TIMEOUT }, () => {
  it("opens the way to a unit found by its name, which is closed otherwise", async () => {
    const { user } = await openSelect();
    expect(screen.queryByText("Розница", TITLE)).not.toBeInTheDocument();

    await user.keyboard("розн");

    expect(await screen.findByText("Розница", TITLE)).toBeInTheDocument();
    expect(screen.getByText("Продажи", TITLE)).toBeInTheDocument();
    expect(screen.queryByText("Поддержка", TITLE)).not.toBeInTheDocument();
  });

  it("chooses a unit found by its name", async () => {
    const { onChange, user } = await openSelect();
    await user.keyboard("розн");
    await screen.findByText("Розница", TITLE);

    await user.click(screen.getByText("Розница", TITLE));

    expect(onChange).toHaveBeenCalledWith(
      "retail",
      expect.anything(),
      expect.anything(),
    );
  });

  it("lists an inactive unit found by the search but does not let it be chosen where a working one is required", async () => {
    const { onChange, user } = await openSelect(true);
    await user.keyboard("архив");

    await user.click(
      await screen.findByText("Архив розницы (неактивно)", TITLE),
    );

    expect(onChange).not.toHaveBeenCalled();
  });
});
