import { Button, type ButtonProps } from "antd";
import { useNavigate } from "react-router";

interface LinkButtonProps extends Pick<ButtonProps, "type" | "children"> {
  to: string;
  /** Navigation state handed to the opened screen, such as the return link of its breadcrumb. */
  state?: unknown;
}

/** A link that looks like a button: a plain left click navigates inside the application, a modified or middle click opens it the browser's way, as in a new tab. */
export function LinkButton({ to, type, state, children }: LinkButtonProps) {
  const navigate = useNavigate();
  return (
    <Button
      type={type}
      href={to}
      onClick={(event) => {
        if (
          event.button !== 0 ||
          event.ctrlKey ||
          event.metaKey ||
          event.shiftKey ||
          event.altKey
        ) {
          return;
        }
        event.preventDefault();
        void navigate(to, { state });
      }}
    >
      {children}
    </Button>
  );
}
