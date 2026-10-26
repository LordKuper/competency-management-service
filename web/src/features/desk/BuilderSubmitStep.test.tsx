import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import type { MatrixVersion } from "../career/matrixApi";
import { BuilderSubmitStep } from "./BuilderSubmitStep";
import { deskPaths } from "./deskTexts";

const SUBMIT_ROUTE = "POST /api/v1/career/matrix-versions/v1/submit";
const VERSION = {
  id: "v1",
  matrixId: "m1",
  number: 2,
  version: 3,
  authorName: "Иванов Иван Иванович",
  content: {
    levels: [],
    competencies: [],
    sharedBlocks: [],
    blockWeights: [],
    requirements: [],
  },
  entries: { track: null, position: null, sharedBlocks: [] },
  superiorApprover: null,
  hr: null,
  actions: ["Submit"],
} as unknown as MatrixVersion;

function serve(hiddenEntryCount: number) {
  fakeApi.on("GET /api/v1/career/matrices/m1", () =>
    Response.json({ id: "m1", orgUnitName: "Команда" }),
  );
  fakeApi.on("GET /api/v1/career/matrix-versions/v1/completeness", () =>
    Response.json({ problems: [] }),
  );
  fakeApi.on("GET /api/v1/career/matrix-versions/v1/publication", () =>
    Response.json({
      publishingActions: ["Submit"],
      entries: [],
      hiddenEntryCount,
      blockers: [],
      moreCount: 0,
      cascadeTag: "tag-submit",
    }),
  );
  fakeApi.on(SUBMIT_ROUTE, () =>
    Response.json({ id: "v1", version: 4, status: "Published" }),
  );
}

function renderStep(onBack: () => void = () => {}) {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter([
          {
            path: "/",
            element: <BuilderSubmitStep version={VERSION} onBack={onBack} />,
          },
          { path: deskPaths.desk, element: <p>рабочий стол</p> },
        ])}
      />
    </Providers>,
  );
  return userEvent.setup();
}

beforeEach(() => {
  queryClient.clear();
});

describe("sending a version whose publication carries drafts the user may not read (AC-52, AC-110, AC-112)", () => {
  it("says the version goes with them, counts them under the list, and sends under the tag of the whole list", async () => {
    serve(3);
    const user = renderStep();

    expect(
      await screen.findByText(/и вместе с ней 3 черновые записи/),
    ).toBeInTheDocument();

    expect(screen.getByText("и ещё 3 черновые записи")).toBeInTheDocument();
    await user.click(
      screen.getByRole("button", { name: "Отправить и опубликовать" }),
    );
    await waitFor(() => expect(fakeApi.calls(SUBMIT_ROUTE)).toHaveLength(1));
    const request = fakeApi.calls(SUBMIT_ROUTE)[0];
    expect(
      new URL(request?.url ?? "http://x").searchParams.get("cascade"),
    ).toBe("tag-submit");
    expect(
      await screen.findByText(
        "Версия опубликована; вместе с ней опубликовано записей: 3",
      ),
    ).toBeInTheDocument();
  });

  it("says no drafts go with the version when none does, and lists none", async () => {
    serve(0);
    renderStep();

    expect(await screen.findByText(/без черновых записей/)).toBeInTheDocument();

    expect(screen.queryByText(/и ещё/)).toBeNull();
  });
});

const CHECK_ROUTE = "GET /api/v1/career/matrix-versions/v1/completeness";
const LIST_ROUTE = "GET /api/v1/career/matrix-versions/v1/publication";
const FAILED_READS: Record<string, string> = {
  "the completeness check": CHECK_ROUTE,
  "the list of what the sending publishes": LIST_ROUTE,
};

describe("the sending of a version whose preparation could not be read (AC-52)", () => {
  it.each(Object.entries(FAILED_READS))(
    "shows the failure of %s with a retry, holds the sending off with the reason, and keeps the way back",
    async (_, route) => {
      serve(0);
      fakeApi.on(route, () => problem(403, { title: "Нет доступа" }));
      const onBack = vi.fn();
      const user = renderStep(onBack);

      expect(
        await screen.findByText("Не удалось подготовить отправку"),
      ).toBeInTheDocument();

      expect(screen.getByRole("button", { name: "Повторить" })).toBeEnabled();
      expect(
        screen.getByRole("button", { name: "Отправить на согласование" }),
      ).toBeDisabled();
      expect(
        screen.getByText(/Отправка недоступна, пока не загружены/),
      ).toBeInTheDocument();
      await user.click(screen.getByRole("button", { name: "Назад" }));
      expect(onBack).toHaveBeenCalledOnce();
    },
  );

  it("reads both again on a retry and offers the sending once they are read", async () => {
    serve(0);
    for (const route of Object.values(FAILED_READS)) {
      fakeApi.on(route, () => problem(403, { title: "Нет доступа" }));
    }
    const user = renderStep();
    const retry = await screen.findByRole("button", { name: "Повторить" });
    serve(0);
    await user.click(retry);

    await waitFor(() =>
      expect(
        screen.queryByText("Не удалось подготовить отправку"),
      ).not.toBeInTheDocument(),
    );

    expect(await screen.findByText(/без черновых записей/)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Отправить и опубликовать" }),
    ).toBeEnabled();
  });
});
