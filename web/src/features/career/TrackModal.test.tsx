import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { stubRowLayout } from "../../test/stubRowLayout";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { currentUserQueryKey } from "../auth/useCurrentUser";
import { careerQueryKey } from "./careerApi";
import type { CareerTrack } from "./catalogApi";
import { TrackModal } from "./TrackModal";

const DIRECTION_ID = "direction";
const SALES = "sales";
const TRACK_ROUTE = "POST /api/v1/career/tracks";
const LEVEL_ROUTE = "POST /api/v1/career/levels";

const unit = (id: string, name: string, parentId: string | null) => ({
  id,
  name,
  parentId,
  headEmployeeId: null,
  isActive: true,
  version: 1,
});

const track = (id: string, name: string, version: number): CareerTrack => ({
  id,
  name,
  directionId: DIRECTION_ID,
  status: "Active",
  ownerOrgUnitId: SALES,
  ownerOrgUnitName: "Продажи",
  version,
  canEdit: true,
  canDelete: true,
});

const savedLevel = (id: string, name: string, trackId: string) => ({
  id,
  name,
  directionId: DIRECTION_ID,
  trackId,
  position: 1,
  status: "Active",
  version: 1,
});

interface Served {
  steps: string[];
  levels: ReturnType<typeof savedLevel>[];
}

function serve(): Served {
  const served: Served = { steps: [], levels: [] };
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
  fakeApi.on("GET /api/v1/org-units/tree", () =>
    Response.json([unit(SALES, "Продажи", null)]),
  );
  fakeApi.on("GET /api/v1/career/levels", () =>
    Response.json({
      items: served.levels,
      total: served.levels.length,
      page: 1,
      pageSize: 200,
    }),
  );
  fakeApi.on(TRACK_ROUTE, async (request) => {
    const body = await request.json();
    served.steps.push(`track ${body.name} in ${body.ownerOrgUnitId}`);
    return Response.json(track("created", body.name, 1));
  });
  fakeApi.on(LEVEL_ROUTE, async (request) => {
    const body = await request.json();
    served.steps.push(`level ${body.name} of ${body.trackId}`);
    const created = savedLevel(
      `level-${served.levels.length + 1}`,
      body.name,
      body.trackId,
    );
    served.levels.push(created);
    return Response.json(created);
  });
  fakeApi.on(
    "PUT /api/v1/career/tracks/created/level-order",
    async (request) => {
      const body = await request.json();
      served.steps.push(`order ${body.levelIds.join(",")}`);
      return Response.json(track("created", "Бэкенд", 2));
    },
  );
  return served;
}

async function dialogTitled(title: string) {
  const [heading] = await screen.findAllByText(title, {
    selector: ".ant-modal-title",
  });
  return heading?.closest('[role="dialog"]') as HTMLElement;
}

async function addLevel(
  user: ReturnType<typeof userEvent.setup>,
  name: string,
) {
  await user.click(screen.getByRole("button", { name: "Добавить уровень" }));
  const dialog = await dialogTitled("Новый уровень");
  await user.type(within(dialog).getByLabelText("Название"), name);
  await user.click(within(dialog).getByRole("button", { name: "Сохранить" }));
  await waitFor(() =>
    expect(
      screen.queryByText("Новый уровень", { selector: ".ant-modal-title" }),
    ).not.toBeInTheDocument(),
  );
}

async function rightsLoaded() {
  await waitFor(() => {
    expect(queryClient.getQueryData(currentUserQueryKey)).toBeDefined();
    expect(queryClient.getQueryData([...careerQueryKey, "me"])).toBeDefined();
  });
  await act(async () => undefined);
}

async function fillNewTrack(
  user: ReturnType<typeof userEvent.setup>,
  name: string,
) {
  await rightsLoaded();
  await user.click(
    await screen.findByRole("combobox", { name: "Подразделение" }),
  );
  await user.click(
    await screen.findByText("Продажи", { selector: ".ant-select-tree-title" }),
  );
  await user.type(screen.getByRole("textbox", { name: "Название" }), name);
}

const save = (user: ReturnType<typeof userEvent.setup>) =>
  user.click(screen.getByRole("button", { name: "Сохранить" }));

const originalScrollIntoView = Element.prototype.scrollIntoView;
let restoreLayout: () => void = () => undefined;

beforeEach(() => {
  queryClient.clear();
  Element.prototype.scrollIntoView = vi.fn();
});

afterEach(() => {
  Element.prototype.scrollIntoView = originalScrollIntoView;
  restoreLayout();
  restoreLayout = () => undefined;
});

