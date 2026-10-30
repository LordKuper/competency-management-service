import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { ApprovalsSection } from "./ApprovalsSection";
import { BlocksTab } from "./BlocksTab";
import { CompetenciesTab } from "./CompetenciesTab";
import { DirectionsTracksTab } from "./DirectionsTracksTab";
import { DraftsSection } from "./DraftsSection";
import { MatricesTab } from "./MatricesTab";
import { ScalesTab } from "./ScalesTab";

const page = (items: object[]) => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 200,
});

const entry = (id: string, name: string) => ({
  id,
  name,
  status: "Active",
  version: 1,
});

const unit = (id: string, name: string, parentId: string | null) => ({
  id,
  name,
  parentId,
  headEmployeeId: null,
  isActive: true,
  version: 1,
});

const DIRECTION = entry("direction", "Разработка");
const SCALE = {
  ...entry("scale", "Базовая 0–4"),
  points: [],
  isDefault: true,
};
const TRACK = {
  ...entry("track", "Бэкенд"),
  directionId: DIRECTION.id,
  ownerOrgUnitId: "company",
  ownerOrgUnitName: "Компания",
};

function serve() {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({
      id: "account",
      email: "admin@test.local",
      roles: ["GlobalAdmin"],
    }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], matrixUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () =>
    Response.json([
      unit("company", "Компания", null),
      unit("sales", "Продажи", "company"),
    ]),
  );
  fakeApi.on("GET /api/v1/career/directions", () =>
    Response.json(page([DIRECTION])),
  );
  fakeApi.on("GET /api/v1/career/scales", () => Response.json(page([SCALE])));
  fakeApi.on("GET /api/v1/career/tracks", () => Response.json(page([TRACK])));
  fakeApi.on("GET /api/v1/career/competencies", () => Response.json(page([])));
  fakeApi.on("GET /api/v1/career/matrices", () => Response.json(page([])));
  fakeApi.on("GET /api/v1/career/blocks", () => Response.json(page([])));
  fakeApi.on("GET /api/v1/career/approvals", () => Response.json([]));
  fakeApi.on("GET /api/v1/career/drafts", () => Response.json([]));
}

interface FilterCase {
  table: string;
  show: () => ReactElement;
  route: string;
  column: string;
  /** Accessible name of the control inside the filter when it differs from the column title. */
  control?: string;
  choose:
    | { option: string }
    | { entry: string }
    | { unit: string }
    | { checkbox: string };
  param: string;
  value: string;
  inNestedTable?: boolean;
}

const CASES: FilterCase[] = [
  {
    table: "scales by status",
    show: () => <ScalesTab />,
    route: "GET /api/v1/career/scales",
    column: "Статус",
    choose: { option: "Черновик" },
    param: "status",
    value: "Draft",
  },
  {
    table: "scales by the default flag",
    show: () => <ScalesTab />,
    route: "GET /api/v1/career/scales",
    column: "По умолчанию",
    choose: { option: "По умолчанию" },
    param: "isDefault",
    value: "true",
  },
  {
    table: "directions by status",
    show: () => <DirectionsTracksTab />,
    route: "GET /api/v1/career/directions",
    column: "Статус",
    choose: { option: "Действует" },
    param: "status",
    value: "Active",
  },
  {
    table: "tracks by status",
    show: () => <DirectionsTracksTab />,
    route: "GET /api/v1/career/tracks",
    column: "Статус",
    choose: { option: "Неактивна" },
    param: "status",
    value: "Inactive",
    inNestedTable: true,
  },
  {
    table: "tracks by unit",
    show: () => <DirectionsTracksTab />,
    route: "GET /api/v1/career/tracks",
    column: "Владелец",
    choose: { unit: "Продажи" },
    param: "ownerOrgUnitId",
    value: "sales",
    inNestedTable: true,
  },
  {
    table: "competencies by direction",
    show: () => <CompetenciesTab />,
    route: "GET /api/v1/career/competencies",
    column: "Область",
    choose: { entry: DIRECTION.name },
    param: "directionId",
    value: DIRECTION.id,
  },
  {
    table: "competencies by scale",
    show: () => <CompetenciesTab />,
    route: "GET /api/v1/career/competencies",
    column: "Шкала",
    choose: { entry: SCALE.name },
    param: "scaleId",
    value: SCALE.id,
  },
  {
    table: "competencies by status",
    show: () => <CompetenciesTab />,
    route: "GET /api/v1/career/competencies",
    column: "Статус",
    choose: { option: "Действует" },
    param: "status",
    value: "Active",
  },
  {
    table: "blocks by unit",
    show: () => <BlocksTab />,
    route: "GET /api/v1/career/blocks",
    column: "Действует для",
    choose: { unit: "Компания" },
    param: "scopeUnitId",
    value: "company",
  },
  {
    table: "blocks for every unit",
    show: () => <BlocksTab />,
    route: "GET /api/v1/career/blocks",
    column: "Действует для",
    choose: { checkbox: "Для всех" },
    param: "forAll",
    value: "true",
  },
  {
    table: "matrices by unit",
    show: () => <MatricesTab />,
    route: "GET /api/v1/career/matrices",
    column: "Подразделение",
    choose: { unit: "Продажи" },
    param: "orgUnitId",
    value: "sales",
  },
  {
    table: "matrices by direction",
    show: () => <MatricesTab />,
    route: "GET /api/v1/career/matrices",
    column: "Область",
    choose: { entry: DIRECTION.name },
    param: "directionId",
    value: DIRECTION.id,
  },
  {
    table: "matrices by track",
    show: () => <MatricesTab />,
    route: "GET /api/v1/career/matrices",
    column: "Трек или должность",
    control: "Трек",
    choose: { entry: TRACK.name },
    param: "trackId",
    value: TRACK.id,
  },
  {
    table: "versions waiting for approval by unit",
    show: () => <ApprovalsSection />,
    route: "GET /api/v1/career/approvals",
    column: "Подразделение",
    choose: { unit: "Продажи" },
    param: "orgUnitId",
    value: "sales",
  },
  {
    table: "versions waiting for approval by step",
    show: () => <ApprovalsSection />,
    route: "GET /api/v1/career/approvals",
    column: "Шаг",
    choose: { option: "HR" },
    param: "step",
    value: "Hr",
  },
  {
    table: "drafts by kind",
    show: () => <DraftsSection />,
    route: "GET /api/v1/career/drafts",
    column: "Тип",
    choose: { option: "Трек" },
    param: "kind",
    value: "Track",
  },
  {
    table: "drafts by unit",
    show: () => <DraftsSection />,
    route: "GET /api/v1/career/drafts",
    column: "Подразделение",
    choose: { unit: "Продажи" },
    param: "orgUnitId",
    value: "sales",
  },
];

