import { useQueryClient } from "@tanstack/react-query";
import { theme as antdTheme } from "antd";
import { useEffect, useRef } from "react";
import { useLocation } from "react-router";
import { deskCountQueryKey, useDeskCount } from "./deskApi";

const MAX_SHOWN = 99;

/**
 * Count of the user's expected actions on the desk menu entry. Nothing is shown at zero, before the first answer and
 * after a failed read, since the desk itself reports the failure. Moving to another screen rereads the count; there is
 * no timer, so others' changes show on the next move or return to the window.
 */
export function DeskCounter() {
  const { token } = antdTheme.useToken();
  const { data, isError } = useDeskCount();
  useRereadOnNavigation();
  const count = data?.actionCount ?? 0;
  if (isError || count === 0) return null;
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        minWidth: token.controlHeightSM,
        height: token.controlHeightSM,
        paddingInline: token.paddingXS,
        marginInlineStart: token.marginXS,
        borderRadius: token.controlHeightSM / 2,
        background: "var(--ant-color-accent)",
        color: token.colorBgSpotlight,
        fontSize: token.fontSize,
        fontWeight: token.fontWeightStrong,
      }}
    >
      {count > MAX_SHOWN ? `${MAX_SHOWN}+` : count}
    </span>
  );
}

function useRereadOnNavigation() {
  const { pathname } = useLocation();
  const queryClient = useQueryClient();
  const lastPathname = useRef(pathname);
  useEffect(() => {
    if (lastPathname.current === pathname) return;
    lastPathname.current = pathname;
    void queryClient.invalidateQueries({
      queryKey: deskCountQueryKey,
      exact: true,
    });
  }, [pathname, queryClient]);
}
