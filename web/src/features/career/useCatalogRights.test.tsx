import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { currentUserQueryKey } from "../auth/useCurrentUser";
import { careerQueryKey } from "./careerApi";
import { useCatalogRights } from "./useCatalogRights";

const UNIT = { id: "own-unit", name: "Свой отдел" };
const OTHER_UNIT = "other-unit";

async function rightsOf(
  role: "GlobalAdmin" | "User",
  roles: { headedUnits?: object[]; hrUnits?: object[] } = {},
) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({ id: "account", email: "user@test.local", role }),
  );
  fakeApi.on("GET /api/v1/career/me", () =>
    Response.json({ headedUnits: [], hrUnits: [], ...roles }),
  );
  const { result } = renderHook(() => useCatalogRights(), {
    wrapper: ({ children }: { children: ReactNode }) => (
      <Providers>{children}</Providers>
    ),
  });
  await waitFor(() => {
    expect(queryClient.getQueryData(currentUserQueryKey)).toBeDefined();
    expect(queryClient.getQueryData([...careerQueryKey, "me"])).toBeDefined();
  });
  await act(async () => undefined);
  return result;
}

beforeEach(() => {
  queryClient.clear();
});

describe("what the catalogue tables offer to whom (AC-9, AC-19)", () => {
  it("lets a global administrator create and manage everything, with or without a unit", async () => {
    const rights = (await rightsOf("GlobalAdmin")).current;

    expect(rights.isAdmin).toBe(true);
    expect(rights.isHr).toBe(true);
    expect(rights.canCreate).toBe(true);
    expect(rights.canCreateTrack).toBe(true);
    expect(rights.canManage()).toBe(true);
    expect(rights.canManage(OTHER_UNIT)).toBe(true);
  });

  it("lets the HR of a unit keep the entries without a unit and the tracks of that unit only", async () => {
    const rights = (await rightsOf("User", { hrUnits: [UNIT] })).current;

    expect(rights.isAdmin).toBe(false);
    expect(rights.isHr).toBe(true);
    expect(rights.canCreate).toBe(true);
    expect(rights.canCreateTrack).toBe(true);
    expect(rights.canManage()).toBe(true);
    expect(rights.canManage(UNIT.id)).toBe(true);
    expect(rights.canManage(OTHER_UNIT)).toBe(false);
  });

  it("lets the head of a unit propose drafts, and never approve or deactivate", async () => {
    const rights = (await rightsOf("User", { headedUnits: [UNIT] })).current;

    expect(rights.isHr).toBe(false);
    expect(rights.canCreate).toBe(true);
    expect(rights.canCreateTrack).toBe(true);
    expect(rights.canManage()).toBe(false);
    expect(rights.canManage(UNIT.id)).toBe(false);
  });

  it("offers a user with no process role nothing to change", async () => {
    const rights = (await rightsOf("User")).current;

    expect(rights.isHr).toBe(false);
    expect(rights.canCreate).toBe(false);
    expect(rights.canCreateTrack).toBe(false);
    expect(rights.canManage()).toBe(false);
  });

  it("lists a unit once as a place for a new track when the user both heads it and is its HR", async () => {
    const rights = (
      await rightsOf("User", { headedUnits: [UNIT], hrUnits: [UNIT] })
    ).current;

    expect(rights.ownerUnits).toEqual([UNIT]);
  });
});
