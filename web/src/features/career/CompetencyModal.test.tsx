import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { deferred, fakeApi, problem } from "../../test/fakeApi";
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

const EXISTING: Competency = {
  id: "competency",
  name: "Проектирование",
  description: null,
  directionId: null,
  directionName: null,
  scaleId: OTHER_SCALE.id,
  scaleName: OTHER_SCALE.name,
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
      roles: ["GlobalAdmin"],
    }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [] }),
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
      <MemoryRouter>
        <CompetencyModal competency={competency} onClose={() => undefined} />
      </MemoryRouter>
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
      selector: ".ant-select-item-option-content *",
    }),
  );
}

async function fillRequiredFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(await screen.findByLabelText("Название"), "Проектирование");
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
    ).toMatchObject({ scaleId: BASE_SCALE.id });
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
            .getByRole("combobox", { name: "Область" })
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

describe("competency form while the competency is being saved (AC-61)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  const ROUTE = "PUT /api/v1/career/competencies/competency";

  function lockedControls() {
    const controls: [string, HTMLElement][] = [
      ["name", screen.getByLabelText("Название")],
      ["description", screen.getByLabelText("Описание")],
      ["evidence", screen.getByLabelText("Примеры подтверждений")],
      ["direction", screen.getByRole("combobox", { name: "Область" })],
      ["scale", screen.getByRole("combobox", { name: "Шкала" })],
    ];
    return controls
      .filter(([, control]) => control.hasAttribute("disabled"))
      .map(([kind]) => kind);
  }

  it("holds every field while the save is in flight, and frees them when it is refused", async () => {
    const held = deferred<Response>();
    const user = open(EXISTING, () => fakeApi.on(ROUTE, () => held.promise));
    await screen.findByDisplayValue("Проектирование");
    expect(lockedControls()).toEqual([]);

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(fakeApi.calls(ROUTE)).toHaveLength(1));
    expect(lockedControls()).toEqual([
      "name",
      "description",
      "evidence",
      "direction",
      "scale",
    ]);
    held.resolve(problem(500, { title: "Сбой" }));
    expect(
      await screen.findByText("Не удалось сохранить запись"),
    ).toBeInTheDocument();
    expect(lockedControls()).toEqual([]);
  });

  it("moves the focus to the first field the server rejects, once the form is enabled again", async () => {
    const held = deferred<Response>();
    const user = open(EXISTING, () => fakeApi.on(ROUTE, () => held.promise));
    await screen.findByDisplayValue("Проектирование");

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    await waitFor(() =>
      expect(screen.getByLabelText("Примеры подтверждений")).toBeDisabled(),
    );
    held.resolve(
      problem(400, { errors: { evidenceExamples: ["Слишком длинно"] } }),
    );
    expect(await screen.findByText("Слишком длинно")).toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByLabelText("Примеры подтверждений")).toHaveFocus(),
    );
  });
});
