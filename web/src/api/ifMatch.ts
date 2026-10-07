/** The `If-Match` value for a version: the server issues the entity tag of a resource as its quoted version, and list rows carry only the version. */
export function ifMatchOf(version: number): string {
  return `"${version}"`;
}