const queries = (route: string) =>
  fakeApi
    .calls(route)
    .map((call) => Object.fromEntries(new URL(call.url).searchParams));

async function tableSettled() {
  await waitFor(() => {
    const blocked = Array.from(
      document.querySelectorAll(".ant-spin-container"),
    ).filter(
      (container) => getComputedStyle(container).pointerEvents === "none",
    );
    expect(blocked).toHaveLength(0);
  });
}

function headerOf(scope: HTMLElement, column: string) {
  const header = within(scope)
    .getAllByRole("columnheader")
    .find((cell) => cell.textContent?.trim() === column);
  if (!header) throw new Error(`No column «${column}»`);
  return header;
}

beforeEach(() => {
  queryClient.clear();
  serve();
});

describe("column filters send their value under the param the server reads (AC-24)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it.each(CASES)("$table", async (filter) => {
    render(
      <Providers>
        <MemoryRouter>{filter.show()}</MemoryRouter>
      </Providers>,
    );
    const user = userEvent.setup();
    let scope: HTMLElement = document.body;
    if (filter.inNestedTable) {
      await user.click(
        await waitFor(() => {
          const expander = document.querySelector(".ant-table-row-expand-icon");
          expect(expander).not.toBeNull();
          return expander as HTMLElement;
        }),
      );
      await screen.findByText(TRACK.name);
      scope = document.querySelector(
        ".ant-table-expanded-row .ant-table-wrapper",
      ) as HTMLElement;
    } else {
      await waitFor(() =>
        expect(queries(filter.route).length).toBeGreaterThan(0),
      );
    }
    await tableSettled();
    const asked = queries(filter.route).length;
    expect(queries(filter.route)[0]).not.toHaveProperty(filter.param);

    await user.click(
      headerOf(scope, filter.column).querySelector(
        ".ant-table-filter-trigger",
      ) as HTMLElement,
    );
    if ("option" in filter.choose) {
      await user.click(
        await screen.findByRole("menuitem", { name: filter.choose.option }),
      );
      await user.click(screen.getByRole("button", { name: "OK" }));
    } else if ("checkbox" in filter.choose) {
      await user.click(
        screen.getByRole("checkbox", { name: filter.choose.checkbox }),
      );
    } else if ("entry" in filter.choose) {
      await user.click(
        screen.getByRole("combobox", { name: filter.control ?? filter.column }),
      );
      await user.click(
        await screen.findByText(filter.choose.entry, {
          selector: ".ant-select-item-option-content",
        }),
      );
    } else {
      await user.click(
        within(
          document.querySelector(".ant-table-filter-dropdown") as HTMLElement,
        ).getByRole("combobox"),
      );
      await user.click(
        await screen.findByText(filter.choose.unit, {
          selector: ".ant-select-tree-title",
        }),
      );
    }

    await waitFor(() => {
      const sent = queries(filter.route);
      expect(sent.length).toBeGreaterThan(asked);
      expect(sent.at(-1)).toMatchObject({ [filter.param]: filter.value });
    });
  });
});
