import { HOME_PATH, LOGIN_PATH } from "../../app/featureContract";
import { FORGOT_PASSWORD_PATH } from "./ForgotPasswordPage";
import {
  ACCEPT_INVITATION_PATH,
  RESET_PASSWORD_PATH,
} from "./LinkPasswordPage";

/** Query parameter of the sign-in address that carries the page to open after signing in. */
const RETURN_TO_PARAM = "returnTo";

/** Screens used without a session: returning to one after signing in makes no sense. */
const SIGN_IN_PATHS: ReadonlySet<string> = new Set([
  LOGIN_PATH,
  FORGOT_PASSWORD_PATH,
  ACCEPT_INVITATION_PATH,
  RESET_PASSWORD_PATH,
]);

const FIRST_PRINTABLE_CODE = 0x20;
const DELETE_CODE = 0x7f;

/** Backslashes and control characters, which browsers may read as path separators or drop while parsing. */
function hasUnsafeCharacter(value: string): boolean {
  return [...value].some((char) => {
    const code = char.charCodeAt(0);
    return char === "\\" || code < FIRST_PRINTABLE_CODE || code === DELETE_CODE;
  });
}

/**
 * Sign-in address that returns to the given in-app page; pages a plain sign-in opens anyway, and the sign-in screens
 * themselves, are not carried.
 */
export function loginPathReturningTo(target: string): string {
  if (target === "/" || target === HOME_PATH || !safeReturnTo(target)) {
    return LOGIN_PATH;
  }
  return `${LOGIN_PATH}?${new URLSearchParams({ [RETURN_TO_PARAM]: target })}`;
}

/** The return target carried by the sign-in address, once it passed {@link safeReturnTo}; null when absent or refused. */
export function returnTargetOf(search: URLSearchParams): string | null {
  return safeReturnTo(search.get(RETURN_TO_PARAM));
}

/**
 * The in-app page a sign-in may return to, as the parsed path and query; null for anything else. The value comes from
 * the address bar, so it is untrusted: it must stay on this origin, be a path starting with exactly one slash, carry no
 * backslash or control character and not be a sign-in screen. The parsed path is returned rather than the raw string,
 * so what is checked is what is opened.
 */
export function safeReturnTo(value: string | null): string | null {
  if (
    !value?.startsWith("/") ||
    value.startsWith("//") ||
    hasUnsafeCharacter(value)
  ) {
    return null;
  }
  let url: URL;
  try {
    url = new URL(value, window.location.origin);
  } catch {
    return null;
  }
  if (
    url.origin !== window.location.origin ||
    url.pathname.startsWith("//") ||
    SIGN_IN_PATHS.has(url.pathname)
  ) {
    return null;
  }
  return url.pathname + url.search;
}
