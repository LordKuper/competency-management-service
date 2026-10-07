import { Alert } from "antd";
import { useEffect } from "react";
import { useRouteError } from "react-router";

/** Fallback for an unexpected failure while rendering or loading a route; the cause goes to the console only. */
export function RouteErrorPage() {
  const error = useRouteError();
  useEffect(() => {
    console.error(error);
  }, [error]);
  return (
    <Alert
      type="error"
      showIcon
      title="Не удалось открыть страницу"
      description="Произошла непредвиденная ошибка. Обновите страницу; если ошибка повторяется, обратитесь к администратору."
    />
  );
}
