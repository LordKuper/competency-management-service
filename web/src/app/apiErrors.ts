import type { FormInstance } from "antd";
import { ApiError } from "../api/ApiError";

const NETWORK_MESSAGE =
  "Нет связи с сервером. Проверьте подключение к сети и повторите попытку.";
const SESSION_ENDED_MESSAGE = "Сессия завершена. Войдите снова.";
const INVALID_INPUT_MESSAGE =
  "Проверьте введённые данные: исправьте отмеченные поля.";
const FORBIDDEN_MESSAGE =
  "Недостаточно прав для этого действия. Обратитесь к администратору.";
const NOT_FOUND_MESSAGE =
  "Запись не найдена: возможно, её уже удалили. Обновите страницу.";
const STALE_MESSAGE =
  "Данные изменены другим пользователем. Показаны актуальные данные: проверьте их и повторите действие.";
const TOO_MANY_REQUESTS_MESSAGE =
  "Слишком много попыток. Подождите немного и повторите.";
const FAILURE_MESSAGE =
  "Операция не выполнена из-за ошибки сервиса. Повторите попытку позже; если ошибка повторяется, обратитесь к администратору.";

/** Validation messages of a failed call that belong to no field. */
const GENERAL_FIELD = "";

/**
 * Russian explanation of a failed call, saying why and what to do next.
 * Only business and validation texts the server wrote in Russian are passed through; every other status gets a fixed message.
 */
export function describeApiError(error: unknown): string {
  if (!(error instanceof ApiError)) return NETWORK_MESSAGE;
  switch (error.status) {
    case 400:
      return error.errors?.[GENERAL_FIELD]?.join(" ") ?? INVALID_INPUT_MESSAGE;
    case 401:
      return error.detail ?? SESSION_ENDED_MESSAGE;
    case 403:
      return FORBIDDEN_MESSAGE;
    case 404:
      return NOT_FOUND_MESSAGE;
    case 409:
      return (
        [error.title, error.detail].filter(Boolean).join(": ") ||
        FAILURE_MESSAGE
      );
    case 412:
      return STALE_MESSAGE;
    case 429:
      return TOO_MANY_REQUESTS_MESSAGE;
    default:
      return FAILURE_MESSAGE;
  }
}

/** Puts the server's per-field validation messages under the matching form fields. */
export function showFieldErrors(form: FormInstance, error: unknown): void {
  if (!(error instanceof ApiError) || !error.errors) return;
  form.setFields(
    Object.entries(error.errors)
      .filter(([field]) => field !== GENERAL_FIELD)
      .map(([name, errors]) => ({ name, errors: [...errors] })),
  );
}