describe("saving a track together with its levels (AC-10, AC-11)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("creates the track, then each level in turn, then puts the levels in the order the user arranged", async () => {
    const served = serve();
    const onClose = vi.fn();
    render(
      <Providers>
        <TrackModal directionId={DIRECTION_ID} onClose={onClose} />
      </Providers>,
    );
    const user = userEvent.setup();
    await fillNewTrack(user, "Бэкенд");
    await addLevel(user, "Младший");
    await addLevel(user, "Старший");
    restoreLayout = stubRowLayout();
    screen
      .getByRole("button", { name: "Перетащить уровень «Старший»" })
      .focus();
    await user.keyboard(" ");
    await user.keyboard("{ArrowUp}");
    await user.keyboard(" ");
    await screen.findByText("Уровень «Старший» перемещён на позицию 1 из 2");

    await save(user);

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(served.steps).toEqual([
      `track Бэкенд in ${SALES}`,
      "level Младший of created",
      "level Старший of created",
      "order level-2,level-1",
    ]);
    expect(
      fakeApi
        .calls("PUT /api/v1/career/tracks/created/level-order")[0]
        ?.headers.get("If-Match"),
    ).toBe('"1"');
  });

  it("does not send an order the user did not change", async () => {
    const served = serve();
    const onClose = vi.fn();
    render(
      <Providers>
        <TrackModal directionId={DIRECTION_ID} onClose={onClose} />
      </Providers>,
    );
    const user = userEvent.setup();
    await fillNewTrack(user, "Бэкенд");
    await addLevel(user, "Младший");

    await save(user);

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(served.steps).toEqual([
      `track Бэкенд in ${SALES}`,
      "level Младший of created",
    ]);
  });

  it("keeps the dialog open when a level is refused, and a second save sends only what is left", async () => {
    const served = serve();
    let refusals = 1;
    fakeApi.on(LEVEL_ROUTE, async (request) => {
      const body = await request.json();
      if (body.name === "Старший" && refusals-- > 0) {
        return problem(500, { title: "Сбой" });
      }
      served.steps.push(`level ${body.name} of ${body.trackId}`);
      const created = savedLevel(
        `level-${served.levels.length + 1}`,
        body.name,
        body.trackId,
      );
      served.levels.push(created);
      return Response.json(created);
    });
    const onClose = vi.fn();
    render(
      <Providers>
        <TrackModal directionId={DIRECTION_ID} onClose={onClose} />
      </Providers>,
    );
    const user = userEvent.setup();
    await fillNewTrack(user, "Бэкенд");
    await addLevel(user, "Младший");
    await addLevel(user, "Старший");

    await save(user);

    expect(
      await screen.findByText("Не удалось сохранить запись"),
    ).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
    expect(served.steps).toEqual([
      `track Бэкенд in ${SALES}`,
      "level Младший of created",
    ]);

    await save(user);

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(served.steps).toEqual([
      `track Бэкенд in ${SALES}`,
      "level Младший of created",
      "level Старший of created",
    ]);
    expect(fakeApi.calls(TRACK_ROUTE)).toHaveLength(1);
  });

  it("renames an existing track against the version it was opened with, and creates nothing else", async () => {
    const served = serve();
    fakeApi.on("PUT /api/v1/career/tracks/existing", () =>
      Response.json(track("existing", "Серверная разработка", 4)),
    );
    const onClose = vi.fn();
    render(
      <Providers>
        <TrackModal
          directionId={DIRECTION_ID}
          track={track("existing", "Бэкенд", 3)}
          onClose={onClose}
        />
      </Providers>,
    );
    const user = userEvent.setup();
    const name = await screen.findByRole("textbox", { name: "Название" });
    await user.clear(name);
    await user.type(name, "Серверная разработка");

    await save(user);

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    const call = fakeApi.calls("PUT /api/v1/career/tracks/existing")[0];
    expect(call?.headers.get("If-Match")).toBe('"3"');
    expect(await call?.json()).toEqual({ name: "Серверная разработка" });
    expect(served.steps).toEqual([]);
  });

  it("saves nothing when a track is opened and saved without a change", async () => {
    serve();
    const onClose = vi.fn();
    render(
      <Providers>
        <TrackModal
          directionId={DIRECTION_ID}
          track={track("existing", "Бэкенд", 3)}
          onClose={onClose}
        />
      </Providers>,
    );
    const user = userEvent.setup();
    await screen.findByRole("textbox", { name: "Название" });

    await save(user);

    await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
    expect(fakeApi.calls("PUT /api/v1/career/tracks/existing")).toHaveLength(0);
    expect(fakeApi.calls(TRACK_ROUTE)).toHaveLength(0);
  });
});
