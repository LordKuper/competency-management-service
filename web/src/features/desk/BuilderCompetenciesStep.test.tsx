import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import type { MatrixVersion } from "../career/matrixApi";
import { BuilderCompetenciesStep } from "./BuilderCompetenciesStep";

const CONTENT_ROUTE = "PUT /api/v1/career/matrix-versions/v1/content";
const UNIT = { id: "u1", name: "Команда" };
const SAVED_COMPETENCY = { id: "c1", name: "Работа в команде" };
const OTHER_COMPETENCY = { id: "c2", name: "Ответственность" };
const BLOCK = { id: "b1", name: "Командный блок" };
const NOT_ATTACHED = /создан черновиком, но к версии не подключён/;
const VERSION = {
  id: "v1",
  matrixId: "m1",
  number: 2,
  version: 3,
  status: "Draft",
  builderStep: 2,
  content: {
    levels: [],
    competencies: [
      { competencyId: SAVED_COMPETENCY.id, blockId: null, type: "Profile" },
    ],
    sharedBlocks: [],
    blockWeights: [],
    requirements: [],
  },
  entries: {
    direction: null,
    track: null,
    levels: [],
    competencies: [],
    position: null,
    sharedBlocks: [],
    scales: [],
  },
  superiorApprover: null,
  hr: null,
  actions: ["Edit"],
} as unknown as MatrixVersion;

function competency({ id, name }: { id: string; name: string }) {
  return {
    id,
    name,
    description: null,
    directionId: null,
    directionName: null,
    scaleId: "s1",
    scaleName: "Шкала",
    status: "Active",
    createdAt: "2026-10-01T10:00:00Z",
    versionNumber: 1,
    version: 1,
    canEdit: false,
    canDelete: false,
  };
}

function serve() {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "head", email: "head@test.local", roles: [] }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [UNIT], hrUnits: [] }),
  );
  fakeApi.on("GET /api/v1/career/matrices/m1", () =>
    Response.json({ id: "m1", orgUnitId: UNIT.id, orgUnitName: UNIT.name }),
  );
  fakeApi.on("GET /api/v1/career/matrix-versions/v1/changes", () =>
    Response.json({
      number: 1,
      previousNumber: null,
      before: VERSION.content,
      after: VERSION.content,
      changes: [],
    }),
  );
  fakeApi.on("GET /api/v1/career/competencies", () =>
    Response.json({
      items: [competency(SAVED_COMPETENCY), competency(OTHER_COMPETENCY)],
      total: 2,
      page: 1,
      pageSize: 200,
    }),
  );
  fakeApi.on("GET /api/v1/org-units/tree", () =>
    Response.json([
      {
        id: UNIT.id,
        name: UNIT.name,
        parentId: null,
        headEmployeeId: null,
        isActive: true,
        version: 1,
      },
    ]),
  );
  fakeApi.on("POST /api/v1/career/blocks", () =>
    Response.json({
      ...BLOCK,
      competencies: [SAVED_COMPETENCY],
      status: "Draft",
      version: 0,
      canEdit: true,
      canDelete: true,
      scopeUnitId: UNIT.id,
      scopeUnitName: UNIT.name,
    }),
  );
  fakeApi.on(`GET /api/v1/career/blocks/${BLOCK.id}/publication`, () =>
    problem(403),
  );
  fakeApi.on(CONTENT_ROUTE, () =>
    Response.json({ ...VERSION, version: 4, builderStep: 2 }),
  );
}

function renderStep() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter([
          {
            path: "/",
            element: (
              <BuilderCompetenciesStep
                version={VERSION}
                step={2}
                onBack={vi.fn()}
                onSaved={vi.fn()}
                onSkip={vi.fn()}
                onEditsChange={vi.fn()}
              />
            ),
          },
        ])}
      />
    </Providers>,
  );
  return userEvent.setup();
}

function checkboxOf(scope: Pick<typeof screen, "getByText">, name: string) {
  const row = scope.getByText(name).closest("tr");
  if (!row) throw new Error(`No row of ${name}`);
  return within(row).getByRole("checkbox");
}

beforeEach(() => {
  queryClient.clear();
});

describe("a block created in the wizard from a competency the author took out of the version is connected to it (AC-38)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("saves the version with the new block and without the competency outside every block, and reports no overlap", async () => {
    serve();
    const user = renderStep();
    const own = await screen.findByText(SAVED_COMPETENCY.name);
    expect(own).toBeInTheDocument();
    await waitFor(() =>
      expect(checkboxOf(screen, SAVED_COMPETENCY.name)).toBeChecked(),
    );
    await user.click(checkboxOf(screen, SAVED_COMPETENCY.name));
    await user.click(
      await screen.findByRole("button", { name: "Создать блок" }),
    );
    const dialog = await screen.findByRole("dialog");
    await user.type(within(dialog).getByLabelText("Название"), BLOCK.name);
    await user.click(
      await waitFor(() => checkboxOf(within(dialog), SAVED_COMPETENCY.name)),
    );

    await user.click(within(dialog).getByRole("button", { name: "Сохранить" }));

    await waitFor(() => expect(fakeApi.calls(CONTENT_ROUTE)).toHaveLength(1));
    const saved = await fakeApi.calls(CONTENT_ROUTE)[0]?.json();
    expect(
      saved.sharedBlocks.map((block: { blockId: string }) => block.blockId),
    ).toEqual([BLOCK.id]);
    expect(saved.competencies).toEqual([]);
    expect(screen.queryByText(NOT_ATTACHED)).toBeNull();
  });
});
