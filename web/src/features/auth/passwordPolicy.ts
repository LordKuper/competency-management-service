import { useQuery } from "@tanstack/react-query";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";

const RULES = "заглавные и строчные буквы, цифры и специальные символы.";

/**
 * Hint under every new-password field. The minimum length is a deployment setting, so it comes from the anonymous
 * `GET /api/v1/auth/password-policy`; until it is known, or if the request fails, the hint names no number.
 */
export function usePasswordHint(): string {
  const { data } = useQuery({
    queryKey: ["auth", "password-policy"],
    queryFn: async () => unwrap(await api.GET("/api/v1/auth/password-policy")),
    staleTime: 60 * 60 * 1000,
    retry: false,
  });
  return data
    ? `Не короче ${data.minLength} символов; ${RULES}`
    : `Пароль должен быть длинным и содержать ${RULES}`;
}
