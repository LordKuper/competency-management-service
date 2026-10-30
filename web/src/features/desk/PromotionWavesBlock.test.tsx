import { act, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { promotionQueryKey } from "../promotion/promotionApi";
import { PromotionWavesBlock } from "./PromotionWavesBlock";

const TITLE = "Карьерные сессии";
const WAVE = "wave-open";
const APPLICATION = "application-1";
const SUBMISSION = 1;
const ANNOUNCED = 0;

const wave = (id: string, stage: number, units: object[]) => ({
  id,
  startsOn: "2026-11-16",
  endsOn: [
    "2026-11-27",
    "2026-11-30",
    "2026-12-07",
    "2026-12-09",
    "2026-12-16",
    "2026-12-23",
  ],
  resultsPublishedOn: "2026-12-16",
  stage,
  units,
});

const unit = (name: string, tracks: string[]) => ({
  id: `unit-${name}`,
  name,
  targets: tracks.map((trackName) => ({
    matrixId: `matrix-${trackName}`,
    trackId: `track-${trackName}`,
    trackName,
    levels: [],
  })),
});

function serve(
  waves: object[],
  applications: object[] = [],
  employeeId: string | null = "employee",
) {
  fakeApi.on("GET /api/v1/auth/me", () =>
    Response.json({
      id: "account",
      email: "user@test.local",
      roles: [],
      employeeId,
    }),
  );
  fakeApi.on("GET /api/v1/promotion/waves/open", () => Response.json(waves));
  fakeApi.on("GET /api/v1/promotion/applications/mine", () =>
    Response.json(applications),
  );
}

async function showBlock() {
  render(
    <Providers>
      <MemoryRouter>
        <PromotionWavesBlock />
      </MemoryRouter>
    </Providers>,
  );
  await waitFor(() =>
    expect(
      queryClient.getQueryData([...promotionQueryKey, "my-applications"]),
    ).toBeDefined(),
  );
  await act(async () => undefined);
}

beforeEach(() => {
  queryClient.clear();
});

describe("the career sessions block of the desk (AC-51)", () => {
  it("offers a session that takes applications with its tracks and deadline, and the way to apply or to the application already filed", async () => {
    serve([
      wave(WAVE, SUBMISSION, [unit("Команда", ["Разработка", "Тестирование"])]),
    ]);
    await showBlock();

    expect(screen.getByRole("heading", { name: TITLE })).toBeInTheDocument();
    expect(screen.getByText("Идёт приём заявок")).toBeInTheDocument();
    expect(screen.getByText("до 27.11.2026")).toBeInTheDocument();
    expect(screen.getByText("Разработка")).toBeInTheDocument();
    expect(screen.getByText("Тестирование")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Подать заявку" })).toHaveAttribute(
      "href",
      `/promotion/apply/${WAVE}`,
    );
  });

  it("leads to the application already filed instead of a second one", async () => {
    serve(
      [wave(WAVE, SUBMISSION, [unit("Команда", ["Разработка"])])],
      [{ id: APPLICATION, waveId: WAVE, status: "Submitted" }],
    );
    await showBlock();

    expect(screen.getByRole("link", { name: "Моя заявка" })).toHaveAttribute(
      "href",
      `/promotion/applications/${APPLICATION}`,
    );
    expect(screen.queryByRole("link", { name: "Подать заявку" })).toBeNull();
  });

  it("announces a session that has not started with the date applications begin", async () => {
    serve([wave(WAVE, ANNOUNCED, [unit("Команда", ["Разработка"])])]);
    await showBlock();

    expect(screen.getByText("Скоро начнётся")).toBeInTheDocument();
    expect(screen.getByText("приём заявок с 16.11.2026")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Подробнее" })).toHaveAttribute(
      "href",
      "/promotion",
    );
    expect(screen.queryByRole("link", { name: "Подать заявку" })).toBeNull();
  });

  it("offers no application to an account without an employee", async () => {
    serve(
      [wave(WAVE, SUBMISSION, [unit("Команда", ["Разработка"])])],
      [],
      null,
    );
    await showBlock();

    expect(screen.getByText("Идёт приём заявок")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Подать заявку" })).toBeNull();
  });

  it("is hidden when no session offers a track", async () => {
    serve([wave(WAVE, SUBMISSION, [unit("Команда", [])])]);
    await showBlock();

    expect(screen.queryByRole("heading", { name: TITLE })).toBeNull();
  });
});
