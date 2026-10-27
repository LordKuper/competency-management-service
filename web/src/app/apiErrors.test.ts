import type { FormInstance } from "antd";
import { describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/ApiError";
import {
  describeApiError,
  describeNameFailure,
  showFieldErrors,
} from "./apiErrors";

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

describe("describeNameFailure (AC-7)", () => {
  it("gives the reason the name was refused, which belongs to a field the screen may not show", () => {
    const error = new ApiError(400, { errors: { name: ["Укажите название"] } });

    expect(describeNameFailure(error)).toBe("Укажите название");
  });

  it("describes any other failure as usual", () => {
    expect(describeNameFailure(new ApiError(403, {}))).toContain(
      "Недостаточно прав",
    );
    expect(describeNameFailure(new TypeError("Failed to fetch"))).toContain(
      "Нет связи с сервером",
    );
  });
});

describe("showFieldErrors (AC-4)", () => {
  function formSpy() {
    const setFields = vi.fn();
    return { form: { setFields } as unknown as FormInstance, setFields };
  }

  it("puts a message of a secondary assignment's field under that field of that assignment", () => {
    const { form, setFields } = formSpy();

    showFieldErrors(
      form,
      new ApiError(400, {
        errors: {
          "assignments[1].positionId": ["Должность недоступна"],
          legalEntityIds: ["Юридическое лицо не найдено."],
        },
      }),
    );

    expect(setFields).toHaveBeenCalledWith([
      {
        name: ["assignments", 1, "positionId"],
        errors: ["Должность недоступна"],
      },
      { name: ["legalEntityIds"], errors: ["Юридическое лицо не найдено."] },
    ]);
  });

  it("leaves the form alone for messages that belong to no field and for failures that are not validation problems", () => {
    const { form, setFields } = formSpy();

    showFieldErrors(form, new ApiError(400, { errors: { "": ["Общее"] } }));
    showFieldErrors(form, new ApiError(409, {}));
    showFieldErrors(form, new TypeError("Failed to fetch"));

    expect(setFields).toHaveBeenLastCalledWith([]);
    expect(setFields).toHaveBeenCalledTimes(1);
  });
});
