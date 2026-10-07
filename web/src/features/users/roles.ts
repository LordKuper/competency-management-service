import type { UserRole } from "../../app/featureContract";

/** Russian name of each role as administrators read it. */
export const ROLE_LABEL: Record<UserRole, string> = {
  GlobalAdmin: "Глобальный администратор",
  User: "Пользователь",
};

/** Role choices for selects, in the order they are offered. */
export const ROLE_OPTIONS: { value: UserRole; label: string }[] = [
  { value: "User", label: ROLE_LABEL.User },
  { value: "GlobalAdmin", label: ROLE_LABEL.GlobalAdmin },
];
