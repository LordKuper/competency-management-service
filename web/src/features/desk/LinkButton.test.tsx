import { act, render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { describe, expect, it } from "vitest";
import { LinkButton } from "./LinkButton";

const LABEL = "Открыть";
const TARGET_TEXT = "экран назначения";
const BROWSER_OWN_CLICKS: Record<string, MouseEventInit> = {
  "with Ctrl held": { ctrlKey: true },
  "with Meta held": { metaKey: true },
  "with Shift held": { shiftKey: true },
  "with Alt held": { altKey: true },
  "of the middle button": { button: 1 },
};

function renderLink() {
  render(
    <RouterProvider
      router={createMemoryRouter([
        {
          path: "/",
          element: <LinkButton to="/target">{LABEL}</LinkButton>,
        },
        { path: "/target", element: <p>{TARGET_TEXT}</p> },
      ])}
    />,
  );
  return screen.getByRole("link", { name: LABEL });
}

/**
 * Sends a click and reports whether the link had already cancelled the browser's own handling when the event reached the
 * document; the document then cancels it, so jsdom does not try to open the address.
 */
function clickAndReadDefault(link: HTMLElement, init: MouseEventInit) {
  let wasCancelledByLink = false;
  const cancelAtDocument = (event: Event) => {
    wasCancelledByLink = event.defaultPrevented;
    event.preventDefault();
  };
  document.addEventListener("click", cancelAtDocument);
  act(() => {
    link.dispatchEvent(
      new MouseEvent("click", { bubbles: true, cancelable: true, ...init }),
    );
  });
  document.removeEventListener("click", cancelAtDocument);
  return wasCancelledByLink;
}

describe("a link that looks like a button opens inside the application on a plain click only", () => {
  it("navigates in place and takes the click over on a plain left click", async () => {
    const link = renderLink();

    const wasCancelled = clickAndReadDefault(link, {});

    expect(wasCancelled).toBe(true);
    expect(await screen.findByText(TARGET_TEXT)).toBeInTheDocument();
  });

  it.each(Object.entries(BROWSER_OWN_CLICKS))(
    "leaves a click %s to the browser, which opens the address its own way",
    (_, init) => {
      const link = renderLink();

      const wasCancelled = clickAndReadDefault(link, init);

      expect(wasCancelled).toBe(false);
      expect(screen.queryByText(TARGET_TEXT)).toBeNull();
    },
  );
});
