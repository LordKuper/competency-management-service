import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import { deskPaths } from "../features/desk/deskTexts";
import { ScreenTour } from "./ScreenTour";

async function openHelpAt(step: number | null) {
  render(
    <MemoryRouter initialEntries={[deskPaths.matrixBuilder]}>
      {step !== null && <div data-tour="wizard-steps" data-tour-step={step} />}
      <ScreenTour />
    </MemoryRouter>,
  );
  await userEvent.click(
    screen.getByRole("button", { name: "Справка по экрану" }),
  );
}

/** The titles the tour shows one after another, by pressing its «Далее» button until it is gone. */
async function titlesOfTour(step: number | null) {
  await openHelpAt(step);
  const titles: string[] = [];
  for (;;) {
    const title = await waitFor(() => {
      const shown = document.querySelector(".ant-tour-title");
      expect(shown).not.toBeNull();
      return shown?.textContent ?? "";
    });
    titles.push(title);
    const next = document.querySelector<HTMLElement>(".tour-next");
    if (!next || next.textContent?.match(/Заверш|Finish/)) return titles;
    await userEvent.click(next);
    await waitFor(() =>
      expect(document.querySelector(".ant-tour-title")?.textContent).not.toBe(
        title,
      ),
    );
  }
}

describe("the help of a wizard screen (AC-92)", () => {
  it("opens with the overview of the process on the first step", async () => {
    const titles = await titlesOfTour(1);

    expect(titles).toContain("Построить матрицу компетенций");
  });

  it("shows the hints of the step the wizard is on and none of the other steps", async () => {
    const titles = await titlesOfTour(2);

    expect(titles).toContain("Шаг 2. Состав матрицы");
    expect(titles).not.toContain("Построить матрицу компетенций");
    expect(titles).not.toContain("Шаг 3. Требования по уровням");
  });

  it("gives the hints of the third step on the third step", async () => {
    const titles = await titlesOfTour(3);

    expect(titles).toContain("Шаг 3. Требования по уровням");
    expect(titles).not.toContain("Шаг 2. Состав матрицы");
  });
});
