import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { careerQueryKey } from "./careerApi";
import { MatrixCreateModal } from "./MatrixCreateModal";

const unit = (id: string, name: string, parentId: string | null) => ({
  id,
  name,
  parentId,
  headEmployeeId: null,
  isActive: true,
  version: 1,
});

const UNITS = [
  unit("root", "Компания", null),
  unit("department", "Департамент", "root"),
  unit("team", "Команда", "department"),
  unit("sibling", "Соседняя команда", "department"),
  unit("branch", "Филиал", "team"),
];

const track = (id: string, name: string, ownerOrgUnitId: string) => ({
  id,
  name,
  directionId: "direction",
  status: "Active",
  ownerOrgUnitId,
  ownerOrgUnitName: ownerOrgUnitId,
  version: 1,
});

const page = (items: object[]) => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 200,
});

const TRACKS = [
  track("t-root", "Трек компании", "root"),
  track("t-department", "Трек департамента", "department"),
  track("t-team", "Трек команды", "team"),
  track("t-sibling", "Трек соседей", "sibling"),
  track("t-branch", "Трек филиала", "branch"),
];

function serve() {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "account", email: "head@test.local", role: "User" }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({
      headedUnits: [
        { id: "team", name: "Команда" },
        { id: "department", name: "Департамент" },
      ],
      hrUnits: [],
    }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () => Response.json(UNITS));
  fakeApi.on("GET /api/v1/career/directions", () =>
    Response.json(
      page([
        { id: "direction", name: "Разработка", status: "Active", version: 1 },
      ]),
    ),
  );
  // The server offers the tracks of the unit in `forOrgUnitId` and of the units above it.
  fakeApi.on("GET /api/v1/career/tracks", (request) => {
    const forUnit = new URL(request.url).searchParams.get("forOrgUnitId");
    const chain = new Set<string>();
    for (
      let current = UNITS.find((u) => u.id === forUnit);
      current;
      current = UNITS.find((u) => u.id === current?.parentId)
    ) {
      chain.add(current.id);
    }
    return Response.json(
      page(TRACKS.filter((item) => chain.has(item.ownerOrgUnitId))),
    );
  });
  fakeApi.on("GET /api/v1/career/scales", () =>
    Response.json(
      page([
        {
          id: "scale",
          name: "Базовая 0–4",
          points: [],
          isDefault: true,
          status: "Active",
          version: 1,
        },
      ]),
    ),
  );
}

