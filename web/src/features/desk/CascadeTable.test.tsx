import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { PublicationEntry } from "../career/catalogApi";
import { CascadeTable } from "./CascadeTable";

const DIRECTION: PublicationEntry = {
  kind: "Direction",
  id: "d1",
  name: "Разработка",
  context: null,
};
const LEVEL: PublicationEntry = {
  kind: "Level",
  id: "l1",
  name: "Старший",
  context: "Бэкенд",
};
const HIDDEN_TEXT = /и ещё/;

describe("the records a decision publishes with its object (AC-110, AC-65, AC-77)", () => {
  it("names each record the user may read with its kind and state, and says nothing of others when there are none", () => {
    render(
      <CascadeTable
        drafts={{ entries: [DIRECTION, LEVEL], hiddenEntryCount: 0 }}
      />,
    );

    expect(screen.getAllByRole("row")).toHaveLength(3);
    expect(screen.getByText("Область")).toBeInTheDocument();
    expect(screen.getByText("Разработка")).toBeInTheDocument();
    expect(screen.getByText("Старший (Бэкенд)")).toBeInTheDocument();
    expect(screen.getAllByText("Черновик")).toHaveLength(2);
    expect(screen.queryByText(HIDDEN_TEXT)).toBeNull();
  });

  it("counts the records the user may not read under the table, by number only", () => {
    render(
      <CascadeTable drafts={{ entries: [DIRECTION], hiddenEntryCount: 2 }} />,
    );

    expect(screen.getAllByRole("row")).toHaveLength(2);
    expect(screen.getByText("Разработка")).toBeInTheDocument();
    expect(screen.getByText("и ещё 2 черновые записи")).toBeInTheDocument();
  });

  it("shows no table at all when the user may read none of the records, only their number", () => {
    render(<CascadeTable drafts={{ entries: [], hiddenEntryCount: 5 }} />);

    expect(screen.queryByRole("table")).toBeNull();
    expect(screen.getByText("и ещё 5 черновых записей")).toBeInTheDocument();
  });
});
