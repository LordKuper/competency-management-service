import { useEffect, useState } from "react";

/** How long typing must pause before a search text is sent to the server. */
export const SEARCH_DEBOUNCE_MS = 300;

/** The value once it has stayed unchanged for the delay; the first value is returned at once. */
export function useDebouncedValue<TValue>(
  value: TValue,
  delayMs: number = SEARCH_DEBOUNCE_MS,
): TValue {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}
