import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { CompetencyModal } from "./CompetencyModal";
import type { Competency } from "./catalogApi";

const page = (items: object[]) => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 200,
});

const BASE_SCALE = {
  id: "base",
  name: "Базовая 0–4",
  points: [],
  isDefault: true,
  status: "Active",
  version: 1,
};
const OTHER_SCALE = {
  ...BASE_SCALE,
  id: "other",
  name: "Расширенная",
  isDefault: false,
};

const CATEGORY = {
  id: "category",
  name: "Профессиональные",
  status: "Active",
  version: 1,
};

const EXISTING: Competency = {
  id: "competency",
  name: "Проектирование",
  description: null,
  categoryId: CATEGORY.id,
  directionId: null,
  type: "Profile",
  scaleId: OTHER_SCALE.id,
  status: "Active",
  createdAt: "2026-10-01T00:00:00Z",
  versionNumber: 2,
  version: 7,
  canEdit: true,
  canDelete: true,
};

function serve() {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({
      id: "account",
      email: "admin@test.local",
      role: "GlobalAdmin",
    }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/career/categories", () =>
    Response.json(page([CATEGORY])),
  );
  fakeApi.on("GET /api/v1/career/directions", () => Response.json(page([])));
  fakeApi.on("GET /api/v1/career/scales", () =>
    Response.json(page([BASE_SCALE, OTHER_SCALE])),
  );
}

function open(competency?: Competency, override: () => void = () => undefined) {
  serve();
  override();
  render(
    <Providers>
      <CompetencyModal competency={competency} onClose={() => undefined} />
    </Providers>,
  );
  return userEvent.setup();
}

async function choose(
  user: ReturnType<typeof userEvent.setup>,
  field: string,
  option: string,
) {
  await user.click(screen.getByRole("combobox", { name: field }));
  await user.click(
    await screen.findByText(option, {
      selector: ".ant-select-item-option-content",
    }),
  );
}

async function fillRequiredFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(await screen.findByLabelText("Название"), "Проектирование");
  await choose(user, "Категория", CATEGORY.name);
  await choose(user, "Тип", "Профильная");
}

beforeEach(() => {
  queryClient.clear();
});

describe("scale of a new competency (AC-22)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("starts with the default scale, which a save sends", async () => {
    fakeApi.on("POST /api/v1/career/competencies", () =>
      Response.json({ ...EXISTING, version: 1 }),
    );
    const user = open();
    await fillRequiredFields(user);
    const scale = screen.getByRole("combobox", { name: "Шкала" });
    await waitFor(() =>
      expect(
        within(scale.closest(".ant-select") as HTMLElement).getByText(
          BASE_SCALE.name,
        ),
      ).toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() =>
      expect(fakeApi.calls("POST /api/v1/career/competencies")).toHaveLength(1),
    );
    expect(
      await fakeApi.calls("POST /api/v1/career/competencies")[0]?.json(),
    ).toMatchObject({ scaleId: BASE_SCALE.id, type: "Profile" });
  });

  it("keeps the scale the user picks instead of the default", async () => {
    fakeApi.on("POST /api/v1/career/competencies", () =>
      Response.json({ ...EXISTING, version: 1 }),
    );
    const user = open();
    await fillRequiredFields(user);
    await waitFor(() =>
      expect(
        within(
          screen
            .getByRole("combobox", { name: "Шкала" })
            .closest(".ant-select") as HTMLElement,
        ).getByText(BASE_SCALE.name),
      ).toBeInTheDocument(),
    );

    await choose(user, "Шкала", OTHER_SCALE.name);
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() =>
      expect(fakeApi.calls("POST /api/v1/career/competencies")).toHaveLength(1),
    );
    expect(
      await fakeApi.calls("POST /api/v1/career/competencies")[0]?.json(),
    ).toMatchObject({ scaleId: OTHER_SCALE.id });
  });

  it("asks the server for the default scale, and starts with it even when it is not the first scale on the list", async () => {
    const user = open(undefined, () =>
      fakeApi.on("GET /api/v1/career/scales", (request) =>
        Response.json(
          page(
            new URL(request.url).searchParams.get("isDefault") === "true"
              ? [BASE_SCALE]
              : [OTHER_SCALE, BASE_SCALE],
          ),
        ),
      ),
    );
    await fillRequiredFields(user);

    await waitFor(() =>
      expect(
        within(
          screen
            .getByRole("combobox", { name: "Шкала" })
            .closest(".ant-select") as HTMLElement,
        ).getByText(BASE_SCALE.name),
      ).toBeInTheDocument(),
    );
  });

  it("shows by name the direction of an existing competency that the first page of directions does not hold", async () => {
    open({ ...EXISTING, directionId: "far" }, () =>
      fakeApi.on("GET /api/v1/career/directions/far", () =>
        Response.json({
          id: "far",
          name: "Дальнее направление",
          status: "Inactive",
          version: 1,
          canEdit: false,
          canDelete: false,
        }),
      ),
    );
    await screen.findByDisplayValue("Проектирование");

    await waitFor(() =>
      expect(
        within(
          screen
            .getByRole("combobox", { name: "Направление" })
            .closest(".ant-select") as HTMLElement,
        ).getByText("Дальнее направление"),
      ).toBeInTheDocument(),
    );
  });

  it("leaves the scale of an existing competency alone and saves against its version", async () => {
    fakeApi.on("PUT /api/v1/career/competencies/competency", () =>
      Response.json({ ...EXISTING, version: 8, versionNumber: 3 }),
    );
    const user = open(EXISTING);
    await screen.findByDisplayValue("Проектирование");
    await waitFor(() =>
      expect(
        within(
          screen
            .getByRole("combobox", { name: "Шкала" })
            .closest(".ant-select") as HTMLElement,
        ).getByText(OTHER_SCALE.name),
      ).toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() =>
      expect(
        fakeApi.calls("PUT /api/v1/career/competencies/competency"),
      ).toHaveLength(1),
    );
    const call = fakeApi.calls("PUT /api/v1/career/competencies/competency")[0];
    expect(call?.headers.get("If-Match")).toBe('"7"');
    expect(await call?.json()).toMatchObject({ scaleId: OTHER_SCALE.id });
  });
});
