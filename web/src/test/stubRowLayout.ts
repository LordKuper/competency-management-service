const ROW_HEIGHT = 40;
const ROW_WIDTH = 300;

/**
 * Gives the keyed table rows a layout, which jsdom does not have: each row sits right below the one before it, as the
 * drag-and-drop of sortable rows needs to find the row a dragged one is over. Returns the call that undoes it.
 */
export function stubRowLayout(): () => void {
  const original = Element.prototype.getBoundingClientRect;
  Element.prototype.getBoundingClientRect = function (this: Element) {
    const row = this.closest("tr[data-row-key]");
    const rows = Array.from(
      row?.closest("tbody")?.querySelectorAll("tr[data-row-key]") ?? [],
    );
    const top = Math.max(rows.indexOf(row as Element), 0) * ROW_HEIGHT;
    return {
      x: 0,
      y: top,
      top,
      bottom: top + ROW_HEIGHT,
      left: 0,
      right: ROW_WIDTH,
      width: ROW_WIDTH,
      height: ROW_HEIGHT,
      toJSON: () => ({}),
    };
  };
  return () => {
    Element.prototype.getBoundingClientRect = original;
  };
}
