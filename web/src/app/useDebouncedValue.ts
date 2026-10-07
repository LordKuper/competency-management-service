import { useEffect, useState } from "react";

/** How long typing must pause before a search text is sent to the server. */
export const SEARCH_DEBOUNCE_MS = 300;

/** The value once it has stayed unchanged for the delay; the first value is returned at once. */
export function useDebouncedValue<TValue>(value: TValue): TValue {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [value]);
  return debounced;
}
