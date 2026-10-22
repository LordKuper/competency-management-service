import { render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { MyPlacePage } from "./MyPlacePage";

const SAME_NAME = "Команда";
const OWN_UNIT = { id: "unit-own", name: SAME_NAME };
const SUPERIOR_UNIT = { id: "unit-superior", name: SAME_NAME };

function servePlace(hrUnit: { id: string; name: string }) {
  fakeApi.on("GET /api/v1/career/me/place", () =>
    Response.json({
      employee: { id: "employee", fullName: "Иванов Иван Иванович" },
      unit: [SUPERIOR_UNIT, OWN_UNIT],
      position: null,
      head: null,
      responsibleHr: {
        employeeId: "hr",
        name: "Петрова Анна Сергеевна",
        orgUnitId: hrUnit.id,
        orgUnitName: hrUnit.name,
      },
      tracks: [],
      publishedMatrices: [],
      noTrack: true,
    }),
  );
}

function renderPlace() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter([{ path: "/", element: <MyPlacePage /> }])}
      />
    </Providers>,
  );
}

beforeEach(() => {
  queryClient.clear();
});

describe("the place of the signed-in employee tells their own HR from an inherited one by the unit, not its name (AC-91)", () => {
  it("calls the assignment inherited when the superior unit that holds it has the same name as the own unit", async () => {
    servePlace(SUPERIOR_UNIT);
    renderPlace();

    expect(
      await screen.findByText(
        `Петрова Анна Сергеевна · унаследовано из «${SAME_NAME}»`,
      ),
    ).toBeInTheDocument();
  });

  it("calls the assignment the unit's own when it is held by the unit the employee works in", async () => {
    servePlace(OWN_UNIT);
    renderPlace();

    expect(
      await screen.findByText(
        "Петрова Анна Сергеевна · закрепление за вашим подразделением",
      ),
    ).toBeInTheDocument();
  });
});
