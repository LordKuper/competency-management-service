import { describe, expect, it } from "vitest";
import { ApiError } from "../api/ApiError";
import { describeApiError } from "./apiErrors";

describe("describeApiError (AC-5, AC-13)", () => {
  it("explains a stale version as a change made by someone else and asks to check and repeat", () => {
    const message = describeApiError(
      new ApiError(412, { title: "Precondition Failed" }),
    );

    expect(message).toContain("изменены другим пользователем");
    expect(message).toContain("повторите");
  });

  it("passes the server's title and detail through on a conflict", () => {
    const error = new ApiError(409, {
      title: "Операция отклонена",
      detail:
        "Нельзя уволить или удалить сотрудника: к нему привязан последний активный глобальный администратор.",
    });

    expect(describeApiError(error)).toBe(
      "Операция отклонена: Нельзя уволить или удалить сотрудника: к нему привязан последний активный глобальный администратор.",
    );
  });

  it("shows the messages of a validation problem that belong to no field, and a hint otherwise", () => {
    const general = new ApiError(400, {
      errors: { "": ["Укажите допустимое значение."] },
    });
    const perField = new ApiError(400, {
      errors: { email: ["E-mail: укажите значение."] },
    });

    expect(describeApiError(general)).toBe("Укажите допустимое значение.");
    expect(describeApiError(perField)).toContain("исправьте отмеченные поля");
  });

  it("treats anything that is not an API error as a lost connection", () => {
    expect(describeApiError(new TypeError("Failed to fetch"))).toContain(
      "Нет связи с сервером",
    );
  });

  it.each([
    [403, "Недостаточно прав"],
    [404, "Запись не найдена"],
    [429, "Слишком много попыток"],
    [500, "ошибки сервиса"],
  ])("uses a fixed Russian message for status %i", (status, expected) => {
    expect(
      describeApiError(
        new ApiError(status, { title: "ignored", detail: "ignored" }),
      ),
    ).toContain(expected);
  });
});
