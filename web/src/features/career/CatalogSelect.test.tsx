import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { CatalogSelect } from "./CatalogSelect";

const COMPETENCIES_ROUTE = "GET /api/v1/career/competencies";
const OPTION = ".ant-select-item-option-content";

const competency = (number: number) => ({
  id: `competency-${number}`,
  name: `Компетенция ${String(number).padStart(3, "0")}`,
});

/** A server that has `count` competencies by name and gives as many of the first as the request asks for. */
function serveCompetencies(count: number) {
  const all = Array.from({ length: count }, (_, index) =>
    competency(index + 1),
  );
  fakeApi.on(COMPETENCIES_ROUTE, (request) => {
    const pageSize = Number(new URL(request.url).searchParams.get("pageSize"));
    return Response.json({
      items: all.slice(0, pageSize),
      total: all.length,
      page: 1,
      pageSize,
    });
  });
  return all;
}

function renderSelect(exclude: readonly string[]) {
  render(
    <Providers>
      <CatalogSelect
        segment="competencies"
        aria-label="Компетенции"
        exclude={exclude}
      />
    </Providers>,
  );
  return userEvent.setup();
}

const askedPageSizes = () =>
  fakeApi
    .calls(COMPETENCIES_ROUTE)
    .map((call) => new URL(call.url).searchParams.get("pageSize"));

beforeEach(() => {
  queryClient.clear();
});

describe("a picker that leaves out the entries already used (AC-14)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("offers the entry that is left when the first twenty by name are the ones already used", async () => {
    const all = serveCompetencies(21);
    const used = all.slice(0, 20).map((entry) => entry.id);
    const user = renderSelect(used);

    await user.click(
      await screen.findByRole("combobox", { name: "Компетенции" }),
    );

    expect(
      await screen.findByText(competency(21).name, { selector: OPTION }),
    ).toBeInTheDocument();
    expect(
      all
        .slice(0, 20)
        .filter((entry) =>
          screen.queryByText(entry.name, { selector: OPTION }),
        ),
    ).toEqual([]);
  });

  it.each([
    [0, "20"],
    [1, "21"],
    [180, "200"],
    [250, "200"],
  ])(
    "with %i entries left out asks for a page of %s, never above the largest the server gives",
    async (used, pageSize) => {
      const all = serveCompetencies(300);
      renderSelect(all.slice(0, used).map((entry) => entry.id));

      await waitFor(() => expect(askedPageSizes()).toEqual([pageSize]));
    },
  );
});
