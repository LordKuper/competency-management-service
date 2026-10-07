import type { TestingLibraryMatchers } from "@testing-library/jest-dom/matchers";

/**
 * Types the jest-dom matchers for Vitest 5, whose matcher interface is `Matchers<R, T>`;
 * the augmentation shipped with jest-dom targets the older `Assertion<T>` shape and does not compile.
 */
declare module "vitest" {
  interface Matchers<
    R extends void | Promise<void> = void | Promise<void>,
    T = unknown,
  > extends TestingLibraryMatchers<unknown, R> {}
}
