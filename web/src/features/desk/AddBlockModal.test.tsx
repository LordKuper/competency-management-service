import { act, render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import type { MatrixVersion } from "../career/matrixApi";
import { AddBlockModal } from "./AddBlockModal";

const ALLOWED = { id: "c-allowed", name: "Работа в команде" };
const FOREIGN = { id: "c-foreign", name: "Продажи по телефону" };
const FOREIGN_READ = `GET /api/v1/career/competencies/${FOREIGN.id}`;
const ENTRIES = {
  direction: null,
  track: null,
  levels: [],
  competencies: [],
  position: null,
  sharedBlocks: [],
  scales: [],
} as unknown as MatrixVersion["entries"];

function block(
  id: string,
  name: string,
  competencies: { id: string; name: string }[],
) {
  return {
    id,
    name,
    competencies,
    status: "Active",
    version: 1,
    canEdit: false,
    canDelete: false,
    scopeUnitId: null,
    scopeUnitName: null,
  };
}

function serveBlocks() {
  fakeApi.on("GET /api/v1/career/blocks", () =>
    Response.json({
      items: [
        block("b-fits", "Командный блок", [ALLOWED]),
        block("b-foreign", "Смешанный блок", [ALLOWED, FOREIGN]),
      ],
      total: 2,
      page: 1,
      pageSize: 200,
    }),
  );
}

function renderModal(allowedIds?: ReadonlySet<string>) {
  render(
    <Providers>
      <AddBlockModal
        entries={ENTRIES}
        reasonOf={() => undefined}
        allowedIds={allowedIds}
        onPick={vi.fn()}
        onClose={vi.fn()}
      />
    </Providers>,
  );
}

function rowOf(blockName: string) {
  const row = screen.getByText(blockName).closest("li");
  if (!row) throw new Error(`No row of the block ${blockName}`);
  return within(row);
}

beforeEach(() => {
  queryClient.clear();
});

describe("a shared block with a competency of another direction is not offered to a track matrix (AC-38)", () => {
  it("turns the block off and names the competency that keeps it out", async () => {
    serveBlocks();
    renderModal(new Set([ALLOWED.id]));

    await screen.findByText("Смешанный блок");

    const foreign = rowOf("Смешанный блок");
    expect(foreign.getByRole("button")).toBeDisabled();
    expect(
      foreign.getByText(
        `Недоступен: в составе компетенция «${FOREIGN.name}» другой области`,
      ),
    ).toBeInTheDocument();
    expect(rowOf("Командный блок").getByRole("button")).toBeEnabled();
  });

  it("reads no competency of the catalogue to name the one that keeps it out", async () => {
    serveBlocks();
    renderModal(new Set([ALLOWED.id]));

    await screen.findByText("Смешанный блок");
    await act(async () => undefined);

    expect(fakeApi.calls(FOREIGN_READ)).toHaveLength(0);
  });

  it("offers every block when the version names no competencies it is limited to", async () => {
    serveBlocks();
    renderModal();

    await screen.findByText("Смешанный блок");

    expect(rowOf("Смешанный блок").getByRole("button")).toBeEnabled();
    expect(screen.queryByText(/Недоступен/)).toBeNull();
  });
});
