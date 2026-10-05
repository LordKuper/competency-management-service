import { QueryClientProvider } from "@tanstack/react-query";
import { App, ConfigProvider } from "antd";
import ru_RU from "antd/locale/ru_RU";
import dayjs from "dayjs";
import "dayjs/locale/ru";
import type { ReactNode } from "react";
import { queryClient } from "../api/queryClient";
import { theme } from "./theme";

dayjs.locale("ru");

/** Application-wide context: Russian locale, design tokens, antd feedback context and the server-state cache. */
export function Providers({ children }: { children: ReactNode }) {
  return (
    <ConfigProvider locale={ru_RU} theme={theme} wave={{ disabled: true }}>
      <App>
        <QueryClientProvider client={queryClient}>
          {children}
        </QueryClientProvider>
      </App>
    </ConfigProvider>
  );
}
