import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { TrackPositionsPage } from "./TrackPositionsPage";

const TRACKS_ROUTE = "GET /api/v1/career/tracks";
const SEARCH_TEXT = "Аналитик";

function track(id: string, name: string) {
  return {
    id,
    name,
    ownerOrgUnitName: "Команда",
    status: "Active",
    canEdit: true,
  };
}

const FIRST_PAGE = [track("first", "Инженер")];
const SEARCHED = [track("far", "Аналитик данных")];
const NOT_FOUND_TEXT = "Хранитель";

function serveTracks() {
  fakeApi.on(TRACKS_ROUTE, (request) => {
    const searched = new URL(request.url).searchParams.get("q");
    const items =
      searched === NOT_FOUND_TEXT ? [] : searched ? SEARCHED : FIRST_PAGE;
    return Response.json({
      items,
      total: items.length,
      page: 1,
      pageSize: 200,
    });
  });
}

function searchedTexts() {
  return fakeApi
    .calls(TRACKS_ROUTE)
    .map((request) => new URL(request.url).searchParams.get("q"));
}

function renderPage() {
  render(
    <Providers>
      <RouterProvider
        router={createMemoryRouter(
          [
            { path: "/desk/track-positions", element: <TrackPositionsPage /> },
            {
              path: "/desk/track-positions/:trackId",
              element: <TrackPositionsPage />,
            },
          ],
          { initialEntries: ["/desk/track-positions"] },
        )}
      />
    </Providers>,
  );
  return userEvent.setup();
}

beforeEach(() => {
  queryClient.clear();
});

describe("the choice of a track to link positions to is searched on the server (AC-162)", () => {
  it("sends what the user types as the search of the track list, and offers the tracks found", async () => {
    serveTracks();
    const user = renderPage();
    const combobox = await screen.findByRole("combobox", { name: "Трек" });
    await waitFor(() => expect(fakeApi.calls(TRACKS_ROUTE)).toHaveLength(1));
    expect(searchedTexts()).toEqual([null]);

    await user.type(combobox, SEARCH_TEXT);

    await waitFor(() => expect(searchedTexts().at(-1)).toBe(SEARCH_TEXT));
    expect(
      await screen.findByText("Аналитик данных — Команда"),
    ).toBeInTheDocument();
  });

  it("names the text searched for when no track the user may edit matches it", async () => {
    serveTracks();
    const user = renderPage();
    const combobox = await screen.findByRole("combobox", { name: "Трек" });

    await user.type(combobox, NOT_FOUND_TEXT);

    expect(
      await screen.findByText(
        `Трек «${NOT_FOUND_TEXT}» не найден среди тех, связи которых вы вправе править.`,
      ),
    ).toBeInTheDocument();
  });

  it("offers the whole list again once the list is closed, without the text searched for", async () => {
    serveTracks();
    const user = renderPage();
    const combobox = await screen.findByRole("combobox", { name: "Трек" });
    await user.type(combobox, SEARCH_TEXT);
    await screen.findByText("Аналитик данных — Команда");

    await user.click(document.body);
    await user.click(combobox);

    expect(await screen.findByText("Инженер — Команда")).toBeInTheDocument();
    expect(screen.queryByText("Аналитик данных — Команда")).toBeNull();
  });
});