async function openModal(override: () => void = () => undefined) {
  serve();
  override();
  render(
    <Providers>
      <MemoryRouter initialEntries={["/career"]}>
        <Routes>
          <Route
            path="/career"
            element={<MatrixCreateModal onClose={() => undefined} />}
          />
          <Route path="/career/matrices/:id" element={<p>Открыта матрица</p>} />
        </Routes>
      </MemoryRouter>
    </Providers>,
  );
  await waitFor(() =>
    expect(queryClient.getQueryData([...careerQueryKey, "me"])).toBeDefined(),
  );
  await act(async () => undefined);
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

async function openTracks(user: ReturnType<typeof userEvent.setup>) {
  const combobox = screen.getByRole("combobox", { name: "Трек" });
  await user.click(combobox);
  const dropdown = await waitFor(() => {
    const list = document.getElementById(
      combobox.getAttribute("aria-controls") ?? "",
    );
    expect(list).not.toBeNull();
    return list?.closest(".ant-select-dropdown") as HTMLElement;
  });
  return Array.from(
    dropdown.querySelectorAll(".ant-select-item-option-content"),
  ).map((option) => option.textContent);
}

beforeEach(() => {
  queryClient.clear();
});

describe("starting a matrix (AC-14)", { timeout: LONG_FLOW_TIMEOUT }, () => {
  it("offers the units the user heads, and only the tracks of the chosen unit and of the units above it", async () => {
    const user = await openModal();
    await choose(user, "Подразделение", "Команда");
    await choose(user, "Направление", "Разработка");

    expect(await openTracks(user)).toEqual([
      "Трек компании",
      "Трек департамента",
      "Трек команды",
    ]);
    expect(
      Object.fromEntries(
        new URL(fakeApi.calls("GET /api/v1/career/tracks").at(-1)?.url ?? "")
          .searchParams,
      ),
    ).toMatchObject({
      forOrgUnitId: "team",
      directionId: "direction",
      inWork: "true",
    });
  });

  it("changes the tracks when another unit is chosen and clears the track chosen before", async () => {
    const user = await openModal();
    await choose(user, "Подразделение", "Команда");
    await choose(user, "Направление", "Разработка");
    await choose(user, "Трек", "Трек команды");

    await choose(user, "Подразделение", "Департамент");

    const track = screen.getByRole("combobox", { name: "Трек" });
    expect(
      within(track.closest(".ant-select") as HTMLElement).queryByText(
        "Трек команды",
      ),
    ).not.toBeInTheDocument();
    expect(await openTracks(user)).toEqual([
      "Трек компании",
      "Трек департамента",
    ]);
  });

  it("creates the matrix with the four choices and opens it", async () => {
    fakeApi.on("POST /api/v1/career/matrices", () =>
      Response.json({ id: "new-matrix" }),
    );
    const user = await openModal();
    await choose(user, "Подразделение", "Команда");
    await choose(user, "Направление", "Разработка");
    await choose(user, "Трек", "Трек департамента");
    await choose(user, "Шкала", "Базовая 0–4");

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    expect(await screen.findByText("Открыта матрица")).toBeInTheDocument();
    expect(
      await fakeApi.calls("POST /api/v1/career/matrices")[0]?.json(),
    ).toEqual({
      orgUnitId: "team",
      directionId: "direction",
      trackId: "t-department",
      scaleId: "scale",
    });
  });

  it("says under the track which choices it waits for, until the unit and the direction are chosen", async () => {
    const reason = "Сначала выберите подразделение и направление";
    const user = await openModal();
    expect(screen.getByText(reason)).toBeInTheDocument();

    await choose(user, "Подразделение", "Команда");
    expect(screen.getByText(reason)).toBeInTheDocument();

    await choose(user, "Направление", "Разработка");
    await waitFor(() =>
      expect(screen.queryByText(reason)).not.toBeInTheDocument(),
    );
  });

  it("finds a direction far past the first page by what is typed, since the search runs on the server", async () => {
    const directions = Array.from({ length: 250 }, (_, index) => ({
      id: `direction-${index + 1}`,
      name: `Направление ${String(index + 1).padStart(3, "0")}`,
      status: "Active",
      version: 1,
    }));
    const user = await openModal(() =>
      fakeApi.on("GET /api/v1/career/directions", (request) => {
        const query = new URL(request.url).searchParams;
        const text = (query.get("q") ?? "").toLowerCase();
        const number = Number(query.get("page"));
        const size = Number(query.get("pageSize"));
        const found = directions.filter((item) =>
          item.name.toLowerCase().includes(text),
        );
        return Response.json({
          items: found.slice((number - 1) * size, number * size),
          total: found.length,
          page: number,
          pageSize: size,
        });
      }),
    );
    const combobox = screen.getByRole("combobox", { name: "Направление" });

    await user.click(combobox);
    await user.keyboard("Направление 230");
    await user.click(
      await screen.findByText("Направление 230", {
        selector: ".ant-select-item-option-content",
      }),
    );

    expect(
      within(combobox.closest(".ant-select") as HTMLElement).getByText(
        "Направление 230",
      ),
    ).toBeInTheDocument();
    const asked = fakeApi
      .calls("GET /api/v1/career/directions")
      .map((call) => Object.fromEntries(new URL(call.url).searchParams));
    expect(asked[0]).toMatchObject({
      page: "1",
      pageSize: "20",
      inWork: "true",
    });
    expect(asked.at(-1)).toMatchObject({
      q: "Направление 230",
      page: "1",
      pageSize: "20",
      inWork: "true",
    });
  });

  it("shows under the track that the unit has a matrix for it already", async () => {
    fakeApi.on("POST /api/v1/career/matrices", () =>
      problem(409, {
        errors: {
          trackId: ["У подразделения уже есть матрица для этого трека"],
        },
      }),
    );
    const user = await openModal();
    await choose(user, "Подразделение", "Команда");
    await choose(user, "Направление", "Разработка");
    await choose(user, "Трек", "Трек команды");
    await choose(user, "Шкала", "Базовая 0–4");

    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    expect(
      await screen.findByText(
        "У подразделения уже есть матрица для этого трека",
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText("Открыта матрица")).not.toBeInTheDocument();
  });
});
