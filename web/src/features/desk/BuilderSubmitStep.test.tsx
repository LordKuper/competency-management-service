import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
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

function renderStep() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter([
          {
            path: "/",
            element: <BuilderSubmitStep version={VERSION} onBack={() => {}} />,
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
