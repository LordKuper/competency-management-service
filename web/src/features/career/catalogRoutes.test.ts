/// <reference types="node" />
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { fakeApi } from "../../test/fakeApi";
import {
  type CatalogAction,
  type CatalogSegment,
  changeCatalogStatus,
  deleteEntry,
  saveNamedEntry,
} from "./catalogApi";
import { type MatrixTransition, transitMatrixVersion } from "./matrixApi";

const contract = JSON.parse(
  readFileSync(resolve(process.cwd(), "..", "openapi", "openapi.json"), "utf8"),
) as {
  paths: Record<string, Record<string, unknown>>;
};

const SEGMENTS = Object.keys({
  directions: true,
  tracks: true,
  levels: true,
  competencies: true,
  scales: true,
  blocks: true,
} satisfies Record<CatalogSegment, true>) as CatalogSegment[];

const ACTIONS = Object.keys({
  approve: true,
  deactivate: true,
  activate: true,
} satisfies Record<CatalogAction, true>) as CatalogAction[];

const TRANSITIONS = Object.keys({
  submit: true,
  approve: true,
  "accept-changes": true,
  "accept-and-revise": true,
  "rollback-changes": true,
} satisfies Record<MatrixTransition, true>) as MatrixTransition[];

const ENTRY = { id: "id", version: 1 };

function serveEveryRouteOfTheContract() {
  for (const [path, methods] of Object.entries(contract.paths)) {
    for (const method of Object.keys(methods)) {
      fakeApi.on(
        `${method.toUpperCase()} ${path.replace(/\{[^}]+\}/g, "id")}`,
        () => Response.json({ ...ENTRY, number: 1 }),
      );
    }
  }
}

beforeEach(() => {
  queryClient.clear();
  serveEveryRouteOfTheContract();
});

describe("requests whose path is assembled at run time are routes of the contract (AC-23, AC-25)", () => {
  it.each(
    SEGMENTS.flatMap((segment) =>
      ACTIONS.map((action) => [segment, action] as const),
    ),
  )("%s: %s", async (segment, action) => {
    await expect(
      changeCatalogStatus(segment, ENTRY, action),
    ).resolves.toBeDefined();
  });

  it.each(SEGMENTS)("deletes a record of %s", async (segment) => {
    await expect(deleteEntry(segment, ENTRY)).resolves.toBeDefined();
  });

  it.each(["directions", "tracks"] as const)(
    "creates and renames a record of %s",
    async (segment) => {
      const values = {
        name: "Имя",
        directionId: "id",
        ownerOrgUnitId: "id",
      };

      await expect(
        saveNamedEntry(segment, values, undefined),
      ).resolves.toBeDefined();
      await expect(
        saveNamedEntry(segment, values, ENTRY),
      ).resolves.toBeDefined();
    },
  );

  it.each(TRANSITIONS)("moves a matrix version by %s", async (transition) => {
    await expect(
      transitMatrixVersion({ id: "id", version: 1 } as never, transition),
    ).resolves.toBeDefined();
  });

  it("goes nowhere when a route is not in the contract", async () => {
    await expect(
      changeCatalogStatus("positions" as CatalogSegment, ENTRY, "approve"),
    ).rejects.toThrow(
      /No fake handler for POST \/api\/v1\/career\/positions\/id\/approve/,
    );
  });
});
