import type { UserRole } from "../../app/featureContract";

/** Russian name of each role as administrators read it. */
export const ROLE_LABEL: Record<UserRole, string> = {
  GlobalAdmin: "Глобальный администратор",
  Hr: "HR",
};

/** What an account without a special role is called. */
export const PLAIN_USER_LABEL = "Пользователь";

/** The role filter value that finds accounts without a special role. */
export type UserRoleFilter = UserRole | "User";

/** Role choices of the account form and the list filter, in the order they are offered. */
export const ROLE_OPTIONS: { value: UserRole; label: string }[] = [
  { value: "GlobalAdmin", label: ROLE_LABEL.GlobalAdmin },
  { value: "Hr", label: ROLE_LABEL.Hr },
];

/** Role choices of the list filter: the roles and the plain user. */
export const ROLE_FILTER_OPTIONS: { value: UserRoleFilter; label: string }[] = [
  { value: "User", label: PLAIN_USER_LABEL },
  ...ROLE_OPTIONS,
];

/** The roles of an account as one comma-separated text; an account without roles is a plain user. */
export function formatRoles(roles: ReadonlyArray<UserRole>): string {
  if (roles.length === 0) return PLAIN_USER_LABEL;
  return ROLE_OPTIONS.filter(({ value }) => roles.includes(value))
    .map(({ label }) => label)
    .join(", ");
}

/** The same text for a role as the audit journal records it: role names joined by commas, or `User` for none. */
export function formatRecordedRoles(recorded: string): string {
  const names = recorded.split(",");
  return formatRoles(
    ROLE_OPTIONS.map(({ value }) => value).filter((role) =>
      names.includes(role),
    ),
  );
}
